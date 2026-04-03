using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.RateLimiting;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public class GetActivePoliciesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/active",
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
