using Microsoft.EntityFrameworkCore;

namespace UserProfile.Api.Data;

public sealed class DatabaseMigrationStartupService(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseMigrationStartupService> logger) : IHostedLifecycleService
{
    private static readonly TimeSpan MigrationDeadline = TimeSpan.FromSeconds(15);

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        using var migrationDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await using var hostCancellationRegistration = cancellationToken.Register(
            () => logger.LogInformation("Database migration startup cancellation requested."));
        migrationDeadline.CancelAfter(MigrationDeadline);

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();

        logger.LogInformation("Applying database migrations.");

        try
        {
            await dbContext.Database.OpenConnectionAsync(migrationDeadline.Token);
            try
            {
                logger.LogInformation("Preparing database migration lock.");
                await using var command = dbContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = "DROP TABLE IF EXISTS \"__EFMigrationsLock\";";
                command.CommandTimeout = 5;
                await command.ExecuteNonQueryAsync(migrationDeadline.Token);
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }

            await dbContext.Database.MigrateAsync(migrationDeadline.Token);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested && migrationDeadline.IsCancellationRequested)
        {
            throw new TimeoutException(
                "Database migration startup exceeded its 15-second deadline.",
                exception);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
