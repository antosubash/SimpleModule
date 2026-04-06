using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class RemoveHostEndpoint : IEndpoint
{
    public const string Route = TenantsConstants.Routes.Api.RemoveHost;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                (TenantId id, TenantHostId hostId, ITenantContracts contracts) =>
                    CrudEndpoints.Delete(() => contracts.RemoveHostAsync(id, hostId))
            )
            .RequirePermission(TenantsPermissions.ManageHosts);
}
