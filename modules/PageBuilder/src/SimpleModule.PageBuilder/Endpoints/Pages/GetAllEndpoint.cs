using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class GetAllEndpoint : IEndpoint
{
    public const string Route = PageBuilderConstants.Routes.GetAll;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (IPageBuilderContracts pageBuilder) =>
                    CrudEndpoints.GetAll(pageBuilder.GetAllPagesAsync)
            )
            .RequirePermission(PageBuilderPermissions.View);
}
