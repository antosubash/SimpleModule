using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Orders.Contracts;
using OrdersConstants = SimpleModule.Orders.Contracts.OrdersConstants;

namespace SimpleModule.Orders.Endpoints.Orders;

public class GetByIdEndpoint : IEndpoint
{
    public const string Route = OrdersConstants.Routes.GetById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (OrderId id, IOrderContracts orderContracts) =>
                    CrudEndpoints.GetById(() => orderContracts.GetOrderByIdAsync(id))
            )
            .RequirePermission(OrdersPermissions.View);
}
