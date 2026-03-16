using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Orders.Views;

public class EditEndpoint : IViewEndpoint
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
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

        app.MapPost(
            "/{id}",
            async (int id, HttpContext context, IOrderContracts orders) =>
            {
                var body = await JsonSerializer.DeserializeAsync<UpdateOrderPayload>(
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

        app.MapDelete(
            "/{id}",
            async (int id, IOrderContracts orders) =>
            {
                await orders.DeleteOrderAsync(id);
                return Results.Redirect("/orders");
            }
        );
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812")]
    private sealed class UpdateOrderPayload
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
