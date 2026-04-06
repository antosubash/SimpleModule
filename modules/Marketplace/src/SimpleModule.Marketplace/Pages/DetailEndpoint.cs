using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Marketplace.Contracts;
using MarketplaceConstants = SimpleModule.Marketplace.Contracts.MarketplaceConstants;

namespace SimpleModule.Marketplace.Pages;

public class DetailEndpoint : IViewEndpoint
{
    public const string Route = MarketplaceConstants.Routes.Detail;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (IMarketplaceContracts marketplace, string id) =>
                {
                    var package = await marketplace.GetPackageDetailsAsync(id);
                    if (package is null)
                    {
                        return Results.NotFound();
                    }

                    return Inertia.Render("Marketplace/Detail", new { package });
                }
            )
            .AllowAnonymous();
    }
}
