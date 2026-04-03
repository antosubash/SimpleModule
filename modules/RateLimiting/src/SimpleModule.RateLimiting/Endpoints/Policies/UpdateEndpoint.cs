using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public class UpdateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/{id:int}",
                (int id, UpdateRateLimitRuleRequest request, IRateLimitingContracts contracts) =>
                    CrudEndpoints.Update(() =>
                        contracts.UpdateRuleAsync(RateLimitRuleId.From(id), request)
                    )
            )
            .RequirePermission(RateLimitingPermissions.Update);
}
