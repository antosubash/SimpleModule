using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Orders.Contracts;
using OrdersConstants = SimpleModule.Orders.Contracts.OrdersConstants;

namespace SimpleModule.Orders.Endpoints.Orders;

public class GetAllEndpoint : IEndpoint
{
    public const string Route = OrdersConstants.Routes.GetAll;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (IOrderContracts orderContracts) =>
                    CrudEndpoints.GetAll(orderContracts.GetAllOrdersAsync)
            )
            .RequirePermission(OrdersPermissions.View);
}
