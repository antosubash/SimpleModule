using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Tags;

public class RemoveTagFromPageEndpoint : IEndpoint
{
    public const string Route = PageBuilderConstants.Routes.RemoveTag;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                async (PageId id, PageTagId tagId, IPageBuilderTagContracts tags) =>
                {
                    await tags.RemoveTagFromPageAsync(id, tagId);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(PageBuilderPermissions.Update);
}
