using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
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
