using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core.Exceptions;
using SimpleModule.Orders;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.CreateOrder;

public static class CreateOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapPost(
            "/",
            async (CreateOrderRequest request, IOrderContracts orderContracts) =>
            {
                var validation = CreateOrderRequestValidator.Validate(request);
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
