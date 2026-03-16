using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Orders;

[Module(OrdersConstants.ModuleName, RoutePrefix = OrdersConstants.RoutePrefix)]
public class OrdersModule : IModule
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<OrdersDbContext>(configuration, OrdersConstants.ModuleName);
        services.AddScoped<IOrderContracts, OrderService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Orders",
                Url = "/orders",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01"/></svg>""",
                Order = 40,
                Section = MenuSection.Navbar,
            }
        );
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/orders").WithTags(OrdersConstants.ModuleName);

        // List orders
        group.MapGet(
            "/",
            async (IOrderContracts orders) =>
                Inertia.Render("Orders/List", new { orders = await orders.GetAllOrdersAsync() })
        );

        // Create order page
        group.MapGet(
            "/create",
            async (IProductContracts products) =>
                Inertia.Render(
                    "Orders/Create",
                    new { products = await products.GetAllProductsAsync() }
                )
        );

        // Edit order page
        group.MapGet(
            "/{id}/edit",
            async (int id, IOrderContracts orders, IProductContracts products) =>
            {
                var order = await orders.GetOrderByIdAsync(id);
                if (order is null)
                    return Results.NotFound();

                return Inertia.Render(
                    "Orders/Edit",
                    new
                    {
                        order = new
                        {
                            id = order.Id,
                            userId = order.UserId,
                            items = order.Items.Select(i => new
                            {
                                productId = i.ProductId,
                                quantity = i.Quantity,
                            }),
                            total = order.Total,
                            createdAt = order.CreatedAt.ToString("O"),
                        },
                        products = await products.GetAllProductsAsync(),
                    }
                );
            }
        );

        // Create order (POST)
        group.MapPost(
            "/",
            async (HttpContext context, IOrderContracts orders) =>
            {
                var body = await JsonSerializer.DeserializeAsync<CreateOrderPayload>(
                    context.Request.Body,
                    JsonOptions
                );

                var request = new CreateOrderRequest
                {
                    UserId = body!.UserId,
                    Items = body
                        .Items.Select(i => new OrderItem
                        {
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                        })
                        .ToList(),
                };

                await orders.CreateOrderAsync(request);
                return Results.Redirect("/orders");
            }
        );

        // Update order (POST)
        group.MapPost(
            "/{id}",
            async (int id, HttpContext context, IOrderContracts orders) =>
            {
                var body = await JsonSerializer.DeserializeAsync<CreateOrderPayload>(
                    context.Request.Body,
                    JsonOptions
                );

                var request = new UpdateOrderRequest
                {
                    UserId = body!.UserId,
                    Items = body
                        .Items.Select(i => new OrderItem
                        {
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                        })
                        .ToList(),
                };

                await orders.UpdateOrderAsync(id, request);
                return Results.Redirect($"/orders/{id}/edit");
            }
        );

        // Delete order
        group.MapDelete(
            "/{id}",
            async (int id, IOrderContracts orders) =>
            {
                await orders.DeleteOrderAsync(id);
                return Results.Redirect("/orders");
            }
        );
    }

    // Used by JSON deserialization
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812")]
    private sealed class CreateOrderPayload
    {
        public string UserId { get; set; } = string.Empty;
        public List<OrderItemPayload> Items { get; set; } = new();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812")]
    private sealed class OrderItemPayload
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
