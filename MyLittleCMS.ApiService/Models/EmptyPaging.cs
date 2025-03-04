using System.Collections;
using Marten.Pagination;

namespace MyLittleCMS.ApiService.Models;

public sealed class EmptyPaging<T> : IPagedList<T>
{
    public static readonly IPagedList<T> Instance = new EmptyPaging<T>();

    private EmptyPaging()
    {
    }

    private readonly IReadOnlyList<T> _collection = [];
    public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();
    public T this[int index] => _collection[index];
    public long Count => 0;
    public long PageNumber => 1;
    public long PageSize => 0;
    public long PageCount => 0;
    public long TotalItemCount => 0;
    public bool HasPreviousPage => false;
    public bool HasNextPage => false;
    public bool IsFirstPage => true;
    public bool IsLastPage => true;
    public long FirstItemOnPage => 0;
    public long LastItemOnPage => 0;
}