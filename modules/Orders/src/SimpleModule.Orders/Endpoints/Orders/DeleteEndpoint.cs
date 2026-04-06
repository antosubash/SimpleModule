using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Orders.Contracts;
using OrdersConstants = SimpleModule.Orders.Contracts.OrdersConstants;

namespace SimpleModule.Orders.Endpoints.Orders;

public class DeleteEndpoint : IEndpoint
{
    public const string Route = OrdersConstants.Routes.Delete;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                (OrderId id, IOrderContracts orderContracts) =>
                    CrudEndpoints.Delete(() => orderContracts.DeleteOrderAsync(id))
            )
            .RequirePermission(OrdersPermissions.Delete);
}
