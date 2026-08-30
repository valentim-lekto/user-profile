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

        response.Headers ??= new Dictionary<string, IOpenApiHeader>(
            StringComparer.OrdinalIgnoreCase);
        response.Headers["Retry-After"] = new OpenApiHeader
        {
            Required = true,
            Description = "Suggested number of seconds before retrying.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Format = "int32",
                Minimum = "1"
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
    }
}
