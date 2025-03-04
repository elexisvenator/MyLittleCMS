using Marten;
using MyLittleCMS.ApiService.DataModels;
using MyLittleCMS.ApiService.DataModels.Events;
using Wolverine.Http;
using Wolverine.Http.Marten;

namespace MyLittleCMS.ApiService.Endpoints.PageContent;

[Tags("PageContent")]
public static class GetPageContentByVersionNumberEndpoint
{
    public static async Task<(IResult, DataModels.PageContent?)> LoadAsync(Guid pageId, int versionNumber, IDocumentSession session, CancellationToken token)
    {
        var pageContentId = PageContentId.From(PageId.From(pageId), versionNumber);
        var pageContent = await session.Events.FetchLatest<DataModels.PageContent>(pageContentId.Value, token);

        if (pageContent is null)
        {
            return (Results.NotFound(), null);
        }

        return (WolverineContinue.Result(), pageContent);
    }

    [WolverineGet("{tenantId:int}/pages/{pageId:guid}/content/{versionNumber:int}", OperationId = "Get Page Content by Version Number")]
    public static ApiModels.PageContent GetPageContentById(
        [Document(Required = true, MaybeSoftDeleted = false)] DataModels.Page page,
        DataModels.PageContent content)
    {
        return ApiModels.PageContent.FromDataModel(page, content);
    }
}
