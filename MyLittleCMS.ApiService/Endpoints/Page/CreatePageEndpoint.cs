using System.ComponentModel;
using FluentValidation;
using Marten;
using MyLittleCMS.ApiService.DataModels;
using MyLittleCMS.ApiService.DataModels.Events;
using MyLittleCMS.ApiService.Models;
using Wolverine.Http;
using Wolverine.Http.Marten;
using Wolverine.Marten;
using Wolverine.Persistence;

namespace MyLittleCMS.ApiService.Endpoints.Page;

[Tags("Pages")]
public static class CreatePageEndpoint
{
    [WolverinePost("{tenantId:int}/pages/{parentPageId:guid}/new", OperationId = "Create Page")]
    public static (PageCreatedResponse, IMartenOp, IMartenOp) CreatePage(
        CreatePageRequest request, 
        [Document("parentPageId", MaybeSoftDeleted = false, Required = true)]DataModels.Page parentPage,
        TenantId tenantId)
    {
        var pageId = PageId.New();
        const int pageVersion = 1;
        var pageContentId = PageContentId.From(pageId, pageVersion);

        var page = new DataModels.Page
        {
            PageId = pageId.Value,
            ParentPageId = parentPage.PageId,
            UriComponent = Sanitize.UriComponent(request.UriComponent).Value,
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

public record CreatePageRequest
{
    public string? UriComponent { get; init; }
    public string? Name { get; init; }
    public string? Content { get; init; }
    //public List<string> Tags { get; init; } = [];
    public Guid? AuthorUserId { get; init; }

    public class Validator : AbstractValidator<CreatePageRequest>
    {
        public Validator(IQuerySession session)
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.UriComponent).NotEmpty();
            RuleFor(x => x.AuthorUserId).MustBeAnExistingActiveUser(session);

        }
    }
}

public sealed record PageCreatedResponse(
    [property: Description("The tenant id")]
    string TenantId,
    [property: Description("The page id")]
    Guid PageId
) : CreationResponse($"/{TenantId}/pages/{PageId:N}");