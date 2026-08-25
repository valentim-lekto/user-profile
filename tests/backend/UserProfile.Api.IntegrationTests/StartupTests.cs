using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace UserProfile.Api.IntegrationTests;

public sealed class StartupTests
{
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

            using var factory = new ConflictingSchemaFactory(databasePath);

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

    private static async Task CreateConflictingSchemaAsync(string databasePath)
    {
        await using var connection = new SqliteConnection(
            $"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE \"Users\" (\"Id\" TEXT NOT NULL PRIMARY KEY);";
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ConflictingSchemaFactory(string databasePath)
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
                        $"Data Source={databasePath};Default Timeout=1;Pooling=False"
                });
            });
        }
    }
}
