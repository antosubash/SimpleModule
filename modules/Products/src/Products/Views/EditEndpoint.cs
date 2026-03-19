using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Views;

public class EditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/{id}/edit",
            async (ProductId id, IProductContracts products) =>
            {
                var product = await products.GetProductByIdAsync(id);
                if (product is null)
                    return Results.NotFound();

                return Inertia.Render("Products/Edit", new { product });
            }
        );

        app.MapPost(
                "/{id}",
                async (
                    ProductId id,
                    [FromForm] string name,
                    [FromForm] decimal price,
                    IProductContracts products
                ) =>
                {
                    var request = new UpdateProductRequest { Name = name, Price = price };
                    await products.UpdateProductAsync(id, request);
                    return Results.Redirect($"/products/{id}/edit");
                }
            )
            .DisableAntiforgery();

        app.MapDelete(
            "/{id}",
            async (ProductId id, IProductContracts products) =>
            {
                await products.DeleteProductAsync(id);
                return Results.Redirect("/products/manage");
            }
        );
    }
}
