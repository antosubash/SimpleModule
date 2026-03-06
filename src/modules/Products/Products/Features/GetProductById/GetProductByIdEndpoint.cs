using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Features.GetProductById;

public static class GetProductByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet(
            "/{id}",
            async Task<Results<Ok<Product>, NotFound>> (int id, IProductContracts productContracts) =>
            {
                var product = await productContracts.GetProductByIdAsync(id);
                return product is not null ? TypedResults.Ok(product) : TypedResults.NotFound();
            }
        );
    }
}
