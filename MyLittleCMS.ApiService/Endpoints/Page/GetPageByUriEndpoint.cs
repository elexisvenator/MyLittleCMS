using Marten;
using MyLittleCMS.ApiService.Models;
using Wolverine.Http;

namespace MyLittleCMS.ApiService.Endpoints.Page;

[Tags("Pages")]
public static class GetPageByUriEndpoint
{
    public static async Task<(IResult?, ParentPage?, DataModels.Page?, DataModels.PageContent?)> LoadAsync(
        IQuerySession session, 
        Guid parentPageId, 
        string uriComponent, 
        CancellationToken token)
    {
        if (parentPageId == Guid.Empty || string.IsNullOrWhiteSpace(uriComponent))
        {
            return (Results.NotFound(), null, null, null);
        }

        var sanitizedUriComponent = Sanitize.UriComponent(uriComponent);

        DataModels.Page? parentPage = null;
        DataModels.PageContent? pageContent = null;
        var page = await session.Query<DataModels.Page>()
            .Include((DataModels.Page p) => parentPage = p).On(p => p.ParentPageId)
            .Include((DataModels.PageContent p) => pageContent = p).On(p => p.CurrentContentVersionId!)
            .FirstOrDefaultAsync(p => p.ParentPageId == parentPageId && p.UriComponent == sanitizedUriComponent.Value, token);

        if (page is null)
        {
            return (Results.NotFound(), null, null, null);
        }

        return (WolverineContinue.Result(), parentPage is null ? null : new ParentPage(parentPage), page, pageContent);
    }

    [WolverineGet("{tenantId:int}/pages/{parentPageId:guid}/{uriComponent}", OperationId = "Get Page by URI")]
    public static ApiModels.Page GetPageByUri(
        DataModels.Page page,
        ParentPage? parent,
        DataModels.PageContent? content)
    {
        return ApiModels.Page.FromDataModel(page, parent, content);
    }
}
