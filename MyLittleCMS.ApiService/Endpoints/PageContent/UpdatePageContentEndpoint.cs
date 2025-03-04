using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Mvc;
using MyLittleCMS.ApiService.DataModels.Events;
using MyLittleCMS.ApiService.Models;
using Wolverine.Http;
using Wolverine.Http.Marten;

namespace MyLittleCMS.ApiService.Endpoints.PageContent;

[Tags("PageContent")]
public static class UpdatePageContentEndpoint
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
                Detail = "Page content is not a draft",
                Status = StatusCodes.Status400BadRequest
            };
        }

        return WolverineContinue.NoProblems;
    }

    [WolverinePut("{tenantId:int}/pages/{pageId:guid}/content/{pageContentId:minlength(32)}", OperationId = "Update Page Content")]
    [EmptyResponse]
    public static PageContentUpdated? UpdatePageContent(
        UpdatePageContentRequest request,
        [Document(Required = true, MaybeSoftDeleted = false)] DataModels.Page page,
        [Aggregate] DataModels.PageContent content)
    {
        var sanitizedContent = Sanitize.PageContent(request.Content);
        if (content.Content == sanitizedContent)
        {
            return null;
        }

        return new PageContentUpdated(sanitizedContent, request.AuthorUserId!.Value);
    }
}


public record UpdatePageContentRequest
{
    public Guid? AuthorUserId { get; init; }
    public string? Content { get; init; }

    public class Validator : AbstractValidator<UpdatePageContentRequest>
    {
        public Validator(IQuerySession session)
        {
            RuleFor(x => x.AuthorUserId).MustBeAnExistingActiveUser(session);
        }
    }
}