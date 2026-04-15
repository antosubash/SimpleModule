using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Core.Validation;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class CreateEndpoint : IEndpoint
{
    public const string Route = ProductsConstants.Routes.Create;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    CreateProductRequest request,
                    IValidator<CreateProductRequest> validator,
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

                    return await CrudEndpoints.Create(
                        () => productContracts.CreateProductAsync(request),
                        p => $"{ProductsConstants.RoutePrefix}/{p.Id}"
                    );
                }
            )
            .RequirePermission(ProductsPermissions.Create);
}
