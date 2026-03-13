using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Views;

public class EditEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id}/edit", async (int id, IProductContracts products) =>
        {
            var product = await products.GetProductByIdAsync(id);
            if (product is null)
                return Results.NotFound();

            return Inertia.Render("Products/Edit", new { product });
        });
    }
}
