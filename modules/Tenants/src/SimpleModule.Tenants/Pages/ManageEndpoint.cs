using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Views;

[ViewPage("Tenants/Manage")]
public class ManageEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/manage",
                async (ITenantContracts contracts) =>
                    Inertia.Render(
                        "Tenants/Manage",
                        new { tenants = await contracts.GetAllTenantsAsync() }
                    )
            )
            .RequirePermission(TenantsPermissions.View);
    }
}
