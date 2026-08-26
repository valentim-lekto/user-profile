using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserProfile.Api.Data;

namespace UserProfile.Api.Features.Profile;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(UserProfileDbContext dbContext) : ControllerBase
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
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
        {
            Response.Headers.WWWAuthenticate = "Bearer";
            return Problem(
                detail: "A valid Bearer token is required.",
                instance: HttpContext.Request.Path,
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
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
}
