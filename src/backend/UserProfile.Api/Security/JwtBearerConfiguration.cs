using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using UserProfile.Api.Configuration;

namespace UserProfile.Api.Security;

public static class JwtBearerConfiguration
{
    public static void Configure(
        JwtBearerOptions bearerOptions,
        JwtOptions jwtOptions,
        TimeProvider timeProvider)
    {
        bearerOptions.MapInboundClaims = false;
        bearerOptions.TimeProvider = timeProvider;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtOptions.SigningKey.ToArray()),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            RequireSignedTokens = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            LifetimeValidator = (notBefore, expires, _, parameters) =>
            {
                if (expires is null)
                {
                    return false;
                }

                var now = timeProvider.GetUtcNow().UtcDateTime;
                return (notBefore is null || notBefore <= now + parameters.ClockSkew) &&
                    expires >= now - parameters.ClockSkew;
            }
        };
        bearerOptions.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (!HasValidRequiredClaims(context.Principal))
                {
                    context.Fail("The token does not contain valid required claims.");
                }

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Bearer";

                var problemDetailsService = context.HttpContext.RequestServices
                    .GetRequiredService<IProblemDetailsService>();
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Title = "Unauthorized",
                        Status = StatusCodes.Status401Unauthorized,
                        Detail = "A valid Bearer token is required.",
                        Instance = context.Request.Path
                    }
                });
            }
        };
    }

    private static bool HasValidRequiredClaims(System.Security.Claims.ClaimsPrincipal? principal)
    {
        if (!TryGetSingleClaim(principal, JwtRegisteredClaimNames.Sub, out var subject))
        {
            return false;
        }

        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
        {
            return false;
        }

        if (!TryGetSingleClaim(principal, JwtRegisteredClaimNames.Jti, out var jwtId))
        {
            return false;
        }

        if (!Guid.TryParse(jwtId, out var parsedJwtId) || parsedJwtId == Guid.Empty)
        {
            return false;
        }

        if (!TryGetPositiveUnixTimestamp(principal, JwtRegisteredClaimNames.Iat, out var issuedAt))
        {
            return false;
        }

        return TryGetPositiveUnixTimestamp(principal, JwtRegisteredClaimNames.Exp, out var expiresAt) &&
            expiresAt > issuedAt;
    }

    private static bool TryGetPositiveUnixTimestamp(
        System.Security.Claims.ClaimsPrincipal? principal,
        string claimType,
        out long value)
    {
        value = 0;
        if (!TryGetSingleClaim(principal, claimType, out var claimValue))
        {
            return false;
        }

        return long.TryParse(
            claimValue,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out value) &&
            value > 0;
    }

    private static bool TryGetSingleClaim(
        System.Security.Claims.ClaimsPrincipal? principal,
        string claimType,
        out string value)
    {
        // Stryker disable once String: callers ignore this output on false and success overwrites it.
        value = string.Empty;
        if (principal is null)
        {
            return false;
        }

        var claims = principal.FindAll(claimType).Take(2).ToArray();
        if (claims.Length != 1 || string.IsNullOrWhiteSpace(claims[0].Value))
        {
            return false;
        }

        value = claims[0].Value;
        return true;
    }
}
