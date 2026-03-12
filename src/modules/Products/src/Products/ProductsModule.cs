using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.Products.Contracts;
using SimpleModule.Products.Features.GetAllProducts;
using SimpleModule.Products.Features.GetProductById;

namespace SimpleModule.Products;

[Module(ProductsConstants.ModuleName)]
public class ProductsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ProductsDbContext>(configuration, ProductsConstants.ModuleName);
        services.AddScoped<IProductContracts, ProductService>();
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
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(ProductsConstants.RoutePrefix);
        GetAllProductsEndpoint.Map(group);
        GetProductByIdEndpoint.Map(group);

        // Inertia page
        endpoints
            .MapGroup("/products")
            .MapGet(
                "/browse",
                async (IProductContracts products) =>
                    Inertia.Render(
                        "Products/Browse",
                        new { products = await products.GetAllProductsAsync() }
                    )
            );
    }
}
