using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Exceptions;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/",
            async (CreateOrderRequest request, IOrderContracts orderContracts) =>
            {
                var validation = CreateRequestValidator.Validate(request);
                if (!validation.IsValid)
                {
                    throw new ValidationException(validation.Errors);
                }

                var order = await orderContracts.CreateOrderAsync(request);
                return TypedResults.Created($"{OrdersConstants.RoutePrefix}/{order.Id}", order);
            }
        );
    }
}
