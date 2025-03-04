using Wolverine.Http;
using Wolverine.Http.Marten;

namespace MyLittleCMS.ApiService.Endpoints.User;

[Tags("Users")]
public static class GetUserEndpoint
{
    [WolverineGet("{tenantId:int}/users/{userId:guid}", OperationId = "Get User")]
    public static ApiModels.User GerUser([Document(MaybeSoftDeleted = false, Required = true)] DataModels.User user)
    {
        return new ApiModels.User
        {
            UserId = user.UserId,
            UserName = user.UserName,
            Name = user.Name,
            Active = user.Active
        };
    }
}