using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}",
                (TenantId id, ITenantContracts contracts) =>
                    CrudEndpoints.GetById(() => contracts.GetTenantByIdAsync(id))
            )
            .RequirePermission(TenantsPermissions.View);
}
