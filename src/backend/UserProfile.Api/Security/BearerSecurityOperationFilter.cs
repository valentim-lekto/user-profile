using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserProfile.Api.Security;

public sealed class BearerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        AddUnauthorizedChallengeHeader(operation);

        if (!context.ApiDescription.ActionDescriptor.EndpointMetadata
                .OfType<IAuthorizeData>()
                .Any())
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("bearerAuth", context.Document, null)] = []
        });
    }

    private static void AddUnauthorizedChallengeHeader(OpenApiOperation operation)
    {
        if (operation.Responses is null ||
            !operation.Responses.TryGetValue("401", out var unauthorizedResponse) ||
            unauthorizedResponse is not OpenApiResponse response)
        {
            return;
        }

        response.Headers ??= new Dictionary<string, IOpenApiHeader>(
            StringComparer.OrdinalIgnoreCase);
        response.Headers["WWW-Authenticate"] = new OpenApiHeader
        {
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Pattern = "^Bearer(?: .*)?$"
            }
        };
    }
}
