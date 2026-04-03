using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id:int}",
                (int id, IRateLimitingContracts contracts) =>
                    CrudEndpoints.GetById(() =>
                        contracts.GetRuleByIdAsync(RateLimitRuleId.From(id))
                    )
            )
            .RequirePermission(RateLimitingPermissions.View);
}
