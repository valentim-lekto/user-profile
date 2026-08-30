namespace UserProfile.Api.Features.Auth;

// Accessed through ASP.NET Core JSON serialization and OpenAPI reflection.
// ReSharper disable once NotAccessedPositionalProperty.Global
public sealed record LoginResponse(string AccessToken);
