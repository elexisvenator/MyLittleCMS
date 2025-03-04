using Marten.Schema;
using Marten.Schema.Indexing.Unique;

namespace MyLittleCMS.ApiService.DataModels;

public sealed record User
{
    [Identity] 
    public required Guid UserId { get; init; }

    [UniqueIndex(TenancyScope = TenancyScope.PerTenant)]
    public required string UserName { get; init; }
    public required string Name { get; init; }
    public required bool Active { get; init; }
}