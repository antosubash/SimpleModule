using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Pages;

public class ManageEndpoint : IViewEndpoint
{
    public const string Route = TenantsConstants.Routes.Views.Manage;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (ITenantContracts contracts) =>
                    Inertia.Render(
                        "Tenants/Manage",
                        new { tenants = await contracts.GetAllTenantsAsync() }
                    )
            )
            .RequirePermission(TenantsPermissions.View);
    }
}
