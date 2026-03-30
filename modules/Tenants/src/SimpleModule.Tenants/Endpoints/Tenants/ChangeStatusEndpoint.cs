using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class ChangeStatusEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                "/{id}/status",
                (TenantId id, ChangeStatusRequest request, ITenantContracts contracts) =>
                    CrudEndpoints.Update(() => contracts.ChangeStatusAsync(id, request.Status))
            )
            .RequirePermission(TenantsPermissions.ChangeStatus);
}

public class ChangeStatusRequest
{
    public TenantStatus Status { get; set; }
}
