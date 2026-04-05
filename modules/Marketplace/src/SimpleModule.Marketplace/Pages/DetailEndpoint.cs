using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Marketplace.Contracts;

namespace SimpleModule.Marketplace.Pages;

public class DetailEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/{id}",
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
