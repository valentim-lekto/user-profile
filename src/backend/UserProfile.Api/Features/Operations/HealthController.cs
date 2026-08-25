using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UserProfile.Api.Features.Operations;

[ApiController]
[Route("health")]
public sealed class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet(Name = "getHealth")]
    [Tags("Operations")]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);

        if (report.Status == HealthStatus.Healthy)
        {
            return Ok(new HealthResponse(HealthState.Healthy));
        }

        return Problem(
            detail: "The service is not ready.",
            instance: HttpContext.Request.Path,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Service Unavailable");
    }
}
