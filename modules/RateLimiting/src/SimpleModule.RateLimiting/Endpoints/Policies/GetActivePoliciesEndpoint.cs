using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.RateLimiting;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public class GetActivePoliciesEndpoint : IEndpoint
{
    public const string Route = RateLimitingConstants.Routes.GetActivePolicies;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (IRateLimitPolicyRegistry registry) =>
                    TypedResults.Ok(
                        registry
                            .GetPolicies()
                            .Select(p => new
                            {
                                p.Name,
                                PolicyType = p.PolicyType.ToString(),
                                Target = p.Target.ToString(),
                                p.PermitLimit,
                                WindowSeconds = (int)p.Window.TotalSeconds,
                                p.SegmentsPerWindow,
                                p.TokenLimit,
                                p.TokensPerPeriod,
                                ReplenishmentPeriodSeconds = (int)
                                    p.ReplenishmentPeriod.TotalSeconds,
                                p.QueueLimit,
                            })
                    )
            )
            .RequirePermission(RateLimitingPermissions.View);
}
