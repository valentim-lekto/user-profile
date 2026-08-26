using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using UserProfile.Api.Features.Auth;
using UserProfile.Api.Features.Profile;

namespace UserProfile.Api.Configuration;

public sealed class ResponseSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema responseSchema)
        {
            return;
        }

        if (context.Type == typeof(LoginResponse))
        {
            FindProperty(responseSchema, "accessToken")?.ReadOnly = true;
            return;
        }

        if (context.Type != typeof(ProfileResponse))
        {
            return;
        }

        FindProperty(responseSchema, "id")?.ReadOnly = true;

        if (FindProperty(responseSchema, "name") is { } name)
        {
            name.MinLength = 3;
            name.MaxLength = 200;
        }

        if (FindProperty(responseSchema, "email") is { } email)
        {
            email.Format = "email";
            email.Pattern = RegisterRequest.EmailPattern;
            email.MaxLength = 320;
        }
    }

    private static OpenApiSchema? FindProperty(OpenApiSchema schema, string propertyName)
    {
        if (schema.Properties is null ||
            !schema.Properties.TryGetValue(propertyName, out var property))
        {
            return null;
        }

        return property as OpenApiSchema;
    }
}
