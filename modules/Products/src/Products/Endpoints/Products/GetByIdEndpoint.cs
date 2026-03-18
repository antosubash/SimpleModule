using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Endpoints;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", (ProductId id, IProductContracts productContracts) =>
            CrudEndpoints.GetById(() => productContracts.GetProductByIdAsync(id)));
}
