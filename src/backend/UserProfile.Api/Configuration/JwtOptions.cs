using System.Security.Cryptography;

namespace UserProfile.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string DefaultIssuer = "UserProfile.Api";
    public const string DefaultAudience = "UserProfile.Web";
    public const int RequiredLifetimeMinutes = 15;
    public const int MinimumSigningKeyBytes = 32;

    private JwtOptions(
        string issuer,
        string audience,
        ReadOnlyMemory<byte> signingKey)
    {
        Issuer = issuer;
        Audience = audience;
        SigningKey = signingKey;
    }

    public string Issuer { get; }

    public string Audience { get; }

    public TimeSpan Lifetime => TimeSpan.FromMinutes(RequiredLifetimeMinutes);

    public ReadOnlyMemory<byte> SigningKey { get; }

    public static JwtOptions Load(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(SectionName);
        var issuer = section[nameof(Issuer)] ?? DefaultIssuer;
        var audience = section[nameof(Audience)] ?? DefaultAudience;
        var lifetimeMinutes = section.GetValue<int?>("LifetimeMinutes") ?? RequiredLifetimeMinutes;

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("JWT issuer configuration is required.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JWT audience configuration is required.");
        }

        if (lifetimeMinutes != RequiredLifetimeMinutes)
        {
            throw new InvalidOperationException(
                $"JWT lifetime must be {RequiredLifetimeMinutes} minutes.");
        }

        var configuredSigningKey = section["SigningKey"];
        byte[] signingKey;
        if (string.IsNullOrEmpty(configuredSigningKey))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("JWT signing key configuration is required.");
            }

            signingKey = RandomNumberGenerator.GetBytes(MinimumSigningKeyBytes);
        }
        else
        {
            try
            {
                signingKey = Convert.FromBase64String(configuredSigningKey);
            }
            catch (FormatException exception)
            {
                throw new InvalidOperationException(
                    "JWT signing key configuration must be valid Base64.",
                    exception);
            }

            if (signingKey.Length < MinimumSigningKeyBytes)
            {
                throw new InvalidOperationException(
                    $"JWT signing key configuration must decode to at least {MinimumSigningKeyBytes} bytes.");
            }
        }

        return new JwtOptions(issuer, audience, signingKey);
    }
}
