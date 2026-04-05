using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Endpoints;
using SimpleModule.Marketplace.Contracts;
using MarketplaceConstants = SimpleModule.Marketplace.Contracts.MarketplaceConstants;

namespace SimpleModule.Marketplace.Endpoints.Marketplace;

public class GetByIdEndpoint : IEndpoint
{
    public const string Route = MarketplaceConstants.Routes.GetById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (IMarketplaceContracts marketplace, string id) =>
                    CrudEndpoints.GetById(() => marketplace.GetPackageDetailsAsync(id))
            )
            .AllowAnonymous();
}
