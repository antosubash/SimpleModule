using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Pages;

public class BrowseEndpoint : IViewEndpoint
{
    public const string Route = ProductsConstants.Routes.Browse;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (IProductContracts products, IOptions<ProductsModuleOptions> options) =>
                    Inertia.Render(
                        "Products/Browse",
                        new
                        {
                            products = await products.GetAllProductsAsync(),
                            pageSize = options.Value.DefaultPageSize,
                        }
                    )
            )
            .AllowAnonymous();
    }
}
