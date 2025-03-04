using Marten.Metadata;
using Marten.Schema;
using Marten.Schema.Identity;
using Marten.Schema.Indexing.Unique;
using Vogen;

namespace MyLittleCMS.ApiService.DataModels;

[ValueObject<Guid>]
public readonly partial struct PageId
{
    public static PageId New() => From(CombGuidIdGeneration.NewGuid());
}

[ValueObject<string>]
public partial class UriComponent
{
    private static string NormalizeInput(string input)
    {
        return input.Trim().ToLowerInvariant();
    }
}

public sealed record Page : ISoftDeleted, IVersioned
{
    [Identity] public required Guid PageId { get; init; }
    [DuplicateField] public required Guid ParentPageId { get; init; }
    [DuplicateField] public required string UriComponent { get; init; }
    public required string Name { get; init; }
    public bool HasPublishedVersion => CurrentContentVersionNumber.HasValue;
    public bool IsPublished { get; init; } = false;
    public string? CurrentContentVersionId { get; init; }
    public int? CurrentContentVersionNumber { get; init; }
    public string? DraftContentVersionId { get; init; }
    public int? DraftContentVersionNumber { get; init; }
    public required Guid CreatedBy { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }


    [UniqueIndex(TenancyScope = TenancyScope.PerTenant)]
    public string ParentAndUriComponent =>
        // if the parent page id is empty then this is the root document
        // a root document never has a uri component
        ParentPageId == Guid.Empty ? "" : $"{ParentPageId:N}-{UriComponent}";

    public bool Deleted { get; set; }
    public string? DeletedBy { get; init; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid Version { get; set; }

    // disabled due to broken index support
    //// user defined tags to aid in searching
    //public List<string> Tags { get; init; }

    internal static string BuildParentAndUriComponent(Guid parentPageId, string uriComponent) =>
        parentPageId == Guid.Empty ? "" : $"{parentPageId:N}-{uriComponent}";
}