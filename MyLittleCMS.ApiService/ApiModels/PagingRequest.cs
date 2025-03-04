using FluentValidation;

namespace MyLittleCMS.ApiService.ApiModels;

public abstract record PagingRequest
{
    public int PageNumber { get; init; } = PagedResult.DefaultPageNumber;
    public int PageSize { get; init; } = PagedResult.DefaultPageSize;
    public abstract class Validator<T> : AbstractValidator<T> where T : PagingRequest
    {
        protected Validator()
        {
            RuleFor(x => x.PageNumber).InclusiveBetween(PagedResult.MinPageNumber, PagedResult.MaxPageNumber);
            RuleFor(x => x.PageSize).InclusiveBetween(PagedResult.MinPageSize, PagedResult.MaxPageSize);
        }
    }
}