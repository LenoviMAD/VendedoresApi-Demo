using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class InjectApiKeyHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        // No dupliques si ya existe.
        if (operation.Parameters.Any(p => p.In == ParameterLocation.Header && p.Name == "x-api-key"))
            return;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "x-api-key",
            In = ParameterLocation.Header,
            Required = true,
            Description = "API key (default para pruebas).",
            Schema = new OpenApiSchema
            {
                Type = "string",
                // 👇 Default que Swagger enviará si no lo cambiás
                Default = new OpenApiString("")
            }
        });
    }
}
