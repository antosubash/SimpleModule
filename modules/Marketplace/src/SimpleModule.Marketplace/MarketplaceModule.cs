using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;

namespace SimpleModule.Marketplace;

[Module(
    MarketplaceConstants.ModuleName,
    RoutePrefix = MarketplaceConstants.RoutePrefix,
    ViewPrefix = "/marketplace"
)]
public class MarketplaceModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient(
            MarketplaceConstants.ModuleName,
            client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "SimpleModule-Marketplace");
            }
        );

        services.AddMemoryCache();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Marketplace",
                Url = "/marketplace/browse",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 100 4 2 2 0 000-4z"/></svg>""",
                Order = 40,
                Section = MenuSection.Navbar,
                RequiresAuth = false,
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Marketplace",
                Url = "/marketplace/browse",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 100 4 2 2 0 000-4z"/></svg>""",
                Order = 40,
                Section = MenuSection.AppSidebar,
                RequiresAuth = false,
            }
        );
    }
}
