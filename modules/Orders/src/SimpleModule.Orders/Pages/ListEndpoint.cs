using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Orders.Contracts;
using OrdersConstants = SimpleModule.Orders.Contracts.OrdersConstants;

namespace SimpleModule.Orders.Pages;

public class ListEndpoint : IViewEndpoint
{
    public const string Route = OrdersConstants.Routes.List;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            Route,
            async (IOrderContracts orders) =>
                Inertia.Render("Orders/List", new { orders = await orders.GetAllOrdersAsync() })
        );
    }
}
