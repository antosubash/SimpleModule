using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Endpoints.Policies;

public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id:int}",
                (int id, IRateLimitingContracts contracts) =>
                    CrudEndpoints.Delete(() => contracts.DeleteRuleAsync(RateLimitRuleId.From(id)))
            )
            .RequirePermission(RateLimitingPermissions.Delete);
}
