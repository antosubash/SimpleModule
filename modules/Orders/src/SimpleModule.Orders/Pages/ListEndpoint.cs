using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Views;

[ViewPage("Orders/List")]
public class ListEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/",
            async (IOrderContracts orders) =>
                Inertia.Render("Orders/List", new { orders = await orders.GetAllOrdersAsync() })
        );
    }
}
