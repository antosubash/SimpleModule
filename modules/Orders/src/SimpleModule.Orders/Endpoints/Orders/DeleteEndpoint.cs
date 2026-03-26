using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}",
                (OrderId id, IOrderContracts orderContracts) =>
                    CrudEndpoints.Delete(() => orderContracts.DeleteOrderAsync(id))
            )
            .RequirePermission(OrdersPermissions.Delete);
}
