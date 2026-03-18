using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class UpdateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", (ProductId id, UpdateProductRequest request, IProductContracts productContracts) =>
        {
            var validation = UpdateRequestValidator.Validate(request);
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            return CrudEndpoints.Update(() => productContracts.UpdateProductAsync(id, request));
        })
        .RequirePermission(ProductsPermissions.Update);
}
