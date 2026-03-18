using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Exceptions;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/",
            async (CreateProductRequest request, IProductContracts productContracts) =>
            {
                var validation = CreateRequestValidator.Validate(request);
                if (!validation.IsValid)
                {
                    throw new ValidationException(validation.Errors);
                }

                var product = await productContracts.CreateProductAsync(request);
                return TypedResults.Created(
                    $"{ProductsConstants.RoutePrefix}/{product.Id}",
                    product
                );
            }
        );
    }
}
