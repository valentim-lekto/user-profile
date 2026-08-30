using System.Text.Json.Serialization;

namespace UserProfile.Api.Features.Operations;

[JsonConverter(typeof(JsonStringEnumConverter<HealthState>))]
public enum HealthState
{
    Healthy
}

// Accessed through ASP.NET Core JSON serialization and OpenAPI reflection.
// ReSharper disable once NotAccessedPositionalProperty.Global
public sealed record HealthResponse(HealthState Status);
