using System.ComponentModel;
using FluentValidation;
using Marten;
using Marten.Events;
using Marten.Services.BatchQuerying;
using Microsoft.AspNetCore.Mvc;
using MyLittleCMS.ApiService.DataModels;
using MyLittleCMS.ApiService.DataModels.Events;
using MyLittleCMS.ApiService.Models;
using Wolverine.Http;
using Wolverine.Http.Marten;
using Wolverine.Marten;
using Wolverine.Persistence;

namespace MyLittleCMS.ApiService.Endpoints.PageContent;

[Tags("PageContent")]
public static class NewPageContentDraftEndpoint
{
    public static async Task<(ProblemDetails, ExistingContentVersions?)> ValidateAsync(
        NewPageContentDraftRequest request,
        DataModels.Page page,
        IDocumentSession session,
        CancellationToken token)
    {
        if (!page.IsPublished && string.IsNullOrWhiteSpace(page.CurrentContentVersionId))
        {
            return (
                new ProblemDetails
                {
                    Detail = "Page does not have a current content version",
                    Status = StatusCodes.Status400BadRequest
                }, 
                null
            );
        }

        var currentDraft = page.DraftContentVersionId is not null
            ? await session.Events.FetchForWriting<DataModels.PageContent>(page.DraftContentVersionId, token)
            : null;

        var batch = session.CreateBatchQuery();

        var toCopyFrom = GetPageContentToCopyFrom(request, page, currentDraft, batch);
        var highestContentVersionNumber = batch.Query<DataModels.PageContent>()
            .Where(pc => pc.PageId == page.PageId)
            .OrderByDescending(pc => pc.VersionNumber)
            .Select(pc => pc.VersionNumber)
            .First();

        await batch.Execute(token);

        return (WolverineContinue.NoProblems, new ExistingContentVersions(currentDraft, await toCopyFrom, await highestContentVersionNumber));
    }

    private static Task<DataModels.PageContent> GetPageContentToCopyFrom(
        NewPageContentDraftRequest request,
        DataModels.Page page,
        IEventStream<DataModels.PageContent>? currentDraft,
        IBatchedQuery queryBatch)
    {
        if (!string.IsNullOrWhiteSpace(request.CopyFromContentVersionId))
        {
            if (request.CopyFromContentVersionId == currentDraft?.Aggregate?.PageContentId)
            {
                return Task.FromResult(currentDraft.Aggregate);
            }

            return queryBatch.Load<DataModels.PageContent>(request.CopyFromContentVersionId!)!;
        }

        // we need to get at least 1 page content
        // sort all content for the page
        // - by published first,
        // - then by archived last
        // - then by newest version number
        return queryBatch.Query<DataModels.PageContent>()
            .Where(pc => pc.PageId == page.PageId)
            .OrderByDescending(pc => pc.Published)
            .ThenBy(pc => pc.Archived)
            .ThenByDescending(pc => pc.VersionNumber)
            .First();
    }

    [WolverinePost("{tenantId:int}/pages/{pageId:guid}/content/new", OperationId = "Create New Page Content Draft")]
    public static (PageContentCreatedResponse, IMartenOp[]) CreateNewPageContentDraft(
        NewPageContentDraftRequest request,
        [Document(Required = true, MaybeSoftDeleted = false)] DataModels.Page page,
        ExistingContentVersions existing,
        TenantId tenantId)
    {
        var newVersionNumber = existing.CurrentMaxPageContentVersionNumber + 1;
        var newStreamKey = PageContentId.From(PageId.From(page.PageId), newVersionNumber);
        var newStream = MartenOps.StartStream<PageContentStream>(
            newStreamKey.Value,
            new PageContentCreated(
                page.PageId,
                newVersionNumber,
                existing.CopyFromContent.Content,
                request.AuthorUserId!.Value)
        );

        if (existing.CurrentDraft is not null)
        {
            existing.CurrentDraft.AppendOne(new PageContentArchived(request.AuthorUserId!.Value));
        }

        var updatePage = MartenOps.Store(
            page with
            {
                DraftContentVersionId = newStreamKey.Value, 
                DraftContentVersionNumber = newVersionNumber
            });

        return (
            new PageContentCreatedResponse(tenantId.Value, page.PageId, newStreamKey.Value),
            [newStream, updatePage]
        );
    }

    public record ExistingContentVersions(
        IEventStream<DataModels.PageContent>? CurrentDraft,
        DataModels.PageContent CopyFromContent, 
        int CurrentMaxPageContentVersionNumber);
}
public record NewPageContentDraftRequest
{
    public Guid? AuthorUserId { get; init; }
    public string? CopyFromContentVersionId { get; init; }

    public class Validator : AbstractValidator<NewPageContentDraftRequest>
    {
        public Validator(IDocumentSession session)
        {
            When(x => !string.IsNullOrWhiteSpace(x.CopyFromContentVersionId), () =>
            {
                RuleFor(x => x.CopyFromContentVersionId)
                    .MustAsync(async (copyFromContentVersionId, token) =>
                    {
                        var pageContent =
                            await session.LoadAsync<DataModels.PageContent>(copyFromContentVersionId!, token);
                        return pageContent is not null;
                    });
            });
            RuleFor(x => x.AuthorUserId).MustBeAnExistingActiveUser(session);
        }
    }
}

public sealed record PageContentCreatedResponse(
    [property: Description("The tenant id")]
    string TenantId,
    [property: Description("The page id")]
    Guid PageId,
    [property: Description("The page content id")]
    string PageContentId
) : CreationResponse($"/{TenantId}/pages/{PageId:N}/content/{PageContentId}");