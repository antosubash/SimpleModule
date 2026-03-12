using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/",
            async (IProductContracts productContracts) =>
            {
                var products = await productContracts.GetAllProductsAsync();
                return TypedResults.Ok(products);
            }
        );
    }
}
