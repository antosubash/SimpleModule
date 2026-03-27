using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Views;

[ViewPage("Settings/MenuManager")]
public class MenuManagerEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/menus",
                async (PublicMenuService service, IReadOnlyList<AvailablePage> availablePages) =>
                    Inertia.Render(
                        "Settings/MenuManager",
                        new { menus = await service.GetAllAsync(), availablePages }
                    )
            )
            .RequireAuthorization();
    }
}
