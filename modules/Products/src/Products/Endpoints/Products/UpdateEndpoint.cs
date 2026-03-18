using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class UpdateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "/{id}",
            async Task<Results<Ok<Product>, NotFound>> (
                ProductId id,
                UpdateProductRequest request,
                IProductContracts productContracts
            ) =>
            {
                var product = await productContracts.UpdateProductAsync(id, request);
                return TypedResults.Ok(product);
            }
        );
    }
}
