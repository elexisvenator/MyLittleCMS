using MyLittleCMS.ApiService.Endpoints.Page;

namespace MyLittleCMS.ApiService.ApiModels;

public record Page
{
    public required Guid PageId { get; init; }
    public required Guid ParentPageId { get; init; }
    public required string UriComponent { get; init; }
    public required string ParentUriComponent { get; init; }
    public required string Name { get; init; }
    public required bool IsPublished { get; init; }
    public required PageContentSummary? PageContent { get; init; }
    public required Guid CreatedBy { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    internal static Page FromDataModel(DataModels.Page page, ParentPage? parent, DataModels.PageContent? content) =>
        new()
        {
            PageId = page.PageId,
            ParentPageId = page.ParentPageId,
            UriComponent = page.UriComponent,
            ParentUriComponent = parent?.Page.UriComponent ?? "",
            Name = page.Name,
            IsPublished = page.IsPublished,
            PageContent = content is null
                ? null
                : new PageContentSummary
                {
                    PageContentId = content.PageContentId,
                    VersionNumber = content.VersionNumber,
                    Content = content.Content,
                    LastModifiedBy = content.PublishedAt > content.LastUpdatedAt
                        ? content.PublishedBy!.Value
                        : content.LastUpdatedBy,
                    LastModifiedAt = content.PublishedAt > content.LastUpdatedAt
                        ? content.PublishedAt.Value
                        : content.LastUpdatedAt
                },
            CreatedBy = page.CreatedBy,
            CreatedAt = page.CreatedAt
        };
}

public record PageContentSummary
{
    public required string PageContentId { get; init; }
    public required int VersionNumber { get; init; }
    public required string Content { get; init; }
    public required Guid LastModifiedBy { get; init; }
    public required DateTimeOffset LastModifiedAt { get; init; }
}
