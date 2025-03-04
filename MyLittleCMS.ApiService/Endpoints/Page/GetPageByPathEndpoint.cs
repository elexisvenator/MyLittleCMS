using System.Text;
using Marten;
using Marten.Linq.MatchesSql;
using MyLittleCMS.ApiService.Models;
using Wolverine.Http;

namespace MyLittleCMS.ApiService.Endpoints.Page;

[Tags("Pages")]
public static class GetPageByPathEndpoint
{
    public static async Task<(IResult?, DataModels.Page?, ParentPage?, DataModels.PageContent?)> LoadAsync(
        IQuerySession session, 
        string? uriPath, 
        CancellationToken token)
    {
        var pathParts = string.IsNullOrWhiteSpace(uriPath)
            ? []
            : uriPath.Split(',', '/', '\\').Select(p => Sanitize.UriComponent(p).Value).ToList();

        if (pathParts.Count == 0)
        {
            DataModels.PageContent? rootPageContent = null;
            var rootPage = await session.Query<DataModels.Page>()
                .Include((DataModels.PageContent p) => rootPageContent = p).On(p => p.CurrentContentVersionId!)
                .FirstOrDefaultAsync(p => p.ParentPageId == Guid.Empty, token: token);

            if (rootPage is null)
            {
                return (Results.NotFound(), null, null, null);
            }

            return (WolverineContinue.Result(), rootPage, null, rootPageContent);
        }

        var (page, parent, content) = await session.QueryByPlanAsync(new QueryPageByPathPlan(pathParts), token);
        if (page is null)
        {
            return (Results.NotFound(), null, null, null);
        }

        return (WolverineContinue.Result(), page, parent, content);
    }

    [WolverineGet("{tenantId:int}/pages/path", OperationId = "Get Page by Path")]
    public static ApiModels.Page GetPageByPath(
        DataModels.Page page,
        ParentPage? parent,
        DataModels.PageContent? content)
    {
        return ApiModels.Page.FromDataModel(page, parent, content);
    }
}

public class QueryPageByPathPlan(IReadOnlyCollection<string> uriPath)
    : IQueryPlan<(DataModels.Page?, ParentPage?, DataModels.PageContent?)>
{
    public async Task<(DataModels.Page?, ParentPage?, DataModels.PageContent?)> Fetch(IQuerySession session, CancellationToken token)
    {
        var schema = session.DocumentStore.Options.Schema;

        var pathQuery = new StringBuilder();
        object[] args = [..uriPath, Guid.Empty];

        pathQuery.AppendLine($"exists ( ");
        pathQuery.AppendLine($"    select 1 ");
        pathQuery.AppendLine($"    from {schema.For<DataModels.Page>()} pp_1 ");

        var currentIndex = 1;
        foreach (var _ in uriPath)
        {
            currentIndex++;
            var currentAlias = $"pp_{currentIndex}";
            var previousAlias = $"pp_{currentIndex - 1}";
            pathQuery.AppendLine($"    inner join {schema.For<DataModels.Page>()} {currentAlias}");
            pathQuery.AppendLine($"        on {currentAlias}.parent_page_id = {previousAlias}.id ");
            pathQuery.AppendLine($"       and {currentAlias}.uri_component = ? ");
        }

        pathQuery.AppendLine($"    where pp_1.parent_page_id = ?");
        pathQuery.AppendLine($"      and pp_{currentIndex}.id = d.id");
        pathQuery.Append(")");

        var pathQueryString = pathQuery.ToString();

        var lastUriComponent = uriPath.LastOrDefault("");

        DataModels.Page? parent = null;
        DataModels.PageContent? content = null;
        var page = await session.Query<DataModels.Page>()
            .Include((DataModels.Page p) => parent = p).On(p => p.ParentPageId)
            .Include((DataModels.PageContent p) => content = p).On(p => p.CurrentContentVersionId!)
            .Where(p => p.MatchesSql(pathQueryString, args))
            .FirstOrDefaultAsync(p => p.UriComponent == lastUriComponent, token);

        return (page, parent is null ? null : new ParentPage(parent), content);
    }
}