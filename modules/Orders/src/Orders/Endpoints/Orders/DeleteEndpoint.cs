using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "/{id}",
            async (int id, IOrderContracts orderContracts) =>
            {
                await orderContracts.DeleteOrderAsync(id);
                return TypedResults.NoContent();
            }
        );
    }
}
