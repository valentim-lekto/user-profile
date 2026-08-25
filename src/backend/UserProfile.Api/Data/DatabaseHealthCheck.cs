using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UserProfile.Api.Data;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();

            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Unhealthy("Database is unavailable.");
            }

            await dbContext.Database.OpenConnectionAsync(cancellationToken);

            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";";

            var appliedMigrationCount = Convert.ToInt32(
                await command.ExecuteScalarAsync(cancellationToken));
            var expectedMigrationCount = dbContext.Database.GetMigrations().Count();

            return appliedMigrationCount == expectedMigrationCount && expectedMigrationCount > 0
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database schema is not current.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Database is unavailable.");
        }
    }
}
