using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserProfile.Api.Configuration;

namespace UserProfile.Api.IntegrationTests;

public sealed class JwtConfigurationTests
{
    [Fact]
    public async Task ValidExternalSigningKeyAllowsStartupOutsideDevelopment()
    {
        var signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using var factory = new ConfigurationFactory("Production", signingKey);

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MissingSigningKeyFailsStartupOutsideDevelopment()
    {
        using var factory = new ConfigurationFactory("Production", signingKey: null);

        var exception = await AssertStartupFailureAsync(factory);

        Assert.Contains("JWT signing key configuration is required.", exception.ToString());
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task InvalidBase64SigningKeyAlwaysFailsStartup(string environment)
    {
        var invalidKeyMarker = $"not-base64-{Guid.NewGuid():N}";
        using var factory = new ConfigurationFactory(environment, invalidKeyMarker);

        var exception = await AssertStartupFailureAsync(factory);

        Assert.Contains("must be valid Base64", exception.ToString());
        Assert.DoesNotContain(invalidKeyMarker, exception.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task ShortSigningKeyAlwaysFailsStartup(string environment)
    {
        var shortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(31));
        using var factory = new ConfigurationFactory(environment, shortKey);

        var exception = await AssertStartupFailureAsync(factory);

        Assert.Contains("must decode to at least 32 bytes", exception.ToString());
        Assert.DoesNotContain(shortKey, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingDevelopmentKeyUsesDifferentRandomFallbackPerProcessHost()
    {
        using var firstFactory = new ConfigurationFactory("Development", signingKey: null);
        using var secondFactory = new ConfigurationFactory("Development", signingKey: null);
        using var firstClient = firstFactory.CreateClient();
        using var secondClient = secondFactory.CreateClient();
        using var firstResponse = await firstClient.GetAsync("/health");
        using var secondResponse = await secondClient.GetAsync("/health");
        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();

        var firstOptions = firstFactory.Services.GetRequiredService<JwtOptions>();
        var secondOptions = secondFactory.Services.GetRequiredService<JwtOptions>();

        Assert.Equal(32, firstOptions.SigningKey.Length);
        Assert.Equal(32, secondOptions.SigningKey.Length);
        Assert.False(firstOptions.SigningKey.Span.SequenceEqual(secondOptions.SigningKey.Span));
    }

    private static async Task<Exception> AssertStartupFailureAsync(ConfigurationFactory factory)
    {
        return await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var client = factory.CreateClient();
            using var response = await client.GetAsync("/health");
        });
    }

    private sealed class ConfigurationFactory : WebApplicationFactory<Program>
    {
        private readonly string environment;
        private readonly string? signingKey;
        private readonly string testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"user-profile-jwt-config-tests-{Guid.NewGuid():N}");

        public ConfigurationFactory(string environment, string? signingKey)
        {
            this.environment = environment;
            this.signingKey = signingKey;
            Directory.CreateDirectory(testDirectory);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] =
                        $"Data Source={Path.Combine(testDirectory, "user-profile.db")};Pooling=False"
                };
                if (signingKey is not null)
                {
                    values["Jwt:SigningKey"] = signingKey;
                }

                configuration.AddInMemoryCollection(values);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }
}
