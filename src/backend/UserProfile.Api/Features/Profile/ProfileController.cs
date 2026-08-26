using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UserProfile.Api.Data;
using UserProfile.Api.Features.Auth;

namespace UserProfile.Api.Features.Profile;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(
    UserProfileDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet(Name = "getCurrentProfile")]
    [Tags("Profile")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<ProfileResponse>> GetCurrent(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return BearerUnauthorized();
        }

        var profile = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new ProfileResponse(user.Id, user.Name, user.Email))
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return Problem(
                detail: "The current profile could not be found.",
                instance: HttpContext.Request.Path,
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return Ok(profile);
    }

    [HttpPut(Name = "updateCurrentProfile")]
    [Tags("Profile")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<ProfileResponse>> UpdateCurrent(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return BearerUnauthorized();
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken);
        if (user is null)
        {
            return CurrentProfileNotFound();
        }

        var normalizedEmail = request.Email!.ToUpperInvariant();
        if (await dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    candidate => candidate.Id != userId &&
                        candidate.NormalizedEmail == normalizedEmail,
                    cancellationToken))
        {
            return EmailConflict();
        }

        user.Name = request.Name!;
        user.Email = request.Email!;
        user.NormalizedEmail = normalizedEmail;
        user.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return EmailConflict();
        }

        return Ok(new ProfileResponse(user.Id, user.Name, user.Email));
    }

    [HttpPut("password", Name = "changeCurrentPassword")]
    [Tags("Profile")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<MessageResponse>> ChangeCurrentPassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return BearerUnauthorized();
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken);
        if (user is null)
        {
            return CurrentProfileNotFound();
        }

        if (passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.CurrentPassword!) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("currentPassword", "Current password is incorrect.");
            return ValidationProblem(
                detail: "Check the errors object for details.",
                instance: HttpContext.Request.Path,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                type: "about:blank",
                modelStateDictionary: ModelState);
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword!);
        user.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new MessageResponse("Password changed successfully."));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(subject, out userId) && userId != Guid.Empty;
    }

    private ObjectResult BearerUnauthorized()
    {
        Response.Headers.WWWAuthenticate = "Bearer";
        return Problem(
            detail: "A valid Bearer token is required.",
            instance: HttpContext.Request.Path,
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized");
    }

    private ObjectResult CurrentProfileNotFound()
    {
        return Problem(
            detail: "The current profile could not be found.",
            instance: HttpContext.Request.Path,
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found");
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
