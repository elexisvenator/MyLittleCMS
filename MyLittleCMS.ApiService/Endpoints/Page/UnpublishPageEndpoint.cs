using Wolverine.Http;
using Wolverine.Http.Marten;
using Wolverine.Marten;

namespace MyLittleCMS.ApiService.Endpoints.Page;

[Tags("Pages")]
public static class UnpublishPageEndpoint
{
    [WolverinePost("{tenantId:int}/pages/{pageId:guid}/unpublish", OperationId = "Unpublish Page")]
    public static IMartenOp UnpublishPage([Document(Required = true, MaybeSoftDeleted = false)] DataModels.Page page)
    {
        if (!page.IsPublished)
        {
            return MartenOps.Nothing();
        }

        return MartenOps.Store(page with { IsPublished = false });
    }
}