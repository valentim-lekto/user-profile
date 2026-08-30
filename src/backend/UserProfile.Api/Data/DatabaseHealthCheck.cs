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

            await dbContext.Database.OpenConnectionAsync(cancellationToken);

            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\";";
            command.CommandTimeout = 1;

            var appliedMigrations = new HashSet<string>(StringComparer.Ordinal);
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    appliedMigrations.Add(reader.GetString(0));
                }
            }

            var expectedMigrations = dbContext.Database
                .GetMigrations()
                .ToHashSet(StringComparer.Ordinal);

            return expectedMigrations.Count > 0 && appliedMigrations.SetEquals(expectedMigrations)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database schema is not current.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Database is unavailable.");
        }
    }
}
