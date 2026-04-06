using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings.Pages;

public class MenuManagerEndpoint : IViewEndpoint
{
    public const string Route = SettingsConstants.Routes.Views.MenuManager;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (PublicMenuService service, IReadOnlyList<AvailablePage> availablePages) =>
                    Inertia.Render(
                        "Settings/MenuManager",
                        new { menuItems = await service.GetAllAsync(), availablePages }
                    )
            )
            .RequireAuthorization();
    }
}
