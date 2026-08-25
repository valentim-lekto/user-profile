using System.Text.Json.Serialization;

namespace UserProfile.Api.Features.Operations;

[JsonConverter(typeof(JsonStringEnumConverter<HealthState>))]
public enum HealthState
{
    Healthy
}

public sealed record HealthResponse(HealthState Status);
