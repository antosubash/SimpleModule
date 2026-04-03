using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.RateLimiting;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Views;

[ViewPage("RateLimiting/Admin")]
public class AdminEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/",
                async (IRateLimitingContracts contracts, IRateLimitPolicyRegistry policyRegistry) =>
                {
                    var rules = await contracts.GetAllRulesAsync();
                    var activePolicies = policyRegistry.GetPolicies();
                    return Inertia.Render(
                        "RateLimiting/Admin",
                        new
                        {
                            rules,
                            activePolicies = activePolicies.Select(p => new
                            {
                                p.Name,
                                PolicyType = p.PolicyType.ToString(),
                                Target = p.Target.ToString(),
                                p.PermitLimit,
                                WindowSeconds = (int)p.Window.TotalSeconds,
                                p.TokenLimit,
                                p.TokensPerPeriod,
                            }),
                        }
                    );
                }
            )
            .RequirePermission(RateLimitingPermissions.View);
    }
}
