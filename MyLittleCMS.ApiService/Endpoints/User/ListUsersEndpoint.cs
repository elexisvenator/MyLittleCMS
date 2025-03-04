using MyLittleCMS.ApiService.ApiModels;
using Wolverine.Http;
using Wolverine.Http.FluentValidation.Internals;
using Wolverine.Http.FluentValidation;
using FluentValidation;
using Marten.Pagination;
using Marten;

namespace MyLittleCMS.ApiService.Endpoints.User;

[Tags("Users")]
public static class ListUsersEndpoint
{
    // Pattern for validating query strings
    // 1 - inject all querystring arguments as parameters, as well as a validator and problemDetailsSource
    // 2 - add all querystring arguments to a request model
    // 3 - invoke FluentValidationHttpExecutor.ExecuteOne. and return the result + the request model
    // 4 - use the request model in the main handler
    public static async Task<(IResult, ListUsersRequest)> ValidateAsync(
        int? pageNumber,
        int? pageSize,
        bool? includeInactive,
        IValidator<ListUsersRequest> validator,
        IProblemDetailSource<ListUsersRequest> problemDetailSource)
    {
        var request = new ListUsersRequest()
        {
            IncludeInactive = includeInactive ?? false,
            PageNumber = pageNumber ?? PagedResult.DefaultPageNumber,
            PageSize = pageSize ?? PagedResult.DefaultPageSize
        };

        return (
            await FluentValidationHttpExecutor.ExecuteOne(validator, problemDetailSource, request),
            request);
    }

    [WolverineGet("{tenantId:int}/users", OperationId = "List Users")]
    public static async Task<PagedResult<ApiModels.User>> RegisterUser(
        ListUsersRequest request,
        IQuerySession session, 
        CancellationToken token)
    {
        return (await session.QueryByPlanAsync(new ListUsersQueryPlan(request), token)).ToPagedResult();
    }
}

public record ListUsersRequest : PagingRequest
{
    public bool IncludeInactive { get; set; }
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
