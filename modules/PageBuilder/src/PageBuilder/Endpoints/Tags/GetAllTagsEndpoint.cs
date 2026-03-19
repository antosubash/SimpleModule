using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Tags;

public class GetAllTagsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/tags",
                async (IPageBuilderContracts pageBuilder) =>
                    TypedResults.Ok(await pageBuilder.GetAllTagsAsync())
            )
            .RequirePermission(PageBuilderPermissions.View);
}
