using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using UserProfile.Api.Data;
using UserProfile.Api.IntegrationTests.Infrastructure;

namespace UserProfile.Api.IntegrationTests;

public sealed class ProfileUpdateTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task ValidProfileUpdateTrimsNormalizesPersistsAndReturnsSafeProfile()
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(factory, client, "Original User");
        var before = await LoadUserAsync(factory, account.Id);
        var token = await GetAccessTokenAsync(client, account.Email, account.Password);
        var updatedEmail = $"  UPDATED-{Guid.NewGuid():N}@Example.Test  ";
        factory.AdvanceTime(TimeSpan.FromMinutes(1));

        using var response = await PutProfileAsync(
            client,
            token,
            new { name = "  Updated User  ", email = updatedEmail });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var responseText = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(responseText);
        Assert.Equal(
            ["email", "id", "name"],
            body.RootElement.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal(account.Id, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Updated User", body.RootElement.GetProperty("name").GetString());
        Assert.Equal(updatedEmail.Trim(), body.RootElement.GetProperty("email").GetString());
        Assert.DoesNotContain("password", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("normalizedEmail", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdAt", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("updatedAt", responseText, StringComparison.OrdinalIgnoreCase);

        var after = await LoadUserAsync(factory, account.Id);
        Assert.Equal("Updated User", after.Name);
        Assert.Equal(updatedEmail.Trim(), after.Email);
        Assert.Equal(updatedEmail.Trim().ToUpperInvariant(), after.NormalizedEmail);
        Assert.Equal(before.PasswordHash, after.PasswordHash);
        Assert.Equal(before.CreatedAtUtc, after.CreatedAtUtc);
        Assert.True(after.UpdatedAtUtc > before.UpdatedAtUtc);
        Assert.Equal(factory.UtcNow.UtcDateTime, after.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(200)]
    public async Task InclusiveProfileNameBoundsAreAccepted(int nameLength)
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(factory, client, "Original User");
        var token = await GetAccessTokenAsync(client, account.Email, account.Password);
        var name = new string('n', nameLength);
        var email = CreateEmail();

        using var response = await PutProfileAsync(client, token, new { name, email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var persisted = await LoadUserAsync(factory, account.Id);
        Assert.Equal(name, persisted.Name);
        Assert.Equal(email, persisted.Email);
    }

    [Fact]
    public async Task InclusiveProfileEmailMaximumIsAccepted()
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(factory, client, "Original User");
        var token = await GetAccessTokenAsync(client, account.Email, account.Password);
        var email = $"{Guid.NewGuid():N}{new string('a', 275)}@example.test";
        Assert.Equal(320, email.Length);

        using var response = await PutProfileAsync(
            client,
            token,
            new { name = "Updated User", email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var persisted = await LoadUserAsync(factory, account.Id);
        Assert.Equal(email, persisted.Email);
        Assert.Equal(email.ToUpperInvariant(), persisted.NormalizedEmail);
    }

    [Theory]
    [InlineData("missing-name", "name")]
    [InlineData("short-name-after-trim", "name")]
    [InlineData("long-name", "name")]
    [InlineData("missing-email", "email")]
    [InlineData("invalid-email", "email")]
    [InlineData("email-without-domain-dot", "email")]
    [InlineData("email-with-inner-space", "email")]
    [InlineData("unicode-email", "email")]
    [InlineData("long-email", "email")]
    public async Task InvalidProfileUpdateReturnsValidationProblemAndPreservesUser(
        string scenario,
        string expectedField)
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(factory, client, "Original User");
        var before = await LoadUserAsync(factory, account.Id);
        var token = await GetAccessTokenAsync(client, account.Email, account.Password);

        using var response = await PutProfileAsync(
            client,
            token,
            CreateInvalidProfilePayload(scenario));

        await AssertValidationProblemAsync(response, expectedField);
        Assert.Equal(before, await LoadUserAsync(factory, account.Id));
    }

    [Fact]
    public async Task DuplicateEmailConflictsWithoutPartialUpdateAndOwnEmailIsAllowed()
    {
        using var client = factory.CreateClient();
        var first = await RegisterAsync(factory, client, "First User");
        var second = await RegisterAsync(factory, client, "Second User");
        var firstToken = await GetAccessTokenAsync(client, first.Email, first.Password);
        var beforeConflict = await LoadUserAsync(factory, first.Id);
        var secondBefore = await LoadUserAsync(factory, second.Id);

        using var conflictResponse = await PutProfileAsync(
            client,
            firstToken,
            new
            {
                name = "Should Not Persist",
                email = $"  {second.Email.ToUpperInvariant()}  "
            });

        await AssertEmailConflictAsync(conflictResponse);
        Assert.Equal(beforeConflict, await LoadUserAsync(factory, first.Id));

        using var ownEmailResponse = await PutProfileAsync(
            client,
            firstToken,
            new
            {
                name = "First Updated",
                email = $"  {first.Email.ToUpperInvariant()}  "
            });

        Assert.Equal(HttpStatusCode.OK, ownEmailResponse.StatusCode);
        var afterOwnEmail = await LoadUserAsync(factory, first.Id);
        Assert.Equal("First Updated", afterOwnEmail.Name);
        Assert.Equal(first.Email.ToUpperInvariant(), afterOwnEmail.Email);
        Assert.Equal(first.Email.ToUpperInvariant(), afterOwnEmail.NormalizedEmail);
        Assert.Equal(secondBefore, await LoadUserAsync(factory, second.Id));
    }

    [Fact]
    public async Task ConcurrentProfileEmailCollisionReturnsOneSuccessAndOneConflict()
    {
        var barrier = new ProfileSaveBarrier();
        using var raceFactory = ApiFactory.WithInterceptor(barrier);
        using var client = raceFactory.CreateClient();
        var first = await RegisterAsync(raceFactory, client, "First User");
        var second = await RegisterAsync(raceFactory, client, "Second User");
        var firstBefore = await LoadUserAsync(raceFactory, first.Id);
        var secondBefore = await LoadUserAsync(raceFactory, second.Id);
        var firstToken = await GetAccessTokenAsync(client, first.Email, first.Password);
        var secondToken = await GetAccessTokenAsync(client, second.Email, second.Password);
        var targetEmail = CreateEmail();

        var responses = await Task.WhenAll(
            PutProfileAsync(
                client,
                firstToken,
                new { name = "First Winner Candidate", email = targetEmail }),
            PutProfileAsync(
                client,
                secondToken,
                new { name = "Second Winner Candidate", email = targetEmail }));

        try
        {
            Assert.Equal(
                [HttpStatusCode.OK, HttpStatusCode.Conflict],
                responses.Select(response => response.StatusCode).Order());
            Assert.Equal(2, barrier.Arrivals);

            var firstAfter = await LoadUserAsync(raceFactory, first.Id);
            var secondAfter = await LoadUserAsync(raceFactory, second.Id);
            Assert.Equal(
                1,
                new[] { firstAfter, secondAfter }.Count(
                    user => user.NormalizedEmail == targetEmail.ToUpperInvariant()));

            if (responses[0].StatusCode == HttpStatusCode.Conflict)
            {
                await AssertEmailConflictAsync(responses[0]);
                Assert.Equal(firstBefore, firstAfter);
                Assert.Equal("Second Winner Candidate", secondAfter.Name);
            }
            else
            {
                await AssertEmailConflictAsync(responses[1]);
                Assert.Equal(secondBefore, secondAfter);
                Assert.Equal("First Winner Candidate", firstAfter.Name);
            }
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
    public async Task ProfileUpdateUsesOnlySubjectAndRejectsUserIdOverposting()
    {
        using var client = factory.CreateClient();
        var first = await RegisterAsync(factory, client, "First User");
        var second = await RegisterAsync(factory, client, "Second User");
        var firstToken = await GetAccessTokenAsync(client, first.Email, first.Password);
        var firstBefore = await LoadUserAsync(factory, first.Id);
        var secondBefore = await LoadUserAsync(factory, second.Id);

        using var overpostingResponse = await PutProfileAsync(
            client,
            firstToken,
            new { name = "Overposted", email = CreateEmail(), userId = second.Id });

        await AssertValidationProblemAsync(overpostingResponse);
        Assert.Equal(firstBefore, await LoadUserAsync(factory, first.Id));
        Assert.Equal(secondBefore, await LoadUserAsync(factory, second.Id));

        var firstUpdatedEmail = CreateEmail();
        using var subjectResponse = await PutProfileAsync(
            client,
            firstToken,
            new { name = "First By Subject", email = firstUpdatedEmail },
            $"/api/profile?userId={second.Id}",
            second.Id);

        Assert.Equal(HttpStatusCode.OK, subjectResponse.StatusCode);
        var firstAfter = await LoadUserAsync(factory, first.Id);
        Assert.Equal("First By Subject", firstAfter.Name);
        Assert.Equal(firstUpdatedEmail, firstAfter.Email);
        Assert.Equal(secondBefore, await LoadUserAsync(factory, second.Id));
    }

    [Fact]
    public async Task ProfileEndpointsRequireBearerAndReturnNotFoundForMissingSubjectUser()
    {
        using var client = factory.CreateClient();
        var password = CreatePassword();
        var newPassword = CreatePassword();
        var profilePayload = new { name = "Updated User", email = CreateEmail() };
        var passwordPayload = new
        {
            currentPassword = password,
            newPassword,
            newPasswordConfirmation = newPassword
        };

        using var profileWithoutToken = await client.PutAsJsonAsync("/api/profile", profilePayload);
        using var passwordWithoutToken = await client.PutAsJsonAsync(
            "/api/profile/password",
            passwordPayload);
        await AssertUnauthorizedProblemAsync(profileWithoutToken, "/api/profile");
        await AssertUnauthorizedProblemAsync(passwordWithoutToken, "/api/profile/password");

        var invalidToken = JwtTestTokenFactory.Create(
            factory,
            Guid.NewGuid(),
            signingKey: RandomNumberGenerator.GetBytes(32));
        using var invalidProfileTokenResponse = await PutProfileAsync(
            client,
            invalidToken,
            profilePayload);
        using var invalidPasswordTokenResponse = await PutPasswordAsync(
            client,
            invalidToken,
            passwordPayload);
        await AssertUnauthorizedProblemAsync(invalidProfileTokenResponse, "/api/profile");
        await AssertUnauthorizedProblemAsync(
            invalidPasswordTokenResponse,
            "/api/profile/password");

        var missingUserToken = JwtTestTokenFactory.Create(factory, Guid.NewGuid());
        using var missingProfileResponse = await PutProfileAsync(
            client,
            missingUserToken,
            profilePayload);
        using var missingPasswordResponse = await PutPasswordAsync(
            client,
            missingUserToken,
            passwordPayload);
        await AssertNotFoundProblemAsync(missingProfileResponse, "/api/profile");
        await AssertNotFoundProblemAsync(missingPasswordResponse, "/api/profile/password");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("wrong")]
    [InlineData("long")]
    public async Task InvalidCurrentPasswordPreservesEntireUser(string scenario)
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(factory, client, "Password User");
        var before = await LoadUserAsync(factory, account.Id);
        var token = await GetAccessTokenAsync(client, account.Email, account.Password);
        var newPassword = CreatePassword();
        var payload = new Dictionary<string, object?>
        {
            ["currentPassword"] = scenario == "wrong" ? CreatePassword() : account.Password,
            ["newPassword"] = newPassword,
            ["newPasswordConfirmation"] = newPassword
        };
        if (scenario == "missing")
        {
            payload.Remove("currentPassword");
        }
        else if (scenario == "long")
        {
            payload["currentPassword"] = new string('p', 129);
        }

        using var response = await PutPasswordAsync(client, token, payload);

        var responseText = await AssertValidationProblemAsync(response, "currentPassword");
        if (scenario == "wrong")
        {
            using var body = JsonDocument.Parse(responseText);
            Assert.Equal("Bad Request", body.RootElement.GetProperty("title").GetString());
            Assert.Equal(
                "Check the errors object for details.",
                body.RootElement.GetProperty("detail").GetString());
            Assert.Equal("about:blank", body.RootElement.GetProperty("type").GetString());
            Assert.Equal(
                "Current password is incorrect.",
                Assert.Single(
                    body.RootElement
                        .GetProperty("errors")
                        .GetProperty("currentPassword")
                        .EnumerateArray())
                    .GetString());
        }
        Assert.DoesNotContain(account.Password, responseText, StringComparison.Ordinal);
        Assert.DoesNotContain(newPassword, responseText, StringComparison.Ordinal);
        Assert.Equal(before, await LoadUserAsync(factory, account.Id));
        await AssertLoginStatusAsync(client, account.Email, account.Password, HttpStatusCode.OK);
        await AssertLoginIsNotSuccessfulAsync(client, account.Email, newPassword);
    }

    [Theory]
    [InlineData("missing-new", "newPassword")]
    [InlineData("short-new", "newPassword")]
    [InlineData("long-new", "newPassword")]
    [InlineData("missing-confirmation", "newPasswordConfirmation")]
    [InlineData("short-confirmation", "newPasswordConfirmation")]
    [InlineData("long-confirmation", "newPasswordConfirmation")]
    [InlineData("different-confirmation", "newPasswordConfirmation")]
    public async Task InvalidNewPasswordPreservesEntireUserAndOldCredentials(
        string scenario,
        string expectedField)
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(factory, client, "Password User");
        var before = await LoadUserAsync(factory, account.Id);
        var token = await GetAccessTokenAsync(client, account.Email, account.Password);
        var candidatePassword = CreatePassword();
        var payload = CreateInvalidNewPasswordPayload(
            scenario,
            account.Password,
            candidatePassword);

        using var response = await PutPasswordAsync(client, token, payload);

        var responseText = await AssertValidationProblemAsync(response, expectedField);
        Assert.DoesNotContain(account.Password, responseText, StringComparison.Ordinal);
        Assert.DoesNotContain(candidatePassword, responseText, StringComparison.Ordinal);
        Assert.Equal(before, await LoadUserAsync(factory, account.Id));
        await AssertLoginStatusAsync(client, account.Email, account.Password, HttpStatusCode.OK);
        await AssertLoginIsNotSuccessfulAsync(client, account.Email, candidatePassword);
    }

    [Fact]
    public async Task CurrentPasswordAtMaximumLengthIsAccepted()
    {
        using var client = factory.CreateClient();
        var currentPassword = new string('p', 128);
        var account = await RegisterAsync(
            factory,
            client,
            "Password User",
            currentPassword);
        var token = await GetAccessTokenAsync(client, account.Email, currentPassword);
        var newPassword = CreatePassword();

        using var response = await PutPasswordAsync(
            client,
            token,
            new
            {
                currentPassword,
                newPassword,
                newPasswordConfirmation = newPassword
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertLoginStatusAsync(
            client,
            account.Email,
            currentPassword,
            HttpStatusCode.Unauthorized);
        await AssertLoginStatusAsync(client, account.Email, newPassword, HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(128)]
    public async Task ValidPasswordChangeUpdatesOnlyHashAndTimestampAndChangesCredentials(
        int newPasswordLength)
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(factory, client, "Password User");
        var before = await LoadUserAsync(factory, account.Id);
        var token = await GetAccessTokenAsync(client, account.Email, account.Password);
        var newPassword = new string(' ', newPasswordLength);
        factory.AdvanceTime(TimeSpan.FromMinutes(1));

        using var response = await PutPasswordAsync(
            client,
            token,
            new
            {
                currentPassword = account.Password,
                newPassword,
                newPasswordConfirmation = newPassword
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var responseText = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(responseText);
        var message = Assert.Single(body.RootElement.EnumerateObject());
        Assert.Equal("message", message.Name);
        Assert.Equal("Password changed successfully.", message.Value.GetString());
        Assert.DoesNotContain(account.Password, responseText, StringComparison.Ordinal);
        Assert.DoesNotContain(newPassword, responseText, StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", responseText, StringComparison.OrdinalIgnoreCase);

        var after = await LoadUserAsync(factory, account.Id);
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.Email, after.Email);
        Assert.Equal(before.NormalizedEmail, after.NormalizedEmail);
        Assert.NotEqual(before.PasswordHash, after.PasswordHash);
        Assert.Equal(before.CreatedAtUtc, after.CreatedAtUtc);
        Assert.True(after.UpdatedAtUtc > before.UpdatedAtUtc);
        Assert.Equal(factory.UtcNow.UtcDateTime, after.UpdatedAtUtc);
        await AssertLoginStatusAsync(
            client,
            account.Email,
            account.Password,
            HttpStatusCode.Unauthorized);
        await AssertLoginStatusAsync(client, account.Email, newPassword, HttpStatusCode.OK);
    }

    [Fact]
    public async Task PasswordChangeUsesOnlySubjectAndRejectsUserIdOverposting()
    {
        using var client = factory.CreateClient();
        var first = await RegisterAsync(factory, client, "First User");
        var second = await RegisterAsync(factory, client, "Second User");
        var firstToken = await GetAccessTokenAsync(client, first.Email, first.Password);
        var firstBefore = await LoadUserAsync(factory, first.Id);
        var secondBefore = await LoadUserAsync(factory, second.Id);
        var newPassword = CreatePassword();

        using var overpostingResponse = await PutPasswordAsync(
            client,
            firstToken,
            new
            {
                currentPassword = first.Password,
                newPassword,
                newPasswordConfirmation = newPassword,
                userId = second.Id
            });

        await AssertValidationProblemAsync(overpostingResponse);
        Assert.Equal(firstBefore, await LoadUserAsync(factory, first.Id));
        Assert.Equal(secondBefore, await LoadUserAsync(factory, second.Id));

        using var subjectResponse = await PutPasswordAsync(
            client,
            firstToken,
            new
            {
                currentPassword = first.Password,
                newPassword,
                newPasswordConfirmation = newPassword
            },
            $"/api/profile/password?userId={second.Id}",
            second.Id);

        Assert.Equal(HttpStatusCode.OK, subjectResponse.StatusCode);
        Assert.NotEqual(firstBefore.PasswordHash, (await LoadUserAsync(factory, first.Id)).PasswordHash);
        Assert.Equal(secondBefore, await LoadUserAsync(factory, second.Id));
        await AssertLoginStatusAsync(client, first.Email, first.Password, HttpStatusCode.Unauthorized);
        await AssertLoginStatusAsync(client, first.Email, newPassword, HttpStatusCode.OK);
        await AssertLoginStatusAsync(client, second.Email, second.Password, HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProfileRequestsRejectIncorrectJsonCasingWithoutChanges()
    {
        using var client = factory.CreateClient();
        var account = await RegisterAsync(factory, client, "Case User");
        var before = await LoadUserAsync(factory, account.Id);
        var token = await GetAccessTokenAsync(client, account.Email, account.Password);
        var newPassword = CreatePassword();

        using var profileResponse = await PutProfileAsync(
            client,
            token,
            new Dictionary<string, object?>
            {
                ["Name"] = "Incorrect Case",
                ["Email"] = CreateEmail()
            });
        using var passwordResponse = await PutPasswordAsync(
            client,
            token,
            new Dictionary<string, object?>
            {
                ["CurrentPassword"] = account.Password,
                ["NewPassword"] = newPassword,
                ["NewPasswordConfirmation"] = newPassword
            });

        await AssertValidationProblemAsync(profileResponse);
        await AssertValidationProblemAsync(passwordResponse);
        Assert.Equal(before, await LoadUserAsync(factory, account.Id));
    }

    private static Dictionary<string, object?> CreateInvalidProfilePayload(string scenario)
    {
        var payload = new Dictionary<string, object?>
        {
            ["name"] = "Updated User",
            ["email"] = CreateEmail()
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
                payload["email"] = "user@example";
                break;
            case "email-with-inner-space":
                payload["email"] = "user @example.test";
                break;
            case "unicode-email":
                payload["email"] = "usuário@example.test";
                break;
            case "long-email":
                payload["email"] = $"{new string('a', 309)}@example.test";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }

        return payload;
    }

    private static Dictionary<string, object?> CreateInvalidNewPasswordPayload(
        string scenario,
        string currentPassword,
        string candidatePassword)
    {
        var payload = new Dictionary<string, object?>
        {
            ["currentPassword"] = currentPassword,
            ["newPassword"] = candidatePassword,
            ["newPasswordConfirmation"] = candidatePassword
        };

        switch (scenario)
        {
            case "missing-new":
                payload.Remove("newPassword");
                break;
            case "short-new":
                payload["newPassword"] = "12345";
                payload["newPasswordConfirmation"] = "12345";
                break;
            case "long-new":
                payload["newPassword"] = new string('p', 129);
                payload["newPasswordConfirmation"] = new string('p', 129);
                break;
            case "missing-confirmation":
                payload.Remove("newPasswordConfirmation");
                break;
            case "short-confirmation":
                payload["newPasswordConfirmation"] = "12345";
                break;
            case "long-confirmation":
                payload["newPasswordConfirmation"] = new string('p', 129);
                break;
            case "different-confirmation":
                payload["newPasswordConfirmation"] = CreatePassword();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }

        return payload;
    }

    private static async Task<Account> RegisterAsync(
        ApiFactory sourceFactory,
        HttpClient client,
        string name,
        string? suppliedPassword = null)
    {
        var email = CreateEmail();
        var password = suppliedPassword ?? CreatePassword();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { name, email, password, passwordConfirmation = password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = sourceFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(candidate => candidate.NormalizedEmail == email.ToUpperInvariant());
        return new Account(user.Id, user.Name, user.Email, password);
    }

    private static async Task<UserSnapshot> LoadUserAsync(ApiFactory sourceFactory, Guid userId)
    {
        using var scope = sourceFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserSnapshot(
                user.Id,
                user.Name,
                user.Email,
                user.NormalizedEmail,
                user.PasswordHash,
                user.CreatedAtUtc,
                user.UpdatedAtUtc))
            .SingleAsync();
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

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        return client.PostAsJsonAsync("/api/auth/login", new { email, password });
    }

    private static async Task AssertLoginStatusAsync(
        HttpClient client,
        string email,
        string password,
        HttpStatusCode expectedStatus)
    {
        using var response = await LoginAsync(client, email, password);
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    private static async Task AssertLoginIsNotSuccessfulAsync(
        HttpClient client,
        string email,
        string password)
    {
        using var response = await LoginAsync(client, email, password);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static Task<HttpResponseMessage> PutProfileAsync(
        HttpClient client,
        string accessToken,
        object payload,
        string requestUri = "/api/profile",
        Guid? headerUserId = null)
    {
        return SendAuthorizedJsonAsync(
            client,
            accessToken,
            requestUri,
            payload,
            headerUserId);
    }

    private static Task<HttpResponseMessage> PutPasswordAsync(
        HttpClient client,
        string accessToken,
        object payload,
        string requestUri = "/api/profile/password",
        Guid? headerUserId = null)
    {
        return SendAuthorizedJsonAsync(
            client,
            accessToken,
            requestUri,
            payload,
            headerUserId);
    }

    private static async Task<HttpResponseMessage> SendAuthorizedJsonAsync(
        HttpClient client,
        string accessToken,
        string requestUri,
        object payload,
        Guid? headerUserId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (headerUserId is not null)
        {
            request.Headers.Add("X-User-Id", headerUserId.Value.ToString());
        }

        return await client.SendAsync(request);
    }

    private static async Task<string> AssertValidationProblemAsync(
        HttpResponseMessage response,
        string? expectedField = null)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var responseText = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(responseText);
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

        return responseText;
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
        Assert.Equal("/api/profile", body.RootElement.GetProperty("instance").GetString());
    }

    private static async Task AssertUnauthorizedProblemAsync(
        HttpResponseMessage response,
        string expectedInstance)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(401, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Unauthorized", body.RootElement.GetProperty("title").GetString());
        Assert.Equal("A valid Bearer token is required.", body.RootElement.GetProperty("detail").GetString());
        Assert.Equal(expectedInstance, body.RootElement.GetProperty("instance").GetString());
    }

    private static async Task AssertNotFoundProblemAsync(
        HttpResponseMessage response,
        string expectedInstance)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", body.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "The current profile could not be found.",
            body.RootElement.GetProperty("detail").GetString());
        Assert.Equal(expectedInstance, body.RootElement.GetProperty("instance").GetString());
    }

    private static string CreateEmail() => $"user-{Guid.NewGuid():N}@example.test";

    private static string CreatePassword() => $"Test!{Guid.NewGuid():N}";

    private sealed class ProfileSaveBarrier : SaveChangesInterceptor
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
                    .Any(entry => entry.State == EntityState.Modified) != true)
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

    private sealed record Account(Guid Id, string Name, string Email, string Password);

    private sealed record UserSnapshot(
        Guid Id,
        string Name,
        string Email,
        string NormalizedEmail,
        string PasswordHash,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);
}
