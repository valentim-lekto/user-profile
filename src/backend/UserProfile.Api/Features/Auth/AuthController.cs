using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UserProfile.Api.Data;

namespace UserProfile.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserProfileDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpPost("register", Name = "registerUser")]
    [Tags("Auth")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
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
