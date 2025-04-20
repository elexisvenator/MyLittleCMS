using MyLittleCMS.ApiService.ApiModels;
using Wolverine.Http;
using Marten.Pagination;
using Marten;
using Microsoft.AspNetCore.Mvc;

namespace MyLittleCMS.ApiService.Endpoints.User;

[Tags("Users")]
public static class ListUsersEndpoint
{
    [WolverineGet("{tenantId:int}/users", OperationId = "List Users")]
    public static async Task<PagedResult<ApiModels.User>> RegisterUser(
        [FromQuery]ListUsersRequest request,
        IQuerySession session, 
        CancellationToken token)
    {
        return (await session.QueryByPlanAsync(new ListUsersQueryPlan(request), token)).ToPagedResult();
    }
}

public record ListUsersRequest : PagingRequest
{
    public bool IncludeInactive { get; set; } = false;
    public sealed class Validator : Validator<ListUsersRequest>;
}

public record ListUsersQueryPlan(ListUsersRequest SearchParameters) : IQueryPlan<IPagedList<ApiModels.User>>
{
    public Task<IPagedList<ApiModels.User>> Fetch(IQuerySession session, CancellationToken token) =>
        session.Query<DataModels.User>()
            .Where(user => user.Active || SearchParameters.IncludeInactive)
            .OrderBy(user => user.UserName)
            .Select(user => new ApiModels.User
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Name = user.Name,
                Active = user.Active
            })
            .ToPagedListAsync(
                SearchParameters.PageNumber == 0 ? 1 : SearchParameters.PageNumber,
                SearchParameters.PageSize,
                token);
}
