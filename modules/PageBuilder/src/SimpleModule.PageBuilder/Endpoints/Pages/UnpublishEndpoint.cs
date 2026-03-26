using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class UnpublishEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/{id}/unpublish",
                async (PageId id, IPageBuilderContracts pageBuilder) =>
                {
                    var page = await pageBuilder.UnpublishPageAsync(id);
                    return TypedResults.Ok(page);
                }
            )
            .RequirePermission(PageBuilderPermissions.Publish);
}
