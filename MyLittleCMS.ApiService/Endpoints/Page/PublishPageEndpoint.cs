using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;
using Wolverine.Http.Marten;
using Wolverine.Marten;

namespace MyLittleCMS.ApiService.Endpoints.Page;

[Tags("Pages")]
public static class PublishPageEndpoint
{
    public static ProblemDetails ValidateAsync(DataModels.Page page)
    {
        if (!page.IsPublished && string.IsNullOrWhiteSpace(page.CurrentContentVersionId))
        {
            return new ProblemDetails
            {
                Detail = "Page does not have a current content version",
                Status = StatusCodes.Status400BadRequest
            };
        }

        return WolverineContinue.NoProblems;
    }

    [WolverinePost("{tenantId:int}/pages/{pageId:guid}/publish", OperationId = "Publish Page")]
    public static IMartenOp PublishPage([Document(Required = true, MaybeSoftDeleted = false)] DataModels.Page page)
    {
        if (page.IsPublished)
        {
            return MartenOps.Nothing();
        }

        return MartenOps.Store(page with { IsPublished = true });
    }
}