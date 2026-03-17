using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Views;

public class CreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/create", () => Inertia.Render("Products/Create"));

        app.MapPost(
            "/",
            async (
                [FromForm] string name,
                [FromForm] decimal price,
                IProductContracts products
            ) =>
            {
                var request = new CreateProductRequest { Name = name, Price = price };
                await products.CreateProductAsync(request);
                return Results.Redirect("/products/manage");
            }
        ).DisableAntiforgery();
    }
}
