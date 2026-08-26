using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UserProfile.Api.Data;

namespace UserProfile.Api.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly int databaseTimeoutSeconds;
    private readonly IInterceptor? dbInterceptor;
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

    public static ApiFactory WithDatabaseTimeout(int seconds) => new(seconds, null);

    public static ApiFactory WithInterceptor(IInterceptor interceptor) => new(30, interceptor);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    $"Data Source={DatabasePath};Default Timeout={databaseTimeoutSeconds};Pooling=False"
            });
        });

        if (dbInterceptor is not null)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<UserProfileDbContext>>();
                services.AddDbContext<UserProfileDbContext>(options =>
                {
                    options.UseSqlite(
                        $"Data Source={DatabasePath};Default Timeout={databaseTimeoutSeconds};Pooling=False");
                    options.AddInterceptors(dbInterceptor);
                });
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }
}
