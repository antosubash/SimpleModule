using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Endpoints.Menus;

public class ReorderMenuItemsEndpoint : IEndpoint
{
    public const string Route = SettingsConstants.Routes.Api.ReorderMenuItems;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (ReorderMenuItemsRequest request, PublicMenuService service) =>
                {
                    await service.ReorderAsync(request);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(SettingsPermissions.ManageMenus);
}
