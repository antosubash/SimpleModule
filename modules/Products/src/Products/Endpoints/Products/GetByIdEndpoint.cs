using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Ids;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/{id}",
            async Task<Results<Ok<Product>, NotFound>> (
                ProductId id,
                IProductContracts productContracts
            ) =>
            {
                var product = await productContracts.GetProductByIdAsync(id);
                return product is not null ? TypedResults.Ok(product) : TypedResults.NotFound();
            }
        );
    }
}
