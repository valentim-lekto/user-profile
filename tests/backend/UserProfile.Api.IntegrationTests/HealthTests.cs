using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserProfile.Api.Data;
using UserProfile.Api.Features.Auth;
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

        var expectedColumns = new Dictionary<string, (string Type, long NotNull, long PrimaryKey)>
        {
            ["Id"] = ("TEXT", 1, 1),
            ["Name"] = ("TEXT", 1, 0),
            ["Email"] = ("TEXT", 1, 0),
            ["NormalizedEmail"] = ("TEXT", 1, 0),
            ["PasswordHash"] = ("TEXT", 1, 0),
            ["CreatedAtUtc"] = ("TEXT", 1, 0),
            ["UpdatedAtUtc"] = ("TEXT", 1, 0)
        };

        await using var columnsCommand = connection.CreateCommand();
        columnsCommand.CommandText = "SELECT name, type, \"notnull\", pk FROM pragma_table_info('Users');";

        var actualColumns = new Dictionary<string, (string Type, long NotNull, long PrimaryKey)>();
        await using var columns = await columnsCommand.ExecuteReaderAsync();
        while (await columns.ReadAsync())
        {
            actualColumns.Add(
                columns.GetString(0),
                (columns.GetString(1), columns.GetInt64(2), columns.GetInt64(3)));
        }

        Assert.Equal(expectedColumns.Count, actualColumns.Count);
        foreach (var expectedColumn in expectedColumns)
        {
            Assert.True(actualColumns.TryGetValue(expectedColumn.Key, out var actualColumn));
            Assert.Equal(expectedColumn.Value, actualColumn);
        }
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
        var stopwatch = Stopwatch.StartNew();
        try
        {
            unavailableResponse = await client.GetAsync("/health");
            stopwatch.Stop();
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
            Assert.True(
                stopwatch.Elapsed < TimeSpan.FromSeconds(5),
                $"Health check took {stopwatch.Elapsed} despite its one-second command timeout.");
        }
    }

    [Fact]
    public async Task SwaggerContainsOnlyTheImplementedOperationsAndRequiredSchemas()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.Equal(
            ["/api/auth/register", "/health"],
            paths.EnumerateObject().Select(path => path.Name).Order());

        var healthOperation = paths.GetProperty("/health").GetProperty("get");
        Assert.Equal("getHealth", healthOperation.GetProperty("operationId").GetString());
        Assert.Equal(
            ["Operations"],
            healthOperation.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()));
        Assert.Equal(
            ["200", "500", "503"],
            healthOperation
                .GetProperty("responses")
                .EnumerateObject()
                .Select(operationResponse => operationResponse.Name)
                .Order());

        var registerOperation = paths
            .GetProperty("/api/auth/register")
            .GetProperty("post");
        Assert.Equal("registerUser", registerOperation.GetProperty("operationId").GetString());
        Assert.Equal(
            ["Auth"],
            registerOperation.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()));
        Assert.Equal(
            ["201", "400", "409", "413", "415", "500", "503"],
            registerOperation
                .GetProperty("responses")
                .EnumerateObject()
                .Select(operationResponse => operationResponse.Name)
                .Order());

        var info = document.RootElement.GetProperty("info");
        Assert.Equal("User Profile API", info.GetProperty("title").GetString());
        Assert.Equal("1.0.0", info.GetProperty("version").GetString());

        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");
        var healthSchema = schemas.GetProperty(nameof(HealthResponse));
        Assert.Contains(
            healthSchema.GetProperty("required").EnumerateArray(),
            property => property.GetString() == "status");
        var statusSchema = healthSchema.GetProperty("properties").GetProperty("status");
        Assert.Equal("string", statusSchema.GetProperty("type").GetString());
        Assert.Equal(
            ["Healthy"],
            statusSchema.GetProperty("enum").EnumerateArray().Select(value => value.GetString()));

        var registerSchema = schemas.GetProperty("RegisterRequest");
        Assert.False(registerSchema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(
            ["email", "name", "password", "passwordConfirmation"],
            registerSchema
                .GetProperty("required")
                .EnumerateArray()
                .Select(property => property.GetString())
                .Order());
        var registerProperties = registerSchema.GetProperty("properties");
        var nameProperty = registerProperties.GetProperty("name");
        Assert.False(nameProperty.TryGetProperty("minLength", out _));
        Assert.False(nameProperty.TryGetProperty("maxLength", out _));
        Assert.False(nameProperty.TryGetProperty("format", out _));
        Assert.True(nameProperty.GetProperty("x-trim").GetBoolean());
        Assert.Equal(3, nameProperty.GetProperty("x-min-length-after-trim").GetInt32());
        Assert.Equal(200, nameProperty.GetProperty("x-max-length-after-trim").GetInt32());

        var emailProperty = registerProperties.GetProperty("email");
        Assert.False(emailProperty.TryGetProperty("minLength", out _));
        Assert.False(emailProperty.TryGetProperty("maxLength", out _));
        Assert.False(emailProperty.TryGetProperty("format", out _));
        Assert.Equal(
            RegisterRequestSchemaFilter.RawEmailPattern,
            emailProperty.GetProperty("pattern").GetString());
        Assert.True(emailProperty.GetProperty("x-trim").GetBoolean());
        Assert.Equal(1, emailProperty.GetProperty("x-min-length-after-trim").GetInt32());
        Assert.Equal(320, emailProperty.GetProperty("x-max-length-after-trim").GetInt32());
        Assert.Equal(
            RegisterRequest.EmailPattern,
            emailProperty.GetProperty("x-pattern-after-trim").GetString());
        Assert.Equal(6, registerProperties.GetProperty("password").GetProperty("minLength").GetInt32());
        Assert.Equal(128, registerProperties.GetProperty("password").GetProperty("maxLength").GetInt32());
        Assert.Equal(
            "password",
            registerProperties.GetProperty("password").GetProperty("format").GetString());
        Assert.True(registerProperties.GetProperty("password").GetProperty("writeOnly").GetBoolean());
        Assert.Equal(
            6,
            registerProperties.GetProperty("passwordConfirmation").GetProperty("minLength").GetInt32());
        Assert.Equal(
            128,
            registerProperties.GetProperty("passwordConfirmation").GetProperty("maxLength").GetInt32());
        Assert.Equal(
            "password",
            registerProperties.GetProperty("passwordConfirmation").GetProperty("format").GetString());
        Assert.True(
            registerProperties
                .GetProperty("passwordConfirmation")
                .GetProperty("writeOnly")
                .GetBoolean());
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
