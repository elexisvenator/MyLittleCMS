using System.ComponentModel;
using FluentValidation;
using MyLittleCMS.ApiService.Models;
using Wolverine.Http;
using Wolverine.Http.Marten;
using Wolverine.Marten;

namespace MyLittleCMS.ApiService.Endpoints.Page;

[Tags("Pages")]
public static class UpdatePageNameEndpoint
{
    [WolverinePut("{tenantId:int}/pages/{pageId:guid}/name", OperationId = "Update Page Name")]
    public static IMartenOp UpdateName(UpdatePageNameRequest request, [Document] DataModels.Page page)
    {
        var sanitizedName = Sanitize.Names(request.Name);
        if (page.Name == sanitizedName)
        {
            return MartenOps.Nothing();
        }

        return MartenOps.Store(page with { Name = sanitizedName });
    }
}

public sealed class UpdatePageNameRequest
{
    [Description("The page name")]
    public string? Name { get; set; }

    public sealed class Validator : AbstractValidator<UpdatePageNameRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
