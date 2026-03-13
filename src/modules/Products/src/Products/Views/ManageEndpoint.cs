using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Views;

public class ManageEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/manage", async (IProductContracts products) =>
            Inertia.Render("Products/Manage",
                new { products = await products.GetAllProductsAsync() }));
    }
}
