using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Pages;

public class EditEndpoint : IViewEndpoint
{
    public const string Route = TenantsConstants.Routes.Views.Edit;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (TenantId id, ITenantContracts contracts) =>
                {
                    var tenant = await contracts.GetTenantByIdAsync(id);
                    if (tenant is null)
                    {
                        return Results.NotFound();
                    }

                    return Inertia.Render("Tenants/Edit", new { tenant });
                }
            )
            .RequirePermission(TenantsPermissions.Update);
    }
}
