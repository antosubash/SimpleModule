using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public class ChangeStatusEndpoint : IEndpoint
{
    public const string Route = TenantsConstants.Routes.Api.ChangeStatus;
    public const string Method = "PUT";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut(
                Route,
                (TenantId id, ChangeStatusRequest request, ITenantContracts contracts) =>
                    CrudEndpoints.Update(() => contracts.ChangeStatusAsync(id, request.Status))
            )
            .RequirePermission(TenantsPermissions.ChangeStatus);
}

public class ChangeStatusRequest
{
    public TenantStatus Status { get; set; }
}
