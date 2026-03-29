using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Endpoints;
using SimpleModule.Marketplace.Contracts;

namespace SimpleModule.Marketplace.Endpoints.Marketplace;

public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}",
                (IMarketplaceContracts marketplace, string id) =>
                    CrudEndpoints.GetById(() => marketplace.GetPackageDetailsAsync(id))
            )
            .AllowAnonymous();
}
