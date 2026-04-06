using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Marketplace.Contracts;
using MarketplaceConstants = SimpleModule.Marketplace.Contracts.MarketplaceConstants;

namespace SimpleModule.Marketplace.Endpoints.Marketplace;

public class SearchEndpoint : IEndpoint
{
    public const string Route = MarketplaceConstants.Routes.Search;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (
                    IMarketplaceContracts marketplace,
                    string? q,
                    MarketplaceCategory? category,
                    MarketplaceSortOption? sort,
                    int? skip,
                    int? take
                ) =>
                    marketplace.SearchPackagesAsync(
                        new MarketplaceSearchRequest
                        {
                            Query = q,
                            Category = category,
                            SortBy = sort ?? MarketplaceSortOption.Relevance,
                            Skip = skip ?? 0,
                            Take = take ?? 20,
                        }
                    )
            )
            .AllowAnonymous();
}
