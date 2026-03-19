using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}",
                (OrderId id, IOrderContracts orderContracts) =>
                    CrudEndpoints.GetById(() => orderContracts.GetOrderByIdAsync(id))
            )
            .RequirePermission(OrdersPermissions.View);
}
