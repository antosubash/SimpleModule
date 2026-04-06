using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class GetByIdEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.GetById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (ProductId id, IProductContracts productContracts) =>
                    CrudEndpoints.GetById(() => productContracts.GetProductByIdAsync(id))
            )
            .RequirePermission(ProductsPermissions.View);
}
