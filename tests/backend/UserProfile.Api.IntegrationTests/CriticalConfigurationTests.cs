using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using UserProfile.Api.Configuration;
using UserProfile.Api.Data;
using UserProfile.Api.Security;

namespace UserProfile.Api.IntegrationTests;

public sealed class CriticalConfigurationTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UserConfigurationDefinesThePersistedSchema()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new UserConfiguration().Configure(modelBuilder.Entity<User>());

        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType>(
            modelBuilder.Model.FindEntityType(typeof(User)));
        Assert.Equal("Users", entity.GetTableName());
        Assert.Equal(
            nameof(User.Id),
            Assert.Single(Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IMutableKey>(
                entity.FindPrimaryKey()).Properties).Name);

        foreach (var propertyName in new[]
                 {
                     nameof(User.Name),
                     nameof(User.Email),
                     nameof(User.NormalizedEmail),
                     nameof(User.PasswordHash),
                     nameof(User.CreatedAtUtc),
                     nameof(User.UpdatedAtUtc)
                 })
        {
            Assert.False(entity.FindProperty(propertyName)?.IsNullable);
        }

        var index = Assert.Single(entity.GetIndexes());
        Assert.Equal(nameof(User.NormalizedEmail), Assert.Single(index.Properties).Name);
        Assert.True(index.IsUnique);
        Assert.Equal("UX_Users_NormalizedEmail", index.GetDatabaseName());
    }

    [Fact]
    public void JwtOptionsLoadEnforcesDefaultsAndExternalConfiguration()
    {
        var signingKey = RandomNumberGenerator.GetBytes(JwtOptions.MinimumSigningKeyBytes);
        var configured = LoadJwtOptions(
            Environments.Production,
            new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "Configured.Issuer",
                ["Jwt:Audience"] = "Configured.Audience",
                ["Jwt:LifetimeMinutes"] = JwtOptions.RequiredLifetimeMinutes.ToString(),
                ["Jwt:SigningKey"] = Convert.ToBase64String(signingKey)
            });

        Assert.Equal("Configured.Issuer", configured.Issuer);
        Assert.Equal("Configured.Audience", configured.Audience);
        Assert.Equal(TimeSpan.FromMinutes(15), configured.Lifetime);
        Assert.True(configured.SigningKey.Span.SequenceEqual(signingKey));

        var defaults = LoadJwtOptions(
            Environments.Development,
            Array.Empty<KeyValuePair<string, string?>>());
        Assert.Equal(JwtOptions.DefaultIssuer, defaults.Issuer);
        Assert.Equal(JwtOptions.DefaultAudience, defaults.Audience);
        Assert.Equal(JwtOptions.MinimumSigningKeyBytes, defaults.SigningKey.Length);

        Assert.Equal(
            "JWT issuer configuration is required.",
            Assert.Throws<InvalidOperationException>(() => LoadJwtOptions(
                Environments.Development,
                new Dictionary<string, string?> { ["Jwt:Issuer"] = " " })).Message);
        Assert.Equal(
            "JWT audience configuration is required.",
            Assert.Throws<InvalidOperationException>(() => LoadJwtOptions(
                Environments.Development,
                new Dictionary<string, string?> { ["Jwt:Audience"] = " " })).Message);
        Assert.Equal(
            "JWT lifetime must be 15 minutes.",
            Assert.Throws<InvalidOperationException>(() => LoadJwtOptions(
                Environments.Development,
                new Dictionary<string, string?> { ["Jwt:LifetimeMinutes"] = "14" })).Message);
        Assert.Equal(
            "JWT signing key configuration is required.",
            Assert.Throws<InvalidOperationException>(() => LoadJwtOptions(
                Environments.Production,
                Array.Empty<KeyValuePair<string, string?>>())).Message);
        Assert.Equal(
            "JWT signing key configuration must be valid Base64.",
            Assert.Throws<InvalidOperationException>(() => LoadJwtOptions(
                Environments.Development,
                new Dictionary<string, string?> { ["Jwt:SigningKey"] = "not-base64" })).Message);
        Assert.Equal(
            "JWT signing key configuration must decode to at least 32 bytes.",
            Assert.Throws<InvalidOperationException>(() => LoadJwtOptions(
                Environments.Development,
                new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] = Convert.ToBase64String(
                        RandomNumberGenerator.GetBytes(JwtOptions.MinimumSigningKeyBytes - 1))
                })).Message);
    }

    [Fact]
    public void JwtBearerOptionsEnforceIssuerAudienceSignatureAndLifetime()
    {
        var (options, jwtOptions, timeProvider) = CreateBearerOptions();
        var parameters = options.TokenValidationParameters;

        Assert.False(options.MapInboundClaims);
        Assert.Same(timeProvider, options.TimeProvider);
        Assert.True(parameters.ValidateIssuer);
        Assert.Equal(jwtOptions.Issuer, parameters.ValidIssuer);
        Assert.True(parameters.ValidateAudience);
        Assert.Equal(jwtOptions.Audience, parameters.ValidAudience);
        Assert.True(parameters.ValidateIssuerSigningKey);
        Assert.True(parameters.RequireSignedTokens);
        Assert.True(parameters.ValidateLifetime);
        Assert.True(parameters.RequireExpirationTime);
        Assert.Equal(new[] { SecurityAlgorithms.HmacSha256 }, parameters.ValidAlgorithms);
        Assert.Equal(TimeSpan.FromSeconds(30), parameters.ClockSkew);
        var signingKey = Assert.IsType<SymmetricSecurityKey>(parameters.IssuerSigningKey);
        Assert.True(signingKey.Key.AsSpan().SequenceEqual(jwtOptions.SigningKey.Span));
        Assert.NotNull(options.Events.OnTokenValidated);
        Assert.NotNull(options.Events.OnChallenge);
    }

    [Fact]
    public void JwtLifetimeValidatorEnforcesExpirationNotBeforeAndClockSkew()
    {
        var (options, _, _) = CreateBearerOptions();
        var parameters = options.TokenValidationParameters;
        var validator = Assert.IsType<LifetimeValidator>(parameters.LifetimeValidator);
        var now = FixedUtcNow.UtcDateTime;

        Assert.False(validator(null, null, null!, parameters));
        Assert.True(validator(null, now, null!, parameters));
        Assert.True(validator(now, now, null!, parameters));
        Assert.True(validator(now.AddSeconds(30), now, null!, parameters));
        Assert.False(validator(now.AddSeconds(31), now, null!, parameters));
        Assert.True(validator(null, now.AddSeconds(-30), null!, parameters));
        Assert.False(validator(null, now.AddSeconds(-31), null!, parameters));
    }

    [Fact]
    public async Task JwtBearerRejectsEveryMalformedRequiredClaimSet()
    {
        var (options, _, _) = CreateBearerOptions();
        var validClaims = CreateValidClaims();
        var validContext = CreateTokenValidatedContext(options, CreatePrincipal(validClaims));

        await options.Events.OnTokenValidated(validContext);

        Assert.Null(validContext.Result);

        foreach (var principal in CreateInvalidPrincipals(validClaims))
        {
            var context = CreateTokenValidatedContext(options, principal);

            await options.Events.OnTokenValidated(context);

            Assert.Equal(
                "The token does not contain valid required claims.",
                Assert.IsAssignableFrom<Exception>(context.Result?.Failure).Message);
        }
    }

    [Fact]
    public async Task DatabaseHealthCheckRequiresAtLeastOneAppliedMigration()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """;
            await command.ExecuteNonQueryAsync();
        }

        using var provider = CreateHealthServiceProvider(
            connection,
            typeof(CriticalConfigurationTests).Assembly.GetName().Name!);
        var healthCheck = CreateHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Database schema is not current.", result.Description);
    }

    [Fact]
    public async Task DatabaseHealthCheckRejectsDifferentMigrationIdsWithExpectedCount()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """;
            await command.ExecuteNonQueryAsync();
        }

        using var provider = CreateHealthServiceProvider(
            connection,
            typeof(UserProfileDbContext).Assembly.GetName().Name!);
        using (var scope = provider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
            var expectedMigrations = dbContext.Database.GetMigrations().ToArray();
            Assert.NotEmpty(expectedMigrations);

            for (var index = 0; index < expectedMigrations.Length; index++)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ($migrationId, '10.0.11');
                    """;
                command.Parameters.AddWithValue("$migrationId", $"unexpected-{index:D4}");
                await command.ExecuteNonQueryAsync();
            }
        }

        var healthCheck = CreateHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Database schema is not current.", result.Description);
    }

    [Fact]
    public async Task DatabaseHealthCheckReportsUnavailableDatabaseAndHonorsCancellation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var provider = CreateHealthServiceProvider(
            connection,
            typeof(UserProfileDbContext).Assembly.GetName().Name!);
        var healthCheck = CreateHealthCheck(provider);

        var unavailable = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, unavailable.Status);
        Assert.Equal("Database is unavailable.", unavailable.Description);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellation.Token));
    }

    [Fact]
    public async Task DatabaseHealthCheckReportsCanConnectFailure()
    {
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            $"user-profile-missing-{Guid.NewGuid():N}",
            "missing.db");
        await using var connection = new SqliteConnection(
            $"Data Source={missingPath};Mode=ReadOnly");
        using var provider = CreateHealthServiceProvider(
            connection,
            typeof(UserProfileDbContext).Assembly.GetName().Name!);
        var healthCheck = CreateHealthCheck(provider);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Database is unavailable.", result.Description);
        Assert.False(File.Exists(missingPath));
    }

    private static JwtOptions LoadJwtOptions(
        string environmentName,
        IEnumerable<KeyValuePair<string, string?>> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return JwtOptions.Load(configuration, new TestHostEnvironment(environmentName));
    }

    private static (JwtBearerOptions Options, JwtOptions JwtOptions, TimeProvider TimeProvider)
        CreateBearerOptions()
    {
        var signingKey = RandomNumberGenerator.GetBytes(JwtOptions.MinimumSigningKeyBytes);
        var jwtOptions = LoadJwtOptions(
            Environments.Production,
            new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "Test.Issuer",
                ["Jwt:Audience"] = "Test.Audience",
                ["Jwt:SigningKey"] = Convert.ToBase64String(signingKey)
            });
        var timeProvider = new FixedTimeProvider(FixedUtcNow);
        var options = new JwtBearerOptions();
        JwtBearerConfiguration.Configure(options, jwtOptions, timeProvider);
        return (options, jwtOptions, timeProvider);
    }

    private static TokenValidatedContext CreateTokenValidatedContext(
        JwtBearerOptions options,
        ClaimsPrincipal? principal)
    {
        return new TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme(
                JwtBearerDefaults.AuthenticationScheme,
                displayName: null,
                typeof(JwtBearerHandler)),
            options)
        {
            Principal = principal
        };
    }

    private static IReadOnlyList<Claim> CreateValidClaims()
    {
        return
        [
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, "100", ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, "200", ClaimValueTypes.Integer64)
        ];
    }

    private static IEnumerable<ClaimsPrincipal?> CreateInvalidPrincipals(
        IReadOnlyList<Claim> validClaims)
    {
        yield return null;
        yield return CreatePrincipal(ReplaceClaims(validClaims, JwtRegisteredClaimNames.Sub, []));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Sub,
            [new Claim(JwtRegisteredClaimNames.Sub, " ")]));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Sub,
            [new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid")]));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Sub,
            [new Claim(JwtRegisteredClaimNames.Sub, Guid.Empty.ToString())]));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Sub,
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())
            ]));
        yield return CreatePrincipal(ReplaceClaims(validClaims, JwtRegisteredClaimNames.Jti, []));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Jti,
            [new Claim(JwtRegisteredClaimNames.Jti, "not-a-guid")]));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Jti,
            [new Claim(JwtRegisteredClaimNames.Jti, Guid.Empty.ToString())]));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Jti,
            [
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]));
        yield return CreatePrincipal(ReplaceClaims(validClaims, JwtRegisteredClaimNames.Iat, []));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Iat,
            [new Claim(JwtRegisteredClaimNames.Iat, "not-a-number")]));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Iat,
            [new Claim(JwtRegisteredClaimNames.Iat, "0")]));
        yield return CreatePrincipal(ReplaceClaims(validClaims, JwtRegisteredClaimNames.Exp, []));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Exp,
            [new Claim(JwtRegisteredClaimNames.Exp, "not-a-number")]));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Exp,
            [new Claim(JwtRegisteredClaimNames.Exp, "0")]));
        yield return CreatePrincipal(ReplaceClaims(
            validClaims,
            JwtRegisteredClaimNames.Exp,
            [new Claim(JwtRegisteredClaimNames.Exp, "100")]));
    }

    private static ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static IReadOnlyList<Claim> ReplaceClaims(
        IEnumerable<Claim> source,
        string claimType,
        IEnumerable<Claim> replacements)
    {
        return source.Where(claim => claim.Type != claimType).Concat(replacements).ToArray();
    }

    private static ServiceProvider CreateHealthServiceProvider(
        SqliteConnection connection,
        string migrationsAssembly)
    {
        var services = new ServiceCollection();
        services.AddDbContext<UserProfileDbContext>(options => options.UseSqlite(
            connection,
            sqlite => sqlite.MigrationsAssembly(migrationsAssembly)));
        return services.BuildServiceProvider();
    }

    private static DatabaseHealthCheck CreateHealthCheck(IServiceProvider provider)
    {
        return new DatabaseHealthCheck(provider.GetRequiredService<IServiceScopeFactory>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = nameof(CriticalConfigurationTests);

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
