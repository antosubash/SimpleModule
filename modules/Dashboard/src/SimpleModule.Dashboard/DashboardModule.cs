using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.Dashboard.Contracts;

namespace SimpleModule.Dashboard;

[Module(
    DashboardConstants.ModuleName,
    RoutePrefix = DashboardConstants.RoutePrefix,
    ViewPrefix = "/"
)]
public class DashboardModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDashboardContracts, DashboardContractsService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Dashboard",
                Url = "/",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-4 0a1 1 0 01-1-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 01-1 1h-2z"/></svg>""",
                Order = 10,
                Section = MenuSection.AppSidebar,
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Broadcasting",
                Url = DashboardConstants.Routes.Views.Broadcasting,
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M3 9.75c4-4 8-4 12 0M6 12.75c2.5-2.5 5.5-2.5 8 0M9 15.75c1-1 2-1 3 0M12 19.5h.01" /></svg>""",
                Order = 20,
                Section = MenuSection.AppSidebar,
            }
        );
    }
}
