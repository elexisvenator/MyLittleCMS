using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;
using Wolverine.Http.Marten;

namespace MyLittleCMS.ApiService.Endpoints.PageContent;

[Tags("PageContent")]
public static class GetPageContentByIdEndpoint
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

        return WolverineContinue.NoProblems;
    }

    [WolverineGet("{tenantId:int}/pages/{pageId:guid}/content/{pageContentId:minlength(32)}", OperationId = "Get Page Content by Id")]
    public static ApiModels.PageContent GetPageContentById(
        [Document(Required = true, MaybeSoftDeleted = false)] DataModels.Page page,
        [Document(Required = true, MaybeSoftDeleted = false)] DataModels.PageContent content)
    {
        return ApiModels.PageContent.FromDataModel(page, content);
    }
}