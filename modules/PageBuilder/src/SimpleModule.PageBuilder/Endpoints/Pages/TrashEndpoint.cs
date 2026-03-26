using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class TrashEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/trash",
                (IPageBuilderContracts pageBuilder) =>
                    CrudEndpoints.GetAll(pageBuilder.GetTrashedPagesAsync)
            )
            .RequirePermission(PageBuilderPermissions.Delete);
}
