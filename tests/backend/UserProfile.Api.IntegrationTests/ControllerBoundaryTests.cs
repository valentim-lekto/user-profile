using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using UserProfile.Api.Data;
using UserProfile.Api.Features.Auth;
using UserProfile.Api.Features.Profile;
using UserProfile.Api.IntegrationTests.Infrastructure;
using UserProfile.Api.Security;

namespace UserProfile.Api.IntegrationTests;

public sealed class ControllerBoundaryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task AuthControllerReturnsTheExactGenericProblemForUnknownEmail()
    {
        using var client = factory.CreateClient();
        using var healthyResponse = await client.GetAsync("/health");
        healthyResponse.EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var controller = new AuthController(
            scope.ServiceProvider.GetRequiredService<UserProfileDbContext>(),
            scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>(),
            scope.ServiceProvider.GetRequiredService<TimeProvider>(),
            scope.ServiceProvider.GetRequiredService<JwtTokenIssuer>());
        var httpContext = CreateHttpContext(scope.ServiceProvider, "/api/auth/login");
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var registration = await controller.Register(
            new RegisterRequest
            {
                Name = "Mutation Registration",
                Email = $"mutation-{Guid.NewGuid():N}@example.test",
                Password = "SyntheticPassword!",
                PasswordConfirmation = "SyntheticPassword!"
            },
            CancellationToken.None);
        var registrationResult = Assert.IsType<ObjectResult>(registration.Result);
        Assert.Equal(StatusCodes.Status201Created, registrationResult.StatusCode);
        Assert.Equal(
            "Registration completed successfully.",
            Assert.IsType<MessageResponse>(registrationResult.Value).Message);

        var action = await controller.Login(
            new LoginRequest
            {
                Email = $"unknown-{Guid.NewGuid():N}@example.test",
                Password = "SyntheticPassword!"
            },
            CancellationToken.None);

        var result = Assert.IsType<ObjectResult>(action.Result);
        AssertProblem(
            result,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "Invalid email or password.",
            "/api/auth/login");
        Assert.Contains("application/problem+json", result.ContentTypes);
        Assert.Equal("about:blank", Assert.IsType<ProblemDetails>(result.Value).Type);
        Assert.Equal("Bearer", httpContext.Response.Headers.WWWAuthenticate);
    }

    [Fact]
    public async Task ProfileControllerRejectsAnEmptySubjectBeforeDatabaseAccess()
    {
        using var client = factory.CreateClient();
        using var healthyResponse = await client.GetAsync("/health");
        healthyResponse.EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();

        var getController = CreateProfileController(scope.ServiceProvider, "/api/profile");
        var getAction = await getController.GetCurrent(CancellationToken.None);
        AssertBearerUnauthorized(Assert.IsType<ObjectResult>(getAction.Result), getController);

        var updateController = CreateProfileController(scope.ServiceProvider, "/api/profile");
        var updateAction = await updateController.UpdateCurrent(
            new UpdateProfileRequest
            {
                Name = "Updated User",
                Email = $"updated-{Guid.NewGuid():N}@example.test"
            },
            CancellationToken.None);
        AssertBearerUnauthorized(Assert.IsType<ObjectResult>(updateAction.Result), updateController);

        var passwordController = CreateProfileController(
            scope.ServiceProvider,
            "/api/profile/password");
        var passwordAction = await passwordController.ChangeCurrentPassword(
            new ChangePasswordRequest
            {
                CurrentPassword = "CurrentPassword!",
                NewPassword = "NewPassword!",
                NewPasswordConfirmation = "NewPassword!"
            },
            CancellationToken.None);
        AssertBearerUnauthorized(
            Assert.IsType<ObjectResult>(passwordAction.Result),
            passwordController);
    }

    private static ProfileController CreateProfileController(
        IServiceProvider serviceProvider,
        string requestPath)
    {
        var controller = new ProfileController(
            serviceProvider.GetRequiredService<UserProfileDbContext>(),
            serviceProvider.GetRequiredService<IPasswordHasher<User>>(),
            serviceProvider.GetRequiredService<TimeProvider>());
        var context = CreateHttpContext(serviceProvider, requestPath);
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, Guid.Empty.ToString())],
            "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    private static DefaultHttpContext CreateHttpContext(
        IServiceProvider serviceProvider,
        string requestPath)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        context.Request.Path = requestPath;
        return context;
    }

    private static void AssertBearerUnauthorized(
        ObjectResult result,
        ControllerBase controller)
    {
        AssertProblem(
            result,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "A valid Bearer token is required.",
            controller.HttpContext.Request.Path);
        Assert.Equal("Bearer", controller.Response.Headers.WWWAuthenticate);
    }

    private static void AssertProblem(
        ObjectResult result,
        int expectedStatus,
        string expectedTitle,
        string expectedDetail,
        string expectedInstance)
    {
        Assert.Equal(expectedStatus, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal(expectedTitle, problem.Title);
        Assert.Equal(expectedDetail, problem.Detail);
        Assert.Equal(expectedInstance, problem.Instance);
    }
}
