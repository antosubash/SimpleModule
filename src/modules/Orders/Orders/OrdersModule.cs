using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Orders.Contracts;
using SimpleModule.Orders.Features.CreateOrder;
using SimpleModule.Orders.Features.GetAllOrders;
using SimpleModule.Orders.Features.GetOrderById;

namespace SimpleModule.Orders;

[Module("Orders")]
public class OrdersModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IOrderContracts, OrderService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders");
        GetAllOrdersEndpoint.Map(group);
        GetOrderByIdEndpoint.Map(group);
        CreateOrderEndpoint.Map(group);
    }
}
