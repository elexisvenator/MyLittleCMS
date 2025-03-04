using Vogen;

namespace MyLittleCMS.ApiService.DataModels.Events;

[ValueObject<string>]
public partial class PageContentId
{
    public static PageContentId From(PageId pageId, int versionNumber) => From($"{pageId.Value:N}-{versionNumber}");
}

// marker class
public sealed record PageContentStream;

public sealed record PageContentCreated(
    Guid PageId,
    int VersionNumber,
    string Content,
    Guid AuthorId);
public sealed record PageContentUpdated(string Content, Guid AuthorId);
public sealed record PageContentPublished(Guid AuthorId);
public sealed record PageContentArchived(Guid AuthorId);