using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}",
                (TenantId id, ITenantContracts contracts) =>
                    CrudEndpoints.Delete(() => contracts.DeleteTenantAsync(id))
            )
            .RequirePermission(TenantsPermissions.Delete);
}
