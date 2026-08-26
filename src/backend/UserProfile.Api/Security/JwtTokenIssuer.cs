using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using UserProfile.Api.Configuration;

namespace UserProfile.Api.Security;

public sealed class JwtTokenIssuer(JwtOptions options, TimeProvider timeProvider)
{
    private readonly SigningCredentials signingCredentials = new(
        new SymmetricSecurityKey(options.SigningKey.ToArray()),
        SecurityAlgorithms.HmacSha256);

    public string Issue(Guid userId)
    {
        var issuedAt = timeProvider.GetUtcNow();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: null,
            expires: issuedAt.Add(options.Lifetime).UtcDateTime,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
