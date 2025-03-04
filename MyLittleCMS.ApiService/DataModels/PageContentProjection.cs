using Marten.Events;
using Marten.Events.Aggregation;
using Marten.Metadata;
using Marten.Schema;
using MyLittleCMS.ApiService.DataModels.Events;

namespace MyLittleCMS.ApiService.DataModels;

public record PageContent : IRevisioned
{
    [Identity]
    public required string PageContentId { get; init; }

    [DuplicateField]
    public Guid PageId { get; init; }
    public int VersionNumber { get; init; }
    public string Content { get; init; } = "";
    public Guid CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid LastUpdatedBy { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
    public bool Published { get; init; }
    public Guid? PublishedBy { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public bool Archived { get; init; }
    public Guid? ArchivedBy { get; init; }
    public DateTimeOffset? ArchivedAt { get; init; }
    public HashSet<Guid> AuthorIds { get; init; } = [];

    /// <summary>
    /// Marten's version tracking
    /// </summary>
    public int Version { get; set; }

    public class Projector : SingleStreamProjection<PageContent>
    {
        public static PageContent Create(PageContentCreated @event, IEvent metadata) =>
            new()
            {
                PageContentId = metadata.StreamKey!,
                PageId = @event.PageId,
                VersionNumber = @event.VersionNumber,
                Content = @event.Content,
                CreatedAt = metadata.Timestamp,
                CreatedBy = @event.AuthorId,
                LastUpdatedAt = metadata.Timestamp,
                LastUpdatedBy = @event.AuthorId,
                AuthorIds = [@event.AuthorId]
            };

        public PageContent Apply(PageContentUpdated @event, IEvent metadata, PageContent current) =>
            current with
            {
                Content = @event.Content,
                LastUpdatedAt = metadata.Timestamp,
                LastUpdatedBy = @event.AuthorId,
                AuthorIds = [.. current.AuthorIds, @event.AuthorId]
            };

        public PageContent Apply(PageContentPublished @event, IEvent metadata, PageContent current) =>
            current with
            {
                Published = true,
                PublishedAt = metadata.Timestamp,
                PublishedBy = @event.AuthorId
            };

        public PageContent Apply(PageContentArchived @event, IEvent metadata, PageContent current) =>
            current with
            {
                Archived = true,
                ArchivedAt = metadata.Timestamp,
                ArchivedBy = @event.AuthorId
            };
    }
}