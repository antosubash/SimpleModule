using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.GetAllOrders;

public static class GetAllOrdersEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/", async (IOrderContracts orderContracts) =>
        {
            var orders = await orderContracts.GetAllOrdersAsync();
            return Results.Ok(orders);
        });
    }
}
