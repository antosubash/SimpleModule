using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Tags;

public class GetAllTagsEndpoint : IEndpoint
{
    public const string Route = PageBuilderConstants.Routes.GetAllTags;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (IPageBuilderTagContracts tags) => CrudEndpoints.GetAll(tags.GetAllTagsAsync)
            )
            .RequirePermission(PageBuilderPermissions.View);
}
