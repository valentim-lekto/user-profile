using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserProfile.Api.Data;
using UserProfile.Api.Features.Operations;
using UserProfile.Api.IntegrationTests.Infrastructure;

namespace UserProfile.Api.IntegrationTests;

public sealed class HealthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task HealthReturnsHealthyAfterStartupMigration()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task StartupCreatesMigrationHistoryAndUniqueNormalizedEmailIndex()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        Assert.False(dbContext.Database.HasPendingModelChanges());

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var migrationCommand = connection.CreateCommand();
        migrationCommand.CommandText = """
            SELECT COUNT(*)
            FROM "__EFMigrationsHistory"
            WHERE "MigrationId" LIKE '%_InitialCreate';
            """;
        Assert.Equal(1L, Convert.ToInt64(await migrationCommand.ExecuteScalarAsync()));

        await using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = """
            SELECT sql
            FROM sqlite_master
            WHERE type = 'index' AND name = 'UX_Users_NormalizedEmail';
            """;
        var indexDefinition = Assert.IsType<string>(await indexCommand.ExecuteScalarAsync());
        Assert.Contains("UNIQUE", indexDefinition, StringComparison.OrdinalIgnoreCase);

        await using var columnCommand = connection.CreateCommand();
        columnCommand.CommandText = """
            SELECT name
            FROM pragma_index_info('UX_Users_NormalizedEmail');
            """;
        Assert.Equal("NormalizedEmail", await columnCommand.ExecuteScalarAsync());

        await using var timestampCommand = connection.CreateCommand();
        timestampCommand.CommandText = """
            SELECT COUNT(*)
            FROM pragma_table_info('Users')
            WHERE name IN ('CreatedAtUtc', 'UpdatedAtUtc')
              AND type = 'TEXT'
              AND "notnull" = 1;
            """;
        Assert.Equal(2L, Convert.ToInt64(await timestampCommand.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task HealthReturnsProblemDetailsWhenDatabaseBecomesUnavailable()
    {
        using var client = factory.CreateClient();
        using var healthyResponse = await client.GetAsync("/health");
        healthyResponse.EnsureSuccessStatusCode();

        await using var lockConnection = CreateConnection();
        await lockConnection.OpenAsync();

        await using var lockCommand = lockConnection.CreateCommand();
        lockCommand.CommandText = "PRAGMA busy_timeout=1000;";
        await lockCommand.ExecuteNonQueryAsync();
        lockCommand.CommandText = "PRAGMA journal_mode=DELETE;";
        Assert.Equal(
            "delete",
            Convert.ToString(await lockCommand.ExecuteScalarAsync())?.ToLowerInvariant());
        lockCommand.CommandText = "PRAGMA locking_mode=EXCLUSIVE;";
        Assert.Equal(
            "exclusive",
            Convert.ToString(await lockCommand.ExecuteScalarAsync())?.ToLowerInvariant());
        lockCommand.CommandText = "BEGIN EXCLUSIVE;";
        await lockCommand.ExecuteNonQueryAsync();
        lockCommand.CommandText = """
            UPDATE "__EFMigrationsHistory"
            SET "ProductVersion" = "ProductVersion" || '-locked';
            """;
        await lockCommand.ExecuteNonQueryAsync();

        HttpResponseMessage? unavailableResponse = null;
        try
        {
            unavailableResponse = await client.GetAsync("/health");
        }
        finally
        {
            await using var rollbackCommand = lockConnection.CreateCommand();
            rollbackCommand.CommandText = "ROLLBACK;";
            await rollbackCommand.ExecuteNonQueryAsync();
        }

        using (unavailableResponse)
        {
            Assert.NotNull(unavailableResponse);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailableResponse.StatusCode);
            Assert.Equal(
                "application/problem+json",
                unavailableResponse.Content.Headers.ContentType?.MediaType);

            using var body = JsonDocument.Parse(
                await unavailableResponse.Content.ReadAsStreamAsync());
            Assert.Equal(503, body.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Service Unavailable", body.RootElement.GetProperty("title").GetString());
            Assert.Equal("The service is not ready.", body.RootElement.GetProperty("detail").GetString());
            Assert.Equal("/health", body.RootElement.GetProperty("instance").GetString());
        }
    }

    [Fact]
    public async Task SwaggerContainsOnlyTheImplementedHealthOperationAndRequiredSchema()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var paths = document.RootElement.GetProperty("paths");
        var path = Assert.Single(paths.EnumerateObject());

        Assert.Equal("/health", path.Name);
        Assert.True(path.Value.TryGetProperty("get", out _));

        var healthSchema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty(nameof(HealthResponse));
        Assert.Contains(
            healthSchema.GetProperty("required").EnumerateArray(),
            property => property.GetString() == "status");
    }

    [Fact]
    public async Task UnknownApiRouteReturnsProblemDetails()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/not-implemented");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", body.RootElement.GetProperty("title").GetString());
        Assert.Equal("/api/not-implemented", body.RootElement.GetProperty("instance").GetString());
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection(
            $"Data Source={factory.DatabasePath};Default Timeout=1;Pooling=False");
    }
}
