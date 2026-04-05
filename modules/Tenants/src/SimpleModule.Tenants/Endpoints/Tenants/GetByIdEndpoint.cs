using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class GetByIdEndpoint : IEndpoint
{
    public const string Route = TenantsConstants.Routes.Api.GetById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (TenantId id, ITenantContracts contracts) =>
                    CrudEndpoints.GetById(() => contracts.GetTenantByIdAsync(id))
            )
            .RequirePermission(TenantsPermissions.View);
}
