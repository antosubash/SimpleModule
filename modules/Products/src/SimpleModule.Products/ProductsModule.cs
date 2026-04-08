using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.FeatureFlags;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products;

[Module(
    ProductsConstants.ModuleName,
    RoutePrefix = ProductsConstants.RoutePrefix,
    ViewPrefix = ProductsConstants.ViewPrefix
)]
public class ProductsModule : IModule, IModuleServices, IModuleMenu
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ProductsDbContext>(configuration, ProductsConstants.ModuleName);
    }

    public void ConfigureFeatureFlags(IFeatureFlagBuilder builder)
    {
        builder
            .Add(
                new FeatureFlagDefinition
                {
                    Name = ProductsFeatures.BulkImport,
                    Description = "Enable bulk product import via CSV upload",
                    DefaultEnabled = false,
                }
            )
            .Add(
                new FeatureFlagDefinition
                {
                    Name = ProductsFeatures.AdvancedPricing,
                    Description = "Enable tiered and time-based pricing rules",
                    DefaultEnabled = true,
                }
            );
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Products",
                Url = "/products",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/></svg>""",
                Order = 20,
                Section = MenuSection.AppSidebar,
            }
        );
    }
}
