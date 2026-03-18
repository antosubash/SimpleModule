using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

[RequirePermission(ProductsPermissions.View)]
public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", (IProductContracts productContracts) =>
            CrudEndpoints.GetAll(productContracts.GetAllProductsAsync))
            .RequirePermission(ProductsPermissions.View);
}
