using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserProfile.Api.Features.Auth;

public sealed class RegisterRequestSchemaFilter : ISchemaFilter
{
    public const string RawEmailPattern =
        @"^\s*[\x21-\x3F\x41-\x7E]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+\s*$";

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema requestSchema ||
            (context.Type != typeof(RegisterRequest) && context.Type != typeof(LoginRequest)))
        {
            return;
        }

        if (context.Type == typeof(RegisterRequest) &&
            FindProperty(requestSchema, "name") is { } name)
        {
            MarkTrimmed(name, minimumLength: 3, maximumLength: 200);
        }

        if (FindProperty(requestSchema, "email") is { } email)
        {
            MarkTrimmed(email, minimumLength: 1, maximumLength: 320);
            email.Format = null;
            email.Pattern = RawEmailPattern;
            AddExtension(email, "x-pattern-after-trim", RegisterRequest.EmailPattern);
        }

        MarkPassword(requestSchema, "password");
        if (context.Type == typeof(RegisterRequest))
        {
            MarkPassword(requestSchema, "passwordConfirmation");
        }
    }

    private static void MarkTrimmed(
        OpenApiSchema property,
        int minimumLength,
        int maximumLength)
    {
        property.MinLength = null;
        property.MaxLength = null;
        AddExtension(property, "x-trim", true);
        AddExtension(property, "x-min-length-after-trim", minimumLength);
        AddExtension(property, "x-max-length-after-trim", maximumLength);
    }

    private static void AddExtension(OpenApiSchema schema, string name, JsonNode value)
    {
        schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        schema.Extensions[name] = new JsonNodeExtension(value);
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
