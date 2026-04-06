using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class GetAllEndpoint : IEndpoint
{
    public const string Route = TenantsConstants.Routes.Api.GetAll;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (ITenantContracts contracts) => CrudEndpoints.GetAll(contracts.GetAllTenantsAsync)
            )
            .RequirePermission(TenantsPermissions.View);
}
