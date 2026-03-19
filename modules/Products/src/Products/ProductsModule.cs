using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products;

[Module(
    ProductsConstants.ModuleName,
    RoutePrefix = ProductsConstants.RoutePrefix,
    ViewPrefix = "/products"
)]
public class ProductsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ProductsDbContext>(configuration, ProductsConstants.ModuleName);
        services.AddScoped<IProductContracts, ProductService>();
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<ProductsPermissions>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Products",
                Url = "/products/browse",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/></svg>""",
                Order = 30,
                Section = MenuSection.Navbar,
                RequiresAuth = false,
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Manage Products",
                Url = "/products/manage",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>""",
                Order = 31,
                Section = MenuSection.Navbar,
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Products",
                Url = "/products/browse",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/></svg>""",
                Order = 20,
                Section = MenuSection.AppSidebar,
            }
        );
    }
}
