using System.ComponentModel;
using FluentValidation;
using Marten;
using MyLittleCMS.ApiService.Models;
using Wolverine.Http;
using Wolverine.Http.Marten;
using Wolverine.Marten;

namespace MyLittleCMS.ApiService.Endpoints.Page;

[Tags("Pages")]
public static class UpdatePageUriComponentEndpoint
{
    [WolverinePut("{tenantId:int}/pages/{pageId:guid}/path", OperationId = "Update Page UriComponent")]
    public static IMartenOp UpdateUriComponent(
        UpdatePageUriComponentRequest request, 
        [Document(Required = true, MaybeSoftDeleted = false)] DataModels.Page page)
    {
        var sanitizedUriComponent = Sanitize.UriComponent(request.UriComponent);
        if (page.UriComponent == sanitizedUriComponent)
        {
            return MartenOps.Nothing();
        }

        return MartenOps.Store(page with { UriComponent = sanitizedUriComponent.Value });
    }
}

public sealed class UpdatePageUriComponentRequest
{
    [Description("The page uri component")]
    public string? UriComponent { get; set; }

    public sealed class Validator : AbstractValidator<UpdatePageUriComponentRequest>
    {
        public Validator(/*DataModels.Page page,*/ IQuerySession session)
        {
            RuleFor(x => x.UriComponent).Cascade(CascadeMode.Stop)
                .NotEmpty();
            //.When(_ => page.ParentPageId != Guid.Empty).WithMessage("{PropertyName} cannot be set on the root page")
            //.WhenAsync(async (request, token) =>
            //{
            //    var sanitizedUriComponent = Sanitize.UriComponent(request.UriComponent);
            //    if (page.UriComponent == sanitizedUriComponent)
            //    {
            //        return true;
            //    }

            //    var newParentAndUriComponent =
            //        DataModels.Page.BuildParentAndUriComponent(page.ParentPageId, sanitizedUriComponent.Value);
            //    return await session.Query<DataModels.Page>()
            //        .AnyAsync(p => p.ParentAndUriComponent == newParentAndUriComponent, token);
            //});
        }
    }
}
