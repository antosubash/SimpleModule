using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Products.Contracts;
using SimpleModule.Products.Features.GetAllProducts;
using SimpleModule.Products.Features.GetProductById;

namespace SimpleModule.Products;

[Module("Products")]
public class ProductsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProductsDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("ProductsConnection"))
        );
        services.AddScoped<IProductContracts, ProductService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products");
        GetAllProductsEndpoint.Map(group);
        GetProductByIdEndpoint.Map(group);
    }
}
