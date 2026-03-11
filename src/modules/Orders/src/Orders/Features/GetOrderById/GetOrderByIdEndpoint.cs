using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Features.GetOrderById;

public static class GetOrderByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet(
            "/{id}",
            async Task<Results<Ok<Order>, NotFound>> (int id, IOrderContracts orderContracts) =>
            {
                var order = await orderContracts.GetOrderByIdAsync(id);
                return order is not null ? TypedResults.Ok(order) : TypedResults.NotFound();
            }
        );
    }
}
