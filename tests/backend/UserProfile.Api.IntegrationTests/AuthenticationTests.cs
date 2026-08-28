using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using UserProfile.Api.Data;
using UserProfile.Api.IntegrationTests.Infrastructure;

namespace UserProfile.Api.IntegrationTests;

public sealed class AuthenticationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task ValidLoginReturnsShortMinimalJwtWithoutSensitiveDataOrRefreshToken()
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(client, "Ana Example");

        using var response = await LoginAsync(client, account.Email, account.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var responseText = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(responseText);
        var responseProperty = Assert.Single(body.RootElement.EnumerateObject());
        Assert.Equal("accessToken", responseProperty.Name);
        var accessToken = Assert.IsType<string>(responseProperty.Value.GetString());
        Assert.DoesNotContain("refresh", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("normalizedEmail", responseText, StringComparison.OrdinalIgnoreCase);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
        Assert.Equal(factory.JwtIssuer, token.Issuer);
        Assert.Equal([factory.JwtAudience], token.Audiences);
        Assert.Equal(
            ["aud", "exp", "iat", "iss", "jti", "sub"],
            token.Payload.Keys.Order());
        Assert.Equal(account.Id.ToString(), token.Subject);
        Assert.True(Guid.TryParse(token.Id, out var jwtId));
        Assert.NotEqual(Guid.Empty, jwtId);
        var issuedAt = Convert.ToInt64(
            token.Payload[JwtRegisteredClaimNames.Iat],
            CultureInfo.InvariantCulture);
        var expiresAt = Convert.ToInt64(
            token.Payload[JwtRegisteredClaimNames.Exp],
            CultureInfo.InvariantCulture);
        Assert.Equal(factory.UtcNow.ToUnixTimeSeconds(), issuedAt);
        Assert.Equal(900L, expiresAt - issuedAt);

        using var profileResponse = await GetProfileAsync(client, accessToken);
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }

    [Fact]
    public async Task LoginUsesRegistrationEmailNormalization()
    {
        using var client = factory.CreateClient();
        var localPart = $"mixed-{Guid.NewGuid():N}";
        var password = CreatePassword();
        var registeredEmail = $"  {localPart.ToUpperInvariant()}@Example.Test  ";
        var account = await RegisterAsync(
            client,
            "Ana Example",
            registeredEmail,
            password);

        using var response = await LoginAsync(
            client,
            $" {localPart}@example.test ",
            password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, account.Id);
    }

    [Fact]
    public async Task UnknownEmailAndWrongPasswordReturnByteIdenticalUnauthorizedProblemDetails()
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(client, "Ana Example");

        using var unknownEmailResponse = await LoginAsync(
            client,
            $"unknown-{Guid.NewGuid():N}@example.test",
            CreatePassword());
        using var wrongPasswordResponse = await LoginAsync(
            client,
            account.Email,
            CreatePassword());

        var unknownBody = await unknownEmailResponse.Content.ReadAsByteArrayAsync();
        var wrongBody = await wrongPasswordResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(unknownBody, wrongBody);
        await AssertUnauthorizedProblemAsync(
            unknownEmailResponse,
            "Invalid email or password.",
            "/api/auth/login");
        await AssertUnauthorizedProblemAsync(
            wrongPasswordResponse,
            "Invalid email or password.",
            "/api/auth/login");
    }

    [Theory]
    [InlineData("missing-email", "email")]
    [InlineData("invalid-email", "email")]
    [InlineData("long-email", "email")]
    [InlineData("missing-password", "password")]
    [InlineData("long-password", "password")]
    public async Task InvalidLoginPayloadReturnsValidationProblemDetails(
        string scenario,
        string expectedField)
    {
        using var client = factory.CreateClient();
        var payload = CreateInvalidLoginPayload(scenario);

        using var response = await client.PostAsJsonAsync("/api/auth/login", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty(expectedField, out _));
    }

    [Fact]
    public async Task ProfileWithoutTokenReturnsBearerUnauthorizedProblemDetails()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/profile");

        await AssertUnauthorizedProblemAsync(response, "A valid Bearer token is required.");
    }

    [Fact]
    public async Task InvalidSignatureReturnsBearerUnauthorizedProblemDetails()
    {
        using var client = factory.CreateClient();
        var token = JwtTestTokenFactory.Create(
            factory,
            Guid.NewGuid(),
            signingKey: RandomNumberGenerator.GetBytes(32));

        using var response = await GetProfileAsync(client, token);

        await AssertUnauthorizedProblemAsync(response, "A valid Bearer token is required.");
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("algorithm")]
    [InlineData("expired")]
    public async Task InvalidTokenMetadataReturnsBearerUnauthorizedProblemDetails(string scenario)
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var token = scenario switch
        {
            "issuer" => JwtTestTokenFactory.Create(
                factory,
                userId,
                issuer: "Unexpected.Issuer"),
            "audience" => JwtTestTokenFactory.Create(
                factory,
                userId,
                audience: "Unexpected.Audience"),
            "algorithm" => JwtTestTokenFactory.Create(
                factory,
                userId,
                algorithm: SecurityAlgorithms.HmacSha384),
            "expired" => JwtTestTokenFactory.Create(
                factory,
                userId,
                issuedAt: factory.UtcNow.AddMinutes(-2),
                expiresAt: factory.UtcNow.AddSeconds(-31)),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

        using var response = await GetProfileAsync(client, token);

        await AssertUnauthorizedProblemAsync(response, "A valid Bearer token is required.");
    }

    [Fact]
    public async Task TokenExpiredWithinClockSkewStillAuthenticates()
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(client, "Clock Skew User");
        var token = JwtTestTokenFactory.Create(
            factory,
            account.Id,
            issuedAt: factory.UtcNow.AddMinutes(-15),
            expiresAt: factory.UtcNow.AddSeconds(-29));

        using var response = await GetProfileAsync(client, token);

        await AssertProfileAsync(response, account);
    }

    [Theory]
    [InlineData(JwtRegisteredClaimNames.Sub)]
    [InlineData(JwtRegisteredClaimNames.Jti)]
    [InlineData(JwtRegisteredClaimNames.Iat)]
    [InlineData(JwtRegisteredClaimNames.Exp)]
    public async Task MissingRequiredClaimReturnsBearerUnauthorizedProblemDetails(string claim)
    {
        using var client = factory.CreateClient();
        var token = JwtTestTokenFactory.Create(
            factory,
            Guid.NewGuid(),
            omittedClaims: [claim]);

        using var response = await GetProfileAsync(client, token);

        await AssertUnauthorizedProblemAsync(response, "A valid Bearer token is required.");
    }

    [Theory]
    [InlineData(JwtRegisteredClaimNames.Sub, "not-a-guid")]
    [InlineData(JwtRegisteredClaimNames.Sub, "00000000-0000-0000-0000-000000000000")]
    [InlineData(JwtRegisteredClaimNames.Jti, "not-a-guid")]
    [InlineData(JwtRegisteredClaimNames.Iat, "not-a-number")]
    [InlineData(JwtRegisteredClaimNames.Exp, "not-a-number")]
    public async Task MalformedRequiredClaimReturnsBearerUnauthorizedProblemDetails(
        string claim,
        string value)
    {
        using var client = factory.CreateClient();
        var token = JwtTestTokenFactory.Create(
            factory,
            Guid.NewGuid(),
            overriddenClaims: new Dictionary<string, object> { [claim] = value });

        using var response = await GetProfileAsync(client, token);

        await AssertUnauthorizedProblemAsync(response, "A valid Bearer token is required.");
    }

    [Fact]
    public async Task ValidSubjectWithoutCurrentUserReturnsNotFoundProblemDetails()
    {
        using var client = factory.CreateClient();
        var token = JwtTestTokenFactory.Create(factory, Guid.NewGuid());

        using var response = await GetProfileAsync(client, token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", body.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "The current profile could not be found.",
            body.RootElement.GetProperty("detail").GetString());
        Assert.Equal("/api/profile", body.RootElement.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task ProfileUsesOnlySubjectAndReturnsExactlyIdNameAndEmail()
    {
        using var client = factory.CreateClient();
        var first = await RegisterAsync(client, "First User");
        var second = await RegisterAsync(client, "Second User");
        var firstToken = await GetAccessTokenAsync(client, first.Email, first.Password);
        var secondToken = await GetAccessTokenAsync(client, second.Email, second.Password);

        using var firstRequest = CreateProfileRequest(
            firstToken,
            $"/api/profile?userId={second.Id}");
        firstRequest.Headers.Add("X-User-Id", second.Id.ToString());
        using var firstResponse = await client.SendAsync(firstRequest);
        using var secondResponse = await GetProfileAsync(client, secondToken);

        await AssertProfileAsync(firstResponse, first);
        await AssertProfileAsync(secondResponse, second);
    }

    private async Task<Account> RegisterAsync(
        HttpClient client,
        string name,
        string? email = null,
        string? password = null)
    {
        email ??= $"user-{Guid.NewGuid():N}@example.test";
        password ??= CreatePassword();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                name,
                email,
                password,
                passwordConfirmation = password
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(candidate => candidate.NormalizedEmail == normalizedEmail);
        return new Account(user.Id, user.Name, user.Email, password);
    }

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        return client.PostAsJsonAsync("/api/auth/login", new { email, password });
    }

    private static async Task<string> GetAccessTokenAsync(
        HttpClient client,
        string email,
        string password)
    {
        using var response = await LoginAsync(client, email, password);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static Task<HttpResponseMessage> GetProfileAsync(HttpClient client, string accessToken)
    {
        return client.SendAsync(CreateProfileRequest(accessToken, "/api/profile"));
    }

    private static HttpRequestMessage CreateProfileRequest(string accessToken, string requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static async Task AssertUnauthorizedProblemAsync(
        HttpResponseMessage response,
        string expectedDetail,
        string expectedInstance = "/api/profile")
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(401, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Unauthorized", body.RootElement.GetProperty("title").GetString());
        Assert.Equal(expectedDetail, body.RootElement.GetProperty("detail").GetString());
        Assert.Equal(expectedInstance, body.RootElement.GetProperty("instance").GetString());
    }

    private static async Task AssertProfileAsync(HttpResponseMessage response, Account expected)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var responseText = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(responseText);
        Assert.Equal(
            ["email", "id", "name"],
            body.RootElement.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal(expected.Id, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(expected.Name, body.RootElement.GetProperty("name").GetString());
        Assert.Equal(expected.Email, body.RootElement.GetProperty("email").GetString());
        Assert.DoesNotContain("password", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("normalizedEmail", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdAt", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("updatedAt", responseText, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object?> CreateInvalidLoginPayload(string scenario)
    {
        var payload = new Dictionary<string, object?>
        {
            ["email"] = $"user-{Guid.NewGuid():N}@example.test",
            ["password"] = CreatePassword()
        };
        switch (scenario)
        {
            case "missing-email":
                payload.Remove("email");
                break;
            case "invalid-email":
                payload["email"] = "not-an-email";
                break;
            case "long-email":
                payload["email"] = $"{new string('a', 309)}@example.test";
                break;
            case "missing-password":
                payload.Remove("password");
                break;
            case "long-password":
                payload["password"] = new string('p', 129);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }

        return payload;
    }

    private static string CreatePassword() => $"Test!{Guid.NewGuid():N}";

    private sealed record Account(Guid Id, string Name, string Email, string Password);
}
