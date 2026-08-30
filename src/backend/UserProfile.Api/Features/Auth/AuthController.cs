using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UserProfile.Api.Data;
using UserProfile.Api.Security;

namespace UserProfile.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserProfileDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider,
    JwtTokenIssuer jwtTokenIssuer) : ControllerBase
{
    private static readonly User InvalidCredentialsUser = new();
    private static readonly string InvalidCredentialsPasswordHash =
        CreateInvalidCredentialsPasswordHash();

    [HttpPost("register", Name = "registerUser")]
    [Tags("Auth")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<MessageResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email!.ToUpperInvariant();

        if (await dbContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return EmailConflict();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name!,
            Email = request.Email!,
            NormalizedEmail = normalizedEmail,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return EmailConflict();
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new MessageResponse("Registration completed successfully."));
    }

    [HttpPost("login", Name = "loginUser")]
    [Tags("Auth")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email!.ToUpperInvariant();
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail,
                cancellationToken);
        var passwordVerification = passwordHasher.VerifyHashedPassword(
            user ?? InvalidCredentialsUser,
            user?.PasswordHash ?? InvalidCredentialsPasswordHash,
            request.Password!);

        if (user is null || passwordVerification == PasswordVerificationResult.Failed)
        {
            return InvalidCredentials();
        }

        return Ok(new LoginResponse(jwtTokenIssuer.Issue(user.Id)));
    }

    private static string CreateInvalidCredentialsPasswordHash()
    {
        var syntheticPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return new PasswordHasher<User>().HashPassword(
            InvalidCredentialsUser,
            syntheticPassword);
    }

    private ObjectResult InvalidCredentials()
    {
        Response.Headers.WWWAuthenticate = "Bearer";
        var result = new ObjectResult(new ProblemDetails
        {
            Type = "about:blank",
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Invalid email or password.",
            Instance = HttpContext.Request.Path
        })
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    private ObjectResult EmailConflict()
    {
        return Problem(
            detail: "An account with this email already exists.",
            instance: HttpContext.Request.Path,
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflict");
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.GetBaseException() is SqliteException
        {
            SqliteErrorCode: 19,
            SqliteExtendedErrorCode: 2067
        };
    }
}
