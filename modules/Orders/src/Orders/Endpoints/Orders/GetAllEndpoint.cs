using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
                (IOrderContracts orderContracts) =>
                    CrudEndpoints.GetAll(orderContracts.GetAllOrdersAsync)
            )
            .RequirePermission(OrdersPermissions.View);
}
