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
                (IRateLimitingContracts contracts, int? skip, int? take) =>
                    CrudEndpoints.GetAll(() =>
                        contracts.GetAllRulesAsync(
                            Math.Max(0, skip ?? 0),
                            Math.Clamp(take ?? 30, 1, 500)
                        )
                    )
            )
            .RequirePermission(RateLimitingPermissions.View);
}
