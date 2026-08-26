using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace UserProfile.Api.IntegrationTests.Infrastructure;

internal static class JwtTestTokenFactory
{
    public static string Create(
        ApiFactory factory,
        Guid subject,
        string? issuer = null,
        string? audience = null,
        DateTimeOffset? issuedAt = null,
        DateTimeOffset? expiresAt = null,
        byte[]? signingKey = null,
        string algorithm = SecurityAlgorithms.HmacSha256,
        IReadOnlyCollection<string>? omittedClaims = null,
        IReadOnlyDictionary<string, object>? overriddenClaims = null)
    {
        var issued = issuedAt ?? factory.UtcNow;
        var expires = expiresAt ?? issued.AddMinutes(15);
        var payload = new JwtPayload
        {
            [JwtRegisteredClaimNames.Iss] = issuer ?? factory.JwtIssuer,
            [JwtRegisteredClaimNames.Aud] = audience ?? factory.JwtAudience,
            [JwtRegisteredClaimNames.Sub] = subject.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            [JwtRegisteredClaimNames.Iat] = issued.ToUnixTimeSeconds(),
            [JwtRegisteredClaimNames.Exp] = expires.ToUnixTimeSeconds()
        };

        if (omittedClaims is not null)
        {
            foreach (var claim in omittedClaims)
            {
                payload.Remove(claim);
            }
        }

        if (overriddenClaims is not null)
        {
            foreach (var claim in overriddenClaims)
            {
                payload[claim.Key] = claim.Value;
            }
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(signingKey ?? factory.JwtSigningKey),
            algorithm);
        return new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(new JwtHeader(credentials), payload));
    }
}
