using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class RemoveHostEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}/hosts/{hostId}",
                async (TenantId id, TenantHostId hostId, ITenantContracts contracts) =>
                {
                    await contracts.RemoveHostAsync(id, hostId);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(TenantsPermissions.ManageHosts);
}
