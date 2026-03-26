using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class PermanentDeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}/permanent",
                (PageId id, IPageBuilderContracts pageBuilder) =>
                    CrudEndpoints.Delete(() => pageBuilder.PermanentDeletePageAsync(id))
            )
            .RequirePermission(PageBuilderPermissions.Delete);
}
