using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
                (IPageBuilderContracts pageBuilder) =>
                    CrudEndpoints.GetAll(pageBuilder.GetAllPagesAsync)
            )
            .RequirePermission(PageBuilderPermissions.View);
}
