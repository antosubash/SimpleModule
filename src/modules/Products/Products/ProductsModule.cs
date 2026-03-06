using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Products.Contracts;
using SimpleModule.Products.Features.GetAllProducts;
using SimpleModule.Products.Features.GetProductById;

namespace SimpleModule.Products;

[Module("Products")]
public class ProductsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IProductContracts, ProductService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products");
        GetAllProductsEndpoint.Map(group);
        GetProductByIdEndpoint.Map(group);
    }
}
