using FluentValidation;
using Marten;
using Marten.Events;
using Microsoft.AspNetCore.Mvc;
using MyLittleCMS.ApiService.DataModels.Events;
using MyLittleCMS.ApiService.Models;
using Wolverine.Http;
using Wolverine.Http.Marten;
using Wolverine.Marten;

namespace MyLittleCMS.ApiService.Endpoints.PageContent;

[Tags("PageContent")]
public static class PublishPageContentEndpoint
{
    public static ProblemDetails Validate(
        DataModels.Page page,
        DataModels.PageContent content)
    {
        if (content.PageId != page.PageId)
        {
            return new ProblemDetails
            {
                Detail = $"Content '{content.PageContentId}' is not for page '{page.PageId}'",
                Status = StatusCodes.Status400BadRequest
            };
        }

        if (page.DraftContentVersionId != content.PageContentId)
        {
            return new ProblemDetails
            {
                Detail = "Only the the current draft of the page content can be published",
                Status = StatusCodes.Status400BadRequest
            };
        }

        if (content.Archived)
        {
            return new ProblemDetails
            {
                Detail = "Page content is already archived",
                Status = StatusCodes.Status400BadRequest
            };
        }

        if (content.Published)
        {
            return new ProblemDetails
            {
                Detail = "Page content is already published",
                Status = StatusCodes.Status400BadRequest
            };
        }

        return WolverineContinue.NoProblems;
    }

    // load the currently published page content if there is one
    // so that we can archive it
    public static async Task<OtherContentVersion?> LoadAsync(IDocumentSession session, DataModels.Page page)
    {
        if (page.CurrentContentVersionId is null)
        {
            return null;
        }

        var eventStream = await session.Events.FetchForWriting<DataModels.PageContent>(page.CurrentContentVersionId!);
        return eventStream.Aggregate is null 
            ? new OtherContentVersion(eventStream) 
            : null;
    }

    [WolverinePost("{tenantId:int}/pages/{pageId:guid}/content/{pageContentId:minlength(32)}/publish", OperationId = "Publish Page Content")]
    [EmptyResponse]
    public static (IMartenOp, PageContentPublished) PublishPageContent(
        PublishPageContentRequest request,
        [Document(Required = true, MaybeSoftDeleted = false)] DataModels.Page page,
        [Aggregate] DataModels.PageContent content,
        OtherContentVersion? currentContentVersion)
    {
        if (currentContentVersion is not null)
        {
            currentContentVersion.Content.AppendOne(new PageContentArchived(request.AuthorUserId!.Value));
        }

        return
        (
            MartenOps.Store(page with
            {
                CurrentContentVersionId = content.PageContentId,
                CurrentContentVersionNumber = content.VersionNumber,
                DraftContentVersionId = null,
                DraftContentVersionNumber = null
            }),
            new PageContentPublished(request.AuthorUserId!.Value)
        );
    }

    public record OtherContentVersion(IEventStream<DataModels.PageContent> Content);
}


public record PublishPageContentRequest
{
    public Guid? AuthorUserId { get; init; }

    public class Validator : AbstractValidator<PublishPageContentRequest>
    {
        public Validator(IQuerySession session)
        {
            RuleFor(x => x.AuthorUserId).MustBeAnExistingActiveUser(session);
        }
    }
}