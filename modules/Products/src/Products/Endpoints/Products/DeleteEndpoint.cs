using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Ids;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "/{id}",
            async (ProductId id, IProductContracts productContracts) =>
            {
                await productContracts.DeleteProductAsync(id);
                return TypedResults.NoContent();
            }
        );
    }
}
