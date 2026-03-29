using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Marketplace.Contracts;

namespace SimpleModule.Marketplace.Views;

[ViewPage("Marketplace/Browse")]
public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/browse",
                async (
                    IMarketplaceContracts marketplace,
                    string? q,
                    MarketplaceCategory? category,
                    MarketplaceSortOption? sort
                ) =>
                {
                    var result = await marketplace.SearchPackagesAsync(
                        new MarketplaceSearchRequest
                        {
                            Query = q,
                            Category = category,
                            SortBy = sort ?? MarketplaceSortOption.Relevance,
                            Take = 50,
                        }
                    );

                    return Inertia.Render(
                        "Marketplace/Browse",
                        new
                        {
                            packages = result.Packages,
                            totalHits = result.TotalHits,
                            query = q ?? string.Empty,
                            selectedCategory = category?.ToString() ?? "All",
                            selectedSort = sort?.ToString() ?? "Relevance",
                        }
                    );
                }
            )
            .AllowAnonymous();
    }
}
