using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.GetOrderById;

public static class GetOrderByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/{id}", async (int id, IOrderContracts orderContracts) =>
        {
            var order = await orderContracts.GetOrderByIdAsync(id);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        });
    }
}
