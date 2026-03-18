using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Exceptions;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", (CreateProductRequest request, IProductContracts productContracts) =>
        {
            var validation = CreateRequestValidator.Validate(request);
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            return CrudEndpoints.Create(
                () => productContracts.CreateProductAsync(request),
                p => $"{ProductsConstants.RoutePrefix}/{p.Id}");
        })
        .RequirePermission(ProductsPermissions.Create);
}
