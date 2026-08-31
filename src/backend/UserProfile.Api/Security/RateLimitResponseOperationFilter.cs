using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserProfile.Api.Security;

// Swashbuckle instantiates the filter registered by OperationFilter<TFilter>().
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class RateLimitResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Responses is null ||
            !operation.Responses.TryGetValue("429", out var rateLimitResponse) ||
            rateLimitResponse is not OpenApiResponse response)
        {
            return;
        }

        var headers = new Dictionary<string, IOpenApiHeader>(
            StringComparer.OrdinalIgnoreCase);
        if (response.Headers is not null)
        {
            foreach (var header in response.Headers)
            {
                if (!header.Key.Equals("Retry-After", StringComparison.OrdinalIgnoreCase) &&
                    !header.Key.Equals("Cache-Control", StringComparison.OrdinalIgnoreCase))
                {
                    headers[header.Key] = header.Value;
                }
            }
        }

        response.Headers = headers;
        response.Headers["Retry-After"] = new OpenApiHeader
        {
            Required = true,
            Description = "Suggested number of seconds before retrying.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Format = "int32",
                Minimum = "60",
                Maximum = "60"
            }
        };
        response.Headers["Cache-Control"] = new OpenApiHeader
        {
            Required = true,
            Description = "Prevents caches from storing the rate-limit response.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Pattern = "^no-store$"
            }
        };

        if (response.Content is not null &&
            response.Content.TryGetValue("application/problem+json", out var mediaType) &&
            mediaType.Schema is { } problemDetailsSchema)
        {
            mediaType.Schema = new OpenApiSchema
            {
                AllOf =
                [
                    problemDetailsSchema,
                    new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Required = new HashSet<string>
                        {
                            "type",
                            "title",
                            "status",
                            "detail",
                            "instance"
                        },
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["type"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Format = "uri-reference"
                            },
                            ["title"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String
                            },
                            ["status"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Integer,
                                Format = "int32",
                                Minimum = "429",
                                Maximum = "429"
                            },
                            ["detail"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                MinLength = 1,
                                Pattern = @"\S"
                            },
                            ["instance"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Format = "uri-reference"
                            }
                        }
                    }
                ]
            };
        }
    }
}
