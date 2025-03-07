using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Mvc;
using MyLittleCMS.ApiService.DataModels;
using MyLittleCMS.ApiService.DataModels.Events;
using MyLittleCMS.ApiService.Models;
using Wolverine.Http;
using Wolverine.Marten;
using Wolverine.Persistence;

namespace MyLittleCMS.ApiService.Endpoints.Page;

[Tags("Pages")]
public static class CreateRootPageEndpoint
{
    public static async Task<ProblemDetails> ValidateAsync(IQuerySession session, CancellationToken token)
    {
        var rootPageExists = await session.Query<DataModels.Page>().AnyAsync(p => p.ParentAndUriComponent == "", token);
        if (rootPageExists)
        {
            return new ProblemDetails
            {
                Detail = "Root page already exists",
                Status = StatusCodes.Status400BadRequest
            };
        }

        return WolverineContinue.NoProblems;
    }

    [WolverinePost("{tenantId:int}/pages/new", OperationId = "Create Root Page")]
    public static (PageCreatedResponse, IMartenOp, IMartenOp) CreatePage(CreateRootPageRequest request, TenantId tenantId)
    {
        var pageId = PageId.New();
        const int pageVersion = 1;
        var pageContentId = PageContentId.From(pageId, pageVersion);

        var page = new DataModels.Page
        {
            PageId = pageId.Value,
            ParentPageId = Guid.Empty,
            UriComponent = "",
            Name = Sanitize.Names(request.Name),
            CreatedBy = request.AuthorUserId!.Value,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var newStream = MartenOps.StartStream<PageContentStream>(
            pageContentId.Value,
            new PageContentCreated(
                page.PageId, 
                pageVersion, 
                Sanitize.PageContent(request.Content),
                request.AuthorUserId!.Value));

        return (
            new PageCreatedResponse(tenantId.Value, pageId.Value),
            MartenOps.Insert(page),
            newStream
        );
    }
}

public record CreateRootPageRequest
{
    public string? Name { get; init; }
    public string? Content { get; init; }
    //public List<string> Tags { get; init; } = [];
    public Guid? AuthorUserId { get; init; }

    public class Validator : AbstractValidator<CreateRootPageRequest>
    {
        public Validator(IQuerySession session)
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.AuthorUserId).MustBeAnExistingActiveUser(session);

        }
    }
}