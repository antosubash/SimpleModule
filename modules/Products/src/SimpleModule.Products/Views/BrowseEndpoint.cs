using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Views;

[ViewPage("Products/Browse")]
public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/browse",
                async (IProductContracts products) =>
                    Inertia.Render(
                        "Products/Browse",
                        new { products = await products.GetAllProductsAsync() }
                    )
            )
            .AllowAnonymous();
    }
}
