using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class PermanentDeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}/permanent",
                async (PageId id, IPageBuilderContracts pageBuilder) =>
                {
                    await pageBuilder.PermanentDeletePageAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(PageBuilderPermissions.Delete);
}
