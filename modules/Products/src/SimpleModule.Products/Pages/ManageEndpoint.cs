using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Pages;

public class ManageEndpoint : IViewEndpoint
{
    public const string Route = ProductsConstants.Routes.Manage;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            Route,
            async (IProductContracts products) =>
                Inertia.Render(
                    "Products/Manage",
                    new { products = await products.GetAllProductsAsync() }
                )
        );
    }
}
