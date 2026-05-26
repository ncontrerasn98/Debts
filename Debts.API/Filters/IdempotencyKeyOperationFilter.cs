using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Debts.API.Filters;

public class IdempotencyKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;

        if (method is not ("POST" or "PATCH" or "PUT"))
            return;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid",
                Example = new Microsoft.OpenApi.Any.OpenApiString(
                    Guid.NewGuid().ToString())
            },
            Description = "UUID único por operación para evitar duplicados"
        });
    }
}