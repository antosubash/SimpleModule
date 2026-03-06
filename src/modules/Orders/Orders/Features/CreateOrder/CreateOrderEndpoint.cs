using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.CreateOrder;

public static class CreateOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapPost("/", async (CreateOrderRequest request, IOrderContracts orderContracts) =>
        {
            var order = await orderContracts.CreateOrderAsync(request);
            return Results.Created($"/api/orders/{order.Id}", order);
        });
    }
}
