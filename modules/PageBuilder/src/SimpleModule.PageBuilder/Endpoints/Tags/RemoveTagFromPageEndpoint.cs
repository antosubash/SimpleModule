using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Tags;

public class RemoveTagFromPageEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}/tags/{tagId}",
                async (PageId id, PageTagId tagId, IPageBuilderTagContracts tags) =>
                {
                    await tags.RemoveTagFromPageAsync(id, tagId);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(PageBuilderPermissions.Update);
}
