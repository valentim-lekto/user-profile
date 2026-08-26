using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserProfile.Api.Features.Auth;

public sealed class RegisterRequestSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(RegisterRequest) || schema is not OpenApiSchema requestSchema)
        {
            return;
        }

        if (FindProperty(requestSchema, "email") is { } email)
        {
            email.Format = "email";
            email.Pattern = RegisterRequest.EmailPattern;
        }

        MarkPassword(requestSchema, "password");
        MarkPassword(requestSchema, "passwordConfirmation");
    }

    private static void MarkPassword(OpenApiSchema schema, string propertyName)
    {
        if (FindProperty(schema, propertyName) is { } password)
        {
            password.Format = "password";
            password.WriteOnly = true;
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
