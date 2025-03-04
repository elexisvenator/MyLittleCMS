using FluentValidation;
using Marten;

namespace MyLittleCMS.ApiService.Models;

public static class CustomValidators
{
    public static IRuleBuilderOptions<T, Guid?> MustBeAnExistingActiveUser<T>(this IRuleBuilderInitial<T, Guid?> ruleBuilder, IQuerySession session)
    {
        return ruleBuilder.Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(userId => !Guid.Empty.Equals(userId)).WithMessage("{PropertyName} must not be empty")
            .MustAsync(async (userId, token) =>
            {
                var author = await session.LoadAsync<DataModels.User>(userId!, token);
                return author is { Active: true };
            }).WithMessage("{PropertyName} '{PropertyValue}' is not a valid active user");
    }
    public static IRuleBuilderOptions<T, IList<TElement>> ListMustContainFewerThan<T, TElement>(this IRuleBuilder<T, IList<TElement>> ruleBuilder, int num)
    {
        return ruleBuilder.Must(list => list.Count < num).WithMessage("The list contains too many items");
    }
}