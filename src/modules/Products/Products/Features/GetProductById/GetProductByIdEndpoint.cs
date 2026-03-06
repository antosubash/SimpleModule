using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Features.GetProductById;

public static class GetProductByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/{id}", async (int id, IProductContracts productContracts) =>
        {
            var product = await productContracts.GetProductByIdAsync(id);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        });
    }
}
