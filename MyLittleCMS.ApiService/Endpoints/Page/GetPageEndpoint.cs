using Marten;
using Wolverine.Http;
using Wolverine.Http.Marten;

namespace MyLittleCMS.ApiService.Endpoints.Page;

[Tags("Pages")]
public static class GetPageEndpoint
{
    public static async Task<(ParentPage?, DataModels.PageContent?)> LoadAsync(
        IQuerySession session, 
        DataModels.Page page)
    {
        if (page.ParentPageId == Guid.Empty && page.CurrentContentVersionId is null)
        {
            return (null, null);
        }

        var batch = session.CreateBatchQuery();
        var parentTask = page.ParentPageId == Guid.Empty
            ? Task.FromResult<DataModels.Page?>(null)
            : batch.Load<DataModels.Page>(page.ParentPageId);
        var contentTask = page.CurrentContentVersionId is null
            ? Task.FromResult<DataModels.PageContent?>(null)
            : batch.Load<DataModels.PageContent>(page.CurrentContentVersionId);

        await batch.Execute();
        var parent = await parentTask;
        var content = await contentTask;

        return (parent is null ? null : new ParentPage(parent), content);
    }

    [WolverineGet("{tenantId:int}/pages/{pageId:guid}", OperationId = "Get Page")]
    public static ApiModels.Page GetPage(
        [Document(Required = true, MaybeSoftDeleted = false)]DataModels.Page page,
        ParentPage? parent,
        DataModels.PageContent? content)
    {
        return ApiModels.Page.FromDataModel(page, parent, content);
    }
}

// creating a new model to store the parent so we can
// add the base page and the parent page to the stack at the same time
public sealed record ParentPage(DataModels.Page Page);