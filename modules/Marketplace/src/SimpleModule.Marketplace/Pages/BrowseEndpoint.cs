using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Marketplace.Contracts;
using MarketplaceConstants = SimpleModule.Marketplace.Contracts.MarketplaceConstants;

namespace SimpleModule.Marketplace.Pages;

public class BrowseEndpoint : IViewEndpoint
{
    public const string Route = MarketplaceConstants.Routes.Browse;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (
                    IMarketplaceContracts marketplace,
                    string? q,
                    MarketplaceCategory? category,
                    MarketplaceSortOption? sort,
                    int? skip
                ) =>
                {
                    var pageSize = 24;
                    var skipCount = skip ?? 0;

                    var result = await marketplace.SearchPackagesAsync(
                        new MarketplaceSearchRequest
                        {
                            Query = q,
                            Category = category,
                            SortBy = sort ?? MarketplaceSortOption.Relevance,
                            Skip = skipCount,
                            Take = pageSize,
                        }
                    );

                    var packages = result.Packages;
                    var hasMore = skipCount + packages.Count < result.TotalHits;

                    return Inertia.Render(
                        "Marketplace/Browse",
                        new
                        {
                            packages,
                            totalHits = result.TotalHits,
                            query = q ?? string.Empty,
                            selectedCategory = category?.ToString() ?? "All",
                            selectedSort = sort?.ToString() ?? "Relevance",
                            skip = skipCount,
                            hasMore,
                        }
                    );
                }
            )
            .AllowAnonymous();
    }
}
