using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public class GetAllEndpoint : IEndpoint
{
    public const string Route = RateLimitingConstants.Routes.GetAll;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (IRateLimitingContracts contracts) =>
                    CrudEndpoints.GetAll(contracts.GetAllRulesAsync)
            )
            .RequirePermission(RateLimitingPermissions.View);
}
