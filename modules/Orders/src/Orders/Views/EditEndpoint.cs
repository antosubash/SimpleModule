using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Orders.Views;

public class EditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/{id}/edit",
            async (OrderId id, IOrderContracts orders, IProductContracts products) =>
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
                            id = order.Id.Value,
                            userId = order.UserId.Value,
                            items = order.Items.Select(i => new
                            {
                                productId = i.ProductId.Value,
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

        app.MapPost(
            "/{id}",
            async (OrderId id, UpdateOrderPayload body, IOrderContracts orders) =>
            {
                var request = new UpdateOrderRequest
                {
                    UserId = UserId.From(body.UserId),
                    Items = body
                        .Items.Select(i => new OrderItem
                        {
                            ProductId = ProductId.From(i.ProductId),
                            Quantity = i.Quantity,
                        })
                        .ToList(),
                };

                await orders.UpdateOrderAsync(id, request);
                return Results.Redirect($"/orders/{id}/edit");
            }
        );

        app.MapDelete(
            "/{id}",
            async (OrderId id, IOrderContracts orders) =>
            {
                await orders.DeleteOrderAsync(id);
                return Results.Redirect("/orders");
            }
        );
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812")]
    internal sealed class UpdateOrderPayload
    {
        public string UserId { get; set; } = string.Empty;
        public List<OrderItemPayload> Items { get; set; } = new();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812")]
    internal sealed class OrderItemPayload
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
