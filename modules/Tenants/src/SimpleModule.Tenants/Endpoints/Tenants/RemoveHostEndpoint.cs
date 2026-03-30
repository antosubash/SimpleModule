using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class RemoveHostEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}/hosts/{hostId}",
                (TenantId id, TenantHostId hostId, ITenantContracts contracts) =>
                    CrudEndpoints.Delete(() => contracts.RemoveHostAsync(id, hostId))
            )
            .RequirePermission(TenantsPermissions.ManageHosts);
}
