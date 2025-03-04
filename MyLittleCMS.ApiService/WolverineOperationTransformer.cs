using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Wolverine.Http;

namespace MyLittleCMS.ApiService;

public class WolverineOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is WolverineActionDescriptor action)
        {
            operation.OperationId = action.Chain.OperationId;
            operation.Summary = action.Chain.OperationId;
        }

        return Task.CompletedTask;
    }
}