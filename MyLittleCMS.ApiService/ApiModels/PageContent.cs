namespace MyLittleCMS.ApiService.ApiModels;

public record PageContent
{
    public required string PageContentId { get; init; }
    public required PageSummary Page { get; init; }
    public required int VersionNumber { get; init; }
    public required string Content { get; init; } 
    public required Guid CreatedBy { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required Guid LastUpdatedBy { get; init; }
    public required DateTimeOffset LastUpdatedAt { get; init; }
    public required bool Published { get; init; }
    public required Guid? PublishedBy { get; init; }
    public required DateTimeOffset? PublishedAt { get; init; }
    public required bool Archived { get; init; }
    public required Guid? ArchivedBy { get; init; }
    public required DateTimeOffset? ArchivedAt { get; init; }
    public required HashSet<Guid> AuthorIds { get; init; }

    public static PageContent FromDataModel(DataModels.Page page, DataModels.PageContent content)
    {
        return new PageContent
        {
            PageContentId = content.PageContentId,
            Page = new PageSummary
            {
                PageId = page.PageId,
                ParentPageId = page.ParentPageId,
                IsPublished = page.IsPublished,
                Name = page.Name,
                UriComponent = page.UriComponent
            },
            VersionNumber = content.VersionNumber,
            Content = content.Content,
            CreatedBy = content.CreatedBy,
            CreatedAt = content.CreatedAt,
            LastUpdatedBy = content.LastUpdatedBy,
            LastUpdatedAt = content.LastUpdatedAt,
            Published = content.Published,
            PublishedBy = content.PublishedBy,
            PublishedAt = content.PublishedAt,
            Archived = content.Archived,
            ArchivedBy = content.ArchivedBy,
            ArchivedAt = content.ArchivedAt,
            AuthorIds = content.AuthorIds
        };
    }
}

public record PageSummary
{
    public required Guid PageId { get; init; }
    public required Guid ParentPageId { get; init; }
    public required string UriComponent { get; init; }
    public required string Name { get; init; }
    public required bool IsPublished { get; init; }
}
