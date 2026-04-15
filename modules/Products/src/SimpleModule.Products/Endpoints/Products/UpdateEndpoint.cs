using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class UpdateEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.Update;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                async (
                    ProductId id,
                    UpdateProductRequest request,
                    IValidator<UpdateProductRequest> validator,
                    IProductContracts productContracts
                ) =>
                {
                    var validation = await validator.ValidateAsync(request);
                    if (!validation.IsValid)
                    {
                        throw new Core.Exceptions.ValidationException(
                            validation.ToValidationErrors()
                        );
                    }

                    return await CrudEndpoints.Update(() =>
                        productContracts.UpdateProductAsync(id, request)
                    );
                }
            )
            .RequirePermission(ProductsPermissions.Update);
}
