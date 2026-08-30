using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Cryptography;
using UserProfile.Api.Data;

namespace UserProfile.Api.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private const int TestSigningKeyBytes = 64;

    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly int databaseTimeoutSeconds;
    private readonly IInterceptor? dbInterceptor;
    private readonly AdjustableTimeProvider timeProvider = new(FixedUtcNow);
    private readonly byte[] jwtSigningKey = RandomNumberGenerator.GetBytes(
        TestSigningKeyBytes);
    private readonly string testDirectory = Path.Combine(
        Path.GetTempPath(),
        $"user-profile-api-tests-{Guid.NewGuid():N}");

    public ApiFactory() : this(30, null)
    {
    }

    private ApiFactory(int databaseTimeoutSeconds, IInterceptor? dbInterceptor)
    {
        this.databaseTimeoutSeconds = databaseTimeoutSeconds;
        this.dbInterceptor = dbInterceptor;
        Directory.CreateDirectory(testDirectory);
    }

    public string DatabasePath => Path.Combine(testDirectory, "user-profile.db");

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public byte[] JwtSigningKey => jwtSigningKey.ToArray();

    public string JwtIssuer => "UserProfile.Api";

    public string JwtAudience => "UserProfile.Web";

    public static ApiFactory WithDatabaseTimeout(int seconds) => new(seconds, null);

    public static ApiFactory WithInterceptor(IInterceptor interceptor) => new(30, interceptor);

    public void AdvanceTime(TimeSpan elapsed) => timeProvider.Advance(elapsed);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    $"Data Source={DatabasePath};Default Timeout={databaseTimeoutSeconds};Pooling=False",
                ["Jwt:SigningKey"] = Convert.ToBase64String(jwtSigningKey),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:LifetimeMinutes"] = "15"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(timeProvider);

            if (dbInterceptor is not null)
            {
                services.RemoveAll<IDbContextOptionsConfiguration<UserProfileDbContext>>();
                services.AddDbContext<UserProfileDbContext>(options =>
                {
                    options.UseSqlite(
                        $"Data Source={DatabasePath};Default Timeout={databaseTimeoutSeconds};Pooling=False");
                    options.AddInterceptors(dbInterceptor);
                });
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private long utcTicks = utcNow.UtcTicks;

        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(Interlocked.Read(ref utcTicks), TimeSpan.Zero);
        }

        public void Advance(TimeSpan elapsed)
        {
            if (elapsed <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsed),
                    elapsed,
                    "Elapsed time must be positive.");
            }

            Interlocked.Add(ref utcTicks, elapsed.Ticks);
        }
    }
}
