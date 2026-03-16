using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Orders.Views;

public class CreateEndpoint : IViewEndpoint
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/create",
            async (IProductContracts products) =>
                Inertia.Render(
                    "Orders/Create",
                    new { products = await products.GetAllProductsAsync() }
                )
        );

        app.MapPost(
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
    }

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
