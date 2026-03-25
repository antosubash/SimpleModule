using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Orders.Views;

public class CreateEndpoint : IViewEndpoint
{
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
            async (CreateOrderPayload body, IOrderContracts orders) =>
            {
                var request = new CreateOrderRequest
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

                await orders.CreateOrderAsync(request);
                return Results.Redirect("/orders");
            }
        );
    }

    internal sealed class CreateOrderPayload
    {
        public string UserId { get; set; } = string.Empty;
        public List<OrderItemPayload> Items { get; set; } = new();
    }

    internal sealed class OrderItemPayload
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
