using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Ids;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/{id}",
            async Task<Results<Ok<Order>, NotFound>> (OrderId id, IOrderContracts orderContracts) =>
            {
                var order = await orderContracts.GetOrderByIdAsync(id);
                return order is not null ? TypedResults.Ok(order) : TypedResults.NotFound();
            }
        );
    }
}
