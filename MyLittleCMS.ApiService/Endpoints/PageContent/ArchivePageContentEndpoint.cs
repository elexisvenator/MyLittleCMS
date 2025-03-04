using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Mvc;
using MyLittleCMS.ApiService.DataModels.Events;
using MyLittleCMS.ApiService.Models;
using Wolverine.Http;
using Wolverine.Http.Marten;
using Wolverine.Marten;

namespace MyLittleCMS.ApiService.Endpoints.PageContent;

[Tags("PageContent")]
public static class ArchivePageContentEndpoint
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

        if (content.Archived)
        {
            return new ProblemDetails
            {
                Detail = "Page content is already archived",
                Status = StatusCodes.Status400BadRequest
            };
        }

        if (page.IsPublished && content.Published && content.PageContentId == page.CurrentContentVersionId)
        {
            return new ProblemDetails
            {
                Detail = "Cannot archive the currently published content version. Instead you should publish a newer content version.",
                Status = StatusCodes.Status400BadRequest
            };
        }

        return WolverineContinue.NoProblems;
    }

    [WolverinePost("{tenantId:int}/pages/{pageId:guid}/content/{pageContentId:minlength(32)}/archive", OperationId = "Archive Page Content")]
    [EmptyResponse]
    public static (IMartenOp, PageContentArchived) ArchivePageContent(
        ArchivePageContentRequest request,
        [Document(Required = true, MaybeSoftDeleted = false)] DataModels.Page page,
        [Aggregate] DataModels.PageContent content)
    {
        return
        (
            MartenOps.Store(page with
            {
                CurrentContentVersionId = page.CurrentContentVersionId == content.PageContentId ? null : page.CurrentContentVersionId,
                CurrentContentVersionNumber = page.CurrentContentVersionNumber == content.VersionNumber ? null : page.CurrentContentVersionNumber,
                DraftContentVersionId = page.DraftContentVersionId == content.PageContentId ? null : page.DraftContentVersionId,
                DraftContentVersionNumber = page.DraftContentVersionNumber == content.VersionNumber ? null : page.DraftContentVersionNumber
            }),
            new PageContentArchived(request.AuthorUserId!.Value)
        );
    }
}


public record ArchivePageContentRequest
{
    public Guid? AuthorUserId { get; init; }

    public class Validator : AbstractValidator<ArchivePageContentRequest>
    {
        public Validator(IQuerySession session)
        {
            RuleFor(x => x.AuthorUserId).MustBeAnExistingActiveUser(session);
        }
    }
}