using System.Security.Cryptography;

namespace UserProfile.Api.Configuration;

public sealed class JwtOptions
{
    private const string SectionName = "Jwt";
    private const string DefaultIssuer = "UserProfile.Api";
    private const string DefaultAudience = "UserProfile.Web";
    private const int RequiredLifetimeMinutes = 15;
    private const int MinimumSigningKeyBytes = 32;

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

    public static TimeSpan Lifetime => TimeSpan.FromMinutes(RequiredLifetimeMinutes);

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
        var signingKey = LoadSigningKey(configuredSigningKey, environment);

        return new JwtOptions(issuer, audience, signingKey);
    }

    private static byte[] LoadSigningKey(
        string? configuredSigningKey,
        IHostEnvironment environment)
    {
        if (string.IsNullOrEmpty(configuredSigningKey))
        {
            return !environment.IsDevelopment() ? throw new InvalidOperationException("JWT signing key configuration is required.") : RandomNumberGenerator.GetBytes(MinimumSigningKeyBytes);
        }

        var signingKey = DecodeSigningKey(configuredSigningKey);
        if (signingKey.Length < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"JWT signing key configuration must decode to at least {MinimumSigningKeyBytes} bytes.");
        }

        return signingKey;
    }

    private static byte[] DecodeSigningKey(string configuredSigningKey)
    {
        try
        {
            return Convert.FromBase64String(configuredSigningKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "JWT signing key configuration must be valid Base64.",
                exception);
        }
    }
}
