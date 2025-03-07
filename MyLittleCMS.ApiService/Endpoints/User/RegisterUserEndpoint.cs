using System.ComponentModel;
using FluentValidation;
using JasperFx.Core;
using Marten;
using Wolverine.Http;
using Wolverine.Marten;
using Wolverine.Persistence;

namespace MyLittleCMS.ApiService.Endpoints.User;

[Tags("Users")]
public static class RegisterUserEndpoint
{
    [WolverinePost("{tenantId:int}/users/register", OperationId = "Register User")]
    public static (UserRegisteredResponse, IMartenOp) RegisterUser(RegisterUserRequest request, TenantId tenantId)
    {
        var userId = CombGuidIdGeneration.NewGuid();
        return (
            new UserRegisteredResponse(tenantId.Value, userId),
            MartenOps.Insert(new DataModels.User
            {
                UserId = userId,
                UserName = request.UserName!.Trim(),
                Name = request.Name!,
                Active = true
            })
        );
    }
}

public record RegisterUserRequest
{
    public string? UserName { get; set; }
    public string? Name { get; set; }

    public class Validator : AbstractValidator<RegisterUserRequest>
    {
        public Validator(IQuerySession session)
        {
            RuleFor(x => x.UserName).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MustAsync(async (username, token) =>
                {
                    var trimmed = username!.Trim();
                    // TODO: should make a decision on case sensitivity support
                    var exists = await session.Query<DataModels.User>().AnyAsync(u => u.UserName == trimmed, token);
                    return !exists;
                }).WithMessage("{PropertyName} '{PropertyValue}' already exists");
            
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}

public sealed record UserRegisteredResponse(
    [property: Description("The tenant id")]
    string TenantId,
    [property: Description("The user id")]
    Guid UserId
) : CreationResponse($"/{TenantId}/users/{UserId}");