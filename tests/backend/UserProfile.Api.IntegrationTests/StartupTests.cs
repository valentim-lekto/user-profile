using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using UserProfile.Api.Data;

namespace UserProfile.Api.IntegrationTests;

public sealed class StartupTests
{
    private const int TestSigningKeyBytes = 64;

    [Fact]
    public async Task StartupFailsWhenInitialMigrationCannotApply()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"user-profile-api-startup-tests-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(testDirectory, "user-profile.db");
        Directory.CreateDirectory(testDirectory);

        try
        {
            await CreateConflictingSchemaAsync(databasePath);

            await using var factory = new StartupFactory(databasePath);

            var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                using var client = factory.CreateClient();
                using var response = await client.GetAsync("/health");
            });

            var sqliteException = Assert.IsType<SqliteException>(exception.GetBaseException());
            Assert.Equal(1, sqliteException.SqliteErrorCode);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task StartupRecoversOrphanedMigrationLockWithoutChangingApplicationData()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"user-profile-api-lock-tests-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(testDirectory, "user-profile.db");
        var userId = Guid.NewGuid();
        Directory.CreateDirectory(testDirectory);

        StartupFactory? recoveryFactory = null;
        Task<HttpStatusCode>? startupTask = null;
        try
        {
            using (var bootstrapFactory = new StartupFactory(databasePath))
            using (var client = bootstrapFactory.CreateClient())
            using (var response = await client.GetAsync("/health"))
            {
                response.EnsureSuccessStatusCode();
            }

            await SeedUserAndOrphanedLockAsync(databasePath, userId);
            var migrationsBefore = await ReadMigrationIdsAsync(databasePath);

            recoveryFactory = new StartupFactory(databasePath);
            startupTask = Task.Run(async () =>
            {
                using var client = recoveryFactory.CreateClient();
                using var response = await client.GetAsync("/health");
                return response.StatusCode;
            });

            var completedWithinLimit = ReferenceEquals(
                await Task.WhenAny(startupTask, Task.Delay(TimeSpan.FromSeconds(20))),
                startupTask);
            Assert.True(
                completedWithinLimit,
                "Startup remained blocked after its migration deadline.");

            var statusCode = await startupTask;
            Assert.Equal(HttpStatusCode.OK, statusCode);
            Assert.Equal(1L, await CountUserAsync(databasePath, userId));
            Assert.Equal(migrationsBefore, await ReadMigrationIdsAsync(databasePath));
            Assert.Equal(0L, await CountMigrationLockRowsAsync(databasePath));
        }
        finally
        {
            if (startupTask is { IsCompleted: false })
            {
                await DropMigrationLockTableAsync(databasePath);
                try
                {
                    await startupTask.WaitAsync(TimeSpan.FromSeconds(10));
                }
                catch
                {
                    // The assertion reports startup failures; cleanup must still continue.
                }
            }

            recoveryFactory?.Dispose();
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task StartupCancelsMigrationPhaseAtOperationalDeadline(
        int connectionOpeningToBlock)
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"user-profile-api-deadline-{connectionOpeningToBlock}-tests-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(testDirectory, "user-profile.db");
        Directory.CreateDirectory(testDirectory);

        StartupFactory? deadlineFactory = null;
        Task<HttpStatusCode>? startupTask = null;
        using var blocker = new BlockingMigrationConnectionInterceptor(
            connectionOpeningToBlock);
        try
        {
            using (var bootstrapFactory = new StartupFactory(databasePath))
            using (var client = bootstrapFactory.CreateClient())
            using (var response = await client.GetAsync("/health"))
            {
                response.EnsureSuccessStatusCode();
            }

            deadlineFactory = new StartupFactory(databasePath, blocker);
            startupTask = Task.Run(async () =>
            {
                using var client = deadlineFactory.CreateClient();
                using var response = await client.GetAsync("/health");
                return response.StatusCode;
            });

            await blocker.Started.WaitAsync(TimeSpan.FromSeconds(20));
            var completedWithinLimit = ReferenceEquals(
                await Task.WhenAny(startupTask, Task.Delay(TimeSpan.FromSeconds(20))),
                startupTask);
            Assert.True(
                completedWithinLimit,
                $"Startup did not cancel retained migration phase {connectionOpeningToBlock} at its deadline.");

            var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                _ = await startupTask;
            });
            Assert.True(
                ContainsException<TimeoutException>(exception),
                "Startup failure did not preserve the migration deadline timeout.");
        }
        finally
        {
            blocker.Release();
            if (startupTask is { IsCompleted: false })
            {
                try
                {
                    await startupTask.WaitAsync(TimeSpan.FromSeconds(10));
                }
                catch
                {
                    // The assertion reports startup failures; cleanup must still continue.
                }
            }

            deadlineFactory?.Dispose();
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task StartingAsyncPropagatesCallerCancellationToMigrationOperation()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"user-profile-api-caller-cancellation-tests-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(testDirectory, "user-profile.db");
        Directory.CreateDirectory(testDirectory);

        try
        {
            using var blocker = new BlockingMigrationConnectionInterceptor(2);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<UserProfileDbContext>(options =>
            {
                options.UseSqlite(
                    $"Data Source={databasePath};Default Timeout=1;Pooling=False");
                options.AddInterceptors(blocker);
            });
            await using var serviceProvider = services.BuildServiceProvider();
            var migrationService = new DatabaseMigrationStartupService(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                serviceProvider.GetRequiredService<
                    ILogger<DatabaseMigrationStartupService>>());
            using var callerCancellation = new CancellationTokenSource();
            var startupTask = migrationService.StartingAsync(callerCancellation.Token);

            try
            {
                await blocker.Started.WaitAsync(TimeSpan.FromSeconds(10));

                callerCancellation.Cancel();

                Assert.True(
                    blocker.BlockedOperationCancellationToken.IsCancellationRequested,
                    "Caller cancellation did not reach the migration database operation.");
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startupTask);
            }
            finally
            {
                blocker.Release();
                if (!startupTask.IsCompleted)
                {
                    try
                    {
                        await startupTask.WaitAsync(TimeSpan.FromSeconds(10));
                    }
                    catch
                    {
                        // The assertion reports propagation failures; cleanup must still continue.
                    }
                }
            }
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task SigtermCancelsMigrationLifecycleBeforeApplicationReadiness()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"user-profile-api-sigterm-tests-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(testDirectory, "user-profile.db");
        var userId = Guid.NewGuid();
        Directory.CreateDirectory(testDirectory);

        Process? apiProcess = null;
        await using var blockingConnection = CreateConnection(databasePath);
        var exclusiveLockHeld = false;
        var processStarted = false;
        try
        {
            using (var bootstrapFactory = new StartupFactory(databasePath))
            using (var client = bootstrapFactory.CreateClient())
            using (var response = await client.GetAsync("/health"))
            {
                response.EnsureSuccessStatusCode();
            }

            await SeedUserAndOrphanedLockAsync(databasePath, userId);
            var migrationsBefore = await ReadMigrationIdsAsync(databasePath);

            await blockingConnection.OpenAsync();
            await ExecuteNonQueryAsync(blockingConnection, "BEGIN EXCLUSIVE;");
            exclusiveLockHeld = true;

            var output = new ConcurrentQueue<string>();
            var migrationPreparationStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var applicationStoppingObserved = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            apiProcess = new Process
            {
                StartInfo = CreateApiProcessStartInfo(databasePath),
                EnableRaisingEvents = true
            };
            void ObserveOutput(string? line)
            {
                if (line is null)
                {
                    return;
                }

                output.Enqueue(line);
                if (line.Contains("Preparing database migration lock.", StringComparison.Ordinal))
                {
                    migrationPreparationStarted.TrySetResult();
                }

                if (line.Contains(
                        "Database migration startup cancellation requested.",
                        StringComparison.Ordinal))
                {
                    applicationStoppingObserved.TrySetResult();
                }
            }

            apiProcess.OutputDataReceived += (_, eventArgs) => ObserveOutput(eventArgs.Data);
            apiProcess.ErrorDataReceived += (_, eventArgs) => ObserveOutput(eventArgs.Data);

            Assert.True(apiProcess.Start(), "The API subprocess did not start.");
            processStarted = true;
            apiProcess.BeginOutputReadLine();
            apiProcess.BeginErrorReadLine();

            await WaitForProcessMarkerAsync(
                migrationPreparationStarted.Task,
                apiProcess,
                output,
                "migration preparation");

            var cancellationWatch = Stopwatch.StartNew();
            await SendSigtermAsync(apiProcess.Id);
            await WaitForProcessMarkerAsync(
                applicationStoppingObserved.Task,
                apiProcess,
                output,
                "host shutdown");

            await ExecuteNonQueryAsync(blockingConnection, "ROLLBACK;");
            exclusiveLockHeld = false;

            await apiProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            cancellationWatch.Stop();

            Assert.True(
                apiProcess.ExitCode == 0,
                $"API subprocess exited with code {apiProcess.ExitCode}. " +
                $"Recent output:{Environment.NewLine}" +
                string.Join(Environment.NewLine, output.TakeLast(20)));
            Assert.True(
                cancellationWatch.Elapsed < TimeSpan.FromSeconds(10),
                "The API subprocess did not observe SIGTERM before its internal deadline.");
            Assert.DoesNotContain(
                output,
                line => line.Contains("Now listening on", StringComparison.Ordinal));
            Assert.Equal(1L, await CountUserAsync(databasePath, userId));
            Assert.Equal(migrationsBefore, await ReadMigrationIdsAsync(databasePath));
            Assert.Equal(0L, await CountMigrationLockRowsIfPresentAsync(databasePath));
        }
        finally
        {
            if (exclusiveLockHeld)
            {
                try
                {
                    await ExecuteNonQueryAsync(blockingConnection, "ROLLBACK;");
                }
                catch
                {
                    // Best-effort release lets subprocess cleanup proceed below.
                }
            }

            if (processStarted && apiProcess is { HasExited: false })
            {
                apiProcess.Kill(entireProcessTree: true);
                await apiProcess.WaitForExitAsync();
            }

            apiProcess?.Dispose();
            await blockingConnection.CloseAsync();
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    private static async Task CreateConflictingSchemaAsync(string databasePath)
    {
        await using var connection = new SqliteConnection(
            $"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE \"Users\" (\"Id\" TEXT NOT NULL PRIMARY KEY);";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SeedUserAndOrphanedLockAsync(
        string databasePath,
        Guid userId)
    {
        await using var connection = CreateConnection(databasePath);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "Users" (
                "Id", "Name", "Email", "NormalizedEmail", "PasswordHash",
                "CreatedAtUtc", "UpdatedAtUtc")
            VALUES (
                $id, 'Lock Recovery User', 'lock-recovery@example.test',
                'LOCK-RECOVERY@EXAMPLE.TEST', 'synthetic-test-hash',
                '2026-08-28T12:00:00Z', '2026-08-28T12:00:00Z');

            INSERT INTO "__EFMigrationsLock" ("Id", "Timestamp")
            VALUES (1, CURRENT_TIMESTAMP);
            """;
        command.Parameters.AddWithValue("$id", userId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<string>> ReadMigrationIdsAsync(string databasePath)
    {
        await using var connection = CreateConnection(databasePath);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "MigrationId"
            FROM "__EFMigrationsHistory"
            ORDER BY "MigrationId";
            """;

        var migrations = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            migrations.Add(reader.GetString(0));
        }

        return migrations;
    }

    private static async Task<long> CountUserAsync(string databasePath, Guid userId)
    {
        await using var connection = CreateConnection(databasePath);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"Users\" WHERE \"Id\" = $id;";
        command.Parameters.AddWithValue("$id", userId);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<long> CountMigrationLockRowsAsync(string databasePath)
    {
        await using var connection = CreateConnection(databasePath);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsLock\";";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<long> CountMigrationLockRowsIfPresentAsync(string databasePath)
    {
        await using var connection = CreateConnection(databasePath);
        await connection.OpenAsync();
        await using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table' AND name = '__EFMigrationsLock';
            """;
        if (Convert.ToInt64(await tableCommand.ExecuteScalarAsync()) == 0)
        {
            return 0;
        }

        await using var rowsCommand = connection.CreateCommand();
        rowsCommand.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsLock\";";
        return Convert.ToInt64(await rowsCommand.ExecuteScalarAsync());
    }

    private static async Task DropMigrationLockTableAsync(string databasePath)
    {
        await using var connection = CreateConnection(databasePath);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DROP TABLE IF EXISTS \"__EFMigrationsLock\";";
        await command.ExecuteNonQueryAsync();
    }

    private static SqliteConnection CreateConnection(string databasePath)
    {
        return new SqliteConnection(
            $"Data Source={databasePath};Default Timeout=1;Pooling=False");
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }

    private static ProcessStartInfo CreateApiProcessStartInfo(string databasePath)
    {
        var apiAssemblyPath = typeof(Program).Assembly.Location;
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(apiAssemblyPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(apiAssemblyPath);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        startInfo.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        startInfo.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";
        startInfo.Environment["ConnectionStrings__Default"] =
            $"Data Source={databasePath};Default Timeout=30;Pooling=False";
        startInfo.Environment["Jwt__SigningKey"] = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(TestSigningKeyBytes));
        return startInfo;
    }

    private static async Task SendSigtermAsync(int processId)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/kill",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-TERM");
        startInfo.ArgumentList.Add(processId.ToString());

        using var signalProcess = Process.Start(startInfo);
        Assert.NotNull(signalProcess);
        await signalProcess.WaitForExitAsync();
        Assert.Equal(0, signalProcess.ExitCode);
    }

    private static async Task WaitForProcessMarkerAsync(
        Task marker,
        Process process,
        ConcurrentQueue<string> output,
        string markerDescription)
    {
        var processExit = process.WaitForExitAsync();
        var timeout = Task.Delay(TimeSpan.FromSeconds(10));
        var completed = await Task.WhenAny(marker, processExit, timeout);
        Assert.True(
            ReferenceEquals(completed, marker),
            $"API subprocess did not report {markerDescription}. " +
            $"ExitCode={(process.HasExited ? process.ExitCode : null)}. " +
            $"Recent output:{Environment.NewLine}" +
            string.Join(Environment.NewLine, output.TakeLast(20)));
        await marker;
    }

    private static bool ContainsException<TException>(Exception exception)
        where TException : Exception
    {
        if (exception is TException)
        {
            return true;
        }

        if (exception is AggregateException aggregateException)
        {
            return aggregateException.InnerExceptions.Any(ContainsException<TException>);
        }

        return exception.InnerException is not null &&
            ContainsException<TException>(exception.InnerException);
    }

    private sealed class StartupFactory(
        string databasePath,
        IInterceptor? dbInterceptor = null)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] =
                        $"Data Source={databasePath};Default Timeout=1;Pooling=False",
                    ["Jwt:SigningKey"] = Convert.ToBase64String(
                        RandomNumberGenerator.GetBytes(TestSigningKeyBytes))
                });
            });

            if (dbInterceptor is not null)
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<
                        IDbContextOptionsConfiguration<UserProfileDbContext>>();
                    services.AddDbContext<UserProfileDbContext>(options =>
                    {
                        options.UseSqlite(
                            $"Data Source={databasePath};Default Timeout=1;Pooling=False");
                        options.AddInterceptors(dbInterceptor);
                    });
                });
            }
        }
    }

    private sealed class BlockingMigrationConnectionInterceptor(
        int connectionOpeningToBlock)
        : DbConnectionInterceptor, IDisposable
    {
        private int connectionOpenings;
        private readonly CancellationTokenSource release = new();
        private readonly TaskCompletionSource started = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken BlockedOperationCancellationToken { get; private set; }

        public Task Started => started.Task;

        public void Release() => release.Cancel();

        public void Dispose() => release.Dispose();

        public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref connectionOpenings) != connectionOpeningToBlock)
            {
                return result;
            }

            BlockedOperationCancellationToken = cancellationToken;
            started.TrySetResult();
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                release.Token);
            await Task.Delay(Timeout.InfiniteTimeSpan, linkedCancellation.Token);

            return result;
        }
    }
}
