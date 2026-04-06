using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class GetAllEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.GetAll;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (IProductContracts productContracts) =>
                    CrudEndpoints.GetAll(productContracts.GetAllProductsAsync)
            )
            .RequirePermission(ProductsPermissions.View);
}
