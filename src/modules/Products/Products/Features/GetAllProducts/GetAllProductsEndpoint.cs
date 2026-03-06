using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Features.GetAllProducts;

public static class GetAllProductsEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/", async (IProductContracts productContracts) =>
        {
            var products = await productContracts.GetAllProductsAsync();
            return Results.Ok(products);
        });
    }
}
