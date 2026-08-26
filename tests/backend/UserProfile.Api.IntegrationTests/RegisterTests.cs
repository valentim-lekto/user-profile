using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using UserProfile.Api.Data;
using UserProfile.Api.IntegrationTests.Infrastructure;

namespace UserProfile.Api.IntegrationTests;

public sealed class RegisterTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task ValidRegistrationReturnsCreatedAndPersistsProtectedPassword()
    {
        using var client = factory.CreateClient();
        var password = CreatePassword();
        var email = $"  {new string('a', 307)}@Example.Test  ";
        Assert.Equal(320, email.Trim().Length);
        var before = DateTime.UtcNow;

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegistrationPayload("  Ana Example  ", email, password, password));
        var after = DateTime.UtcNow;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var responseText = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(responseText);
        var property = Assert.Single(body.RootElement.EnumerateObject());
        Assert.Equal("message", property.Name);
        Assert.Equal("Registration completed successfully.", property.Value.GetString());
        Assert.DoesNotContain("password", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("normalizedEmail", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", responseText, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(candidate => candidate.NormalizedEmail == normalizedEmail);

        Assert.Equal("Ana Example", user.Name);
        Assert.Equal(email.Trim(), user.Email);
        Assert.Equal(normalizedEmail, user.NormalizedEmail);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.NotEqual(default, user.CreatedAtUtc);
        Assert.Equal(user.CreatedAtUtc, user.UpdatedAtUtc);
        Assert.InRange(user.CreatedAtUtc, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.NotEqual(password, user.PasswordHash);

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password));
    }

    [Theory]
    [InlineData("missing-name", "name")]
    [InlineData("short-name-after-trim", "name")]
    [InlineData("long-name", "name")]
    [InlineData("missing-email", "email")]
    [InlineData("invalid-email", "email")]
    [InlineData("email-without-domain-dot", "email")]
    [InlineData("email-with-inner-space", "email")]
    [InlineData("long-email", "email")]
    [InlineData("missing-password", "password")]
    [InlineData("short-password", "password")]
    [InlineData("long-password", "password")]
    [InlineData("missing-confirmation", "passwordConfirmation")]
    [InlineData("long-confirmation", "passwordConfirmation")]
    [InlineData("different-confirmation", "passwordConfirmation")]
    public async Task InvalidRegistrationReturnsValidationProblemDetails(
        string scenario,
        string expectedField)
    {
        using var client = factory.CreateClient();
        var countBefore = await CountUsersAsync();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            CreateInvalidPayload(scenario));

        await AssertValidationProblemAsync(response, expectedField);
        Assert.Equal(countBefore, await CountUsersAsync());
    }

    [Fact]
    public async Task UnknownJsonPropertyReturnsValidationProblemDetails()
    {
        using var client = factory.CreateClient();
        var countBefore = await CountUsersAsync();
        var password = CreatePassword();
        var payload = new Dictionary<string, object?>
        {
            ["name"] = "Ana Example",
            ["email"] = CreateEmail(),
            ["password"] = password,
            ["passwordConfirmation"] = password,
            ["userId"] = Guid.NewGuid()
        };

        using var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        await AssertValidationProblemAsync(response);
        Assert.Equal(countBefore, await CountUsersAsync());
    }

    [Fact]
    public async Task ExactDuplicateEmailReturnsConflictWithoutSecondUser()
    {
        using var client = factory.CreateClient();
        var email = CreateEmail();
        var password = CreatePassword();
        var payload = new RegistrationPayload("Ana Example", email, password, password);

        using var firstResponse = await client.PostAsJsonAsync("/api/auth/register", payload);
        using var duplicateResponse = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        await AssertEmailConflictAsync(duplicateResponse);
        Assert.Equal(1, await CountUsersByNormalizedEmailAsync(email.ToUpperInvariant()));
    }

    [Fact]
    public async Task DuplicateEmailIgnoresCaseAndOuterSpaces()
    {
        using var client = factory.CreateClient();
        var localPart = $"case-{Guid.NewGuid():N}";
        var firstEmail = $"  {localPart.ToUpperInvariant()}@Example.Test  ";
        var duplicateEmail = $" {localPart}@example.test ";
        var password = CreatePassword();

        using var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegistrationPayload("Ana Example", firstEmail, password, password));
        using var duplicateResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegistrationPayload("Another Name", duplicateEmail, password, password));

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        await AssertEmailConflictAsync(duplicateResponse);
        Assert.Equal(
            1,
            await CountUsersByNormalizedEmailAsync(duplicateEmail.Trim().ToUpperInvariant()));
    }

    [Fact]
    public async Task ConcurrentDuplicateEmailReturnsOneCreatedAndOneConflict()
    {
        var barrier = new RegistrationSaveBarrier();
        using var raceFactory = ApiFactory.WithInterceptor(barrier);
        using var client = raceFactory.CreateClient();
        var email = CreateEmail();
        var password = CreatePassword();
        var payload = new RegistrationPayload("Ana Example", email, password, password);

        var requests = new[]
        {
            client.PostAsJsonAsync("/api/auth/register", payload),
            client.PostAsJsonAsync("/api/auth/register", payload)
        };
        var responses = await Task.WhenAll(requests);

        try
        {
            Assert.Equal(
                [HttpStatusCode.Created, HttpStatusCode.Conflict],
                responses.Select(response => response.StatusCode).Order());
            Assert.Equal(2, barrier.Arrivals);
            using var scope = raceFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
            Assert.Equal(
                1,
                await dbContext.Users.CountAsync(
                    user => user.NormalizedEmail == email.ToUpperInvariant()));
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task MalformedJsonReturnsValidationProblemDetails()
    {
        using var client = factory.CreateClient();
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/auth/register", content);

        await AssertValidationProblemAsync(response);
    }

    [Fact]
    public async Task UnsupportedMediaTypeReturnsProblemDetails()
    {
        using var client = factory.CreateClient();
        using var content = new StringContent("not-json", Encoding.UTF8, "text/plain");

        using var response = await client.PostAsync("/api/auth/register", content);

        await AssertProblemDetailsAsync(response, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task UnsupportedMethodReturnsProblemDetails()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/auth/register");

        await AssertProblemDetailsAsync(response, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task DatabaseFailureReturnsSafeProblemDetails()
    {
        using var failureFactory = ApiFactory.WithDatabaseTimeout(1);
        using var client = failureFactory.CreateClient();
        using var healthyResponse = await client.GetAsync("/health");
        healthyResponse.EnsureSuccessStatusCode();

        await using var lockConnection = new SqliteConnection(
            $"Data Source={failureFactory.DatabasePath};Default Timeout=1;Pooling=False");
        await lockConnection.OpenAsync();
        await AcquireExclusiveLockAsync(lockConnection);

        var password = CreatePassword();
        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegistrationPayload("Ana Example", CreateEmail(), password, password));
            stopwatch.Stop();
        }
        finally
        {
            await using var rollbackCommand = lockConnection.CreateCommand();
            rollbackCommand.CommandText = "ROLLBACK;";
            await rollbackCommand.ExecuteNonQueryAsync();
        }

        using (response)
        {
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);
            var responseText = await response.Content.ReadAsStringAsync();
            using var body = JsonDocument.Parse(responseText);
            Assert.Equal(500, body.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("/api/auth/register", body.RootElement.GetProperty("instance").GetString());
            Assert.DoesNotContain(password, responseText, StringComparison.Ordinal);
            Assert.DoesNotContain("SQLite", responseText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SELECT", responseText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UserProfileDbContext", responseText, StringComparison.Ordinal);
            Assert.True(
                stopwatch.Elapsed < TimeSpan.FromSeconds(5),
                $"Database failure took {stopwatch.Elapsed} despite its one-second timeout.");
        }
    }

    private async Task<int> CountUsersAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        return await dbContext.Users.CountAsync();
    }

    private async Task<int> CountUsersByNormalizedEmailAsync(string normalizedEmail)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        return await dbContext.Users.CountAsync(user => user.NormalizedEmail == normalizedEmail);
    }

    private static async Task AssertValidationProblemAsync(
        HttpResponseMessage response,
        string? expectedField = null)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
        var errors = body.RootElement.GetProperty("errors");
        Assert.NotEmpty(errors.EnumerateObject());
        if (expectedField is not null)
        {
            Assert.True(
                errors.TryGetProperty(expectedField, out var fieldErrors),
                $"Expected validation errors for '{expectedField}', but received {errors}.");
            Assert.NotEmpty(fieldErrors.EnumerateArray());
        }
    }

    private static async Task AssertEmailConflictAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(409, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Conflict", body.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "An account with this email already exists.",
            body.RootElement.GetProperty("detail").GetString());
        Assert.Equal("/api/auth/register", body.RootElement.GetProperty("instance").GetString());
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal((int)expectedStatus, body.RootElement.GetProperty("status").GetInt32());
        Assert.True(body.RootElement.TryGetProperty("title", out _));
        Assert.Equal("/api/auth/register", body.RootElement.GetProperty("instance").GetString());
    }

    private static Dictionary<string, object?> CreateInvalidPayload(string scenario)
    {
        var password = CreatePassword();
        var payload = new Dictionary<string, object?>
        {
            ["name"] = "Ana Example",
            ["email"] = CreateEmail(),
            ["password"] = password,
            ["passwordConfirmation"] = password
        };

        switch (scenario)
        {
            case "missing-name":
                payload.Remove("name");
                break;
            case "short-name-after-trim":
                payload["name"] = "  ab  ";
                break;
            case "long-name":
                payload["name"] = new string('n', 201);
                break;
            case "missing-email":
                payload.Remove("email");
                break;
            case "invalid-email":
                payload["email"] = "not-an-email";
                break;
            case "email-without-domain-dot":
                payload["email"] = "ana@example";
                break;
            case "email-with-inner-space":
                payload["email"] = "ana @example.test";
                break;
            case "long-email":
                payload["email"] = $"{new string('a', 309)}@example.test";
                break;
            case "missing-password":
                payload.Remove("password");
                break;
            case "short-password":
                payload["password"] = "12345";
                payload["passwordConfirmation"] = "12345";
                break;
            case "long-password":
                payload["password"] = new string('p', 129);
                payload["passwordConfirmation"] = new string('p', 129);
                break;
            case "missing-confirmation":
                payload.Remove("passwordConfirmation");
                break;
            case "long-confirmation":
                payload["passwordConfirmation"] = new string('p', 129);
                break;
            case "different-confirmation":
                payload["passwordConfirmation"] = $"different-{Guid.NewGuid():N}";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }

        return payload;
    }

    private static async Task AcquireExclusiveLockAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout=1000;";
        await command.ExecuteNonQueryAsync();
        command.CommandText = "PRAGMA journal_mode=DELETE;";
        await command.ExecuteScalarAsync();
        command.CommandText = "PRAGMA locking_mode=EXCLUSIVE;";
        await command.ExecuteScalarAsync();
        command.CommandText = "BEGIN EXCLUSIVE;";
        await command.ExecuteNonQueryAsync();
        command.CommandText = """
            UPDATE "__EFMigrationsHistory"
            SET "ProductVersion" = "ProductVersion" || '-registration-lock';
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static string CreateEmail() => $"user-{Guid.NewGuid():N}@example.test";

    private static string CreatePassword() => $"Test!{Guid.NewGuid():N}";

    private sealed class RegistrationSaveBarrier : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource<bool> bothRequestsArrived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int arrivals;

        public int Arrivals => Volatile.Read(ref arrivals);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<User>()
                    .Any(entry => entry.State == EntityState.Added) != true)
            {
                return result;
            }

            if (Interlocked.Increment(ref arrivals) == 2)
            {
                bothRequestsArrived.TrySetResult(true);
            }

            await bothRequestsArrived.Task.WaitAsync(
                TimeSpan.FromSeconds(10),
                cancellationToken);
            return result;
        }
    }

    private sealed record RegistrationPayload(
        string Name,
        string Email,
        string Password,
        string PasswordConfirmation);
}
