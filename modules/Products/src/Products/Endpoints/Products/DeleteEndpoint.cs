using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}",
                (ProductId id, IProductContracts productContracts) =>
                    CrudEndpoints.Delete(() => productContracts.DeleteProductAsync(id))
            )
            .RequirePermission(ProductsPermissions.Delete);
}
