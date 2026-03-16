using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/",
            async (IOrderContracts orderContracts) =>
            {
                var orders = await orderContracts.GetAllOrdersAsync();
                return TypedResults.Ok(orders);
            }
        );
    }
}
