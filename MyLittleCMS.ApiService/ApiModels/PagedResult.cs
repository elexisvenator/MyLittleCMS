using Marten.Pagination;

namespace MyLittleCMS.ApiService.ApiModels;

public record PagedResult<TItem>(
    IReadOnlyList<TItem> Items,
    long TotalItems,
    long PageCount,
    int PageNumber,
    int PageSize,
    long CurrentPageItemCount,
    bool HasNextPage,
    bool HasPreviousPage);

public static class PagedResult
{
    public const int DefaultPageSize = 10;
    public const int DefaultPageNumber = 1;
    public const int MaxPageSize = 200;
    public const int MinPageSize = 0;
    public const int MinPageNumber = 0;
    public const int MaxPageNumber = int.MaxValue;

    public static PagedResult<TItem> From<TItem>(IPagedList<TItem> queryResult)
    {
        return new PagedResult<TItem>(
            Items: queryResult.ToList(),
            TotalItems: queryResult.TotalItemCount,
            PageCount: queryResult.PageCount,
            PageNumber: (int)queryResult.PageNumber,
            PageSize: (int)queryResult.PageSize,
            CurrentPageItemCount: queryResult.Count,
            HasNextPage: queryResult.HasNextPage,
            HasPreviousPage: queryResult.HasPreviousPage);
    }

    public static PagedResult<TItem> ToPagedResult<TItem>(this IPagedList<TItem> queryResult) => From(queryResult);
}