using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Pages;

public class CreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/create", () => Inertia.Render("Tenants/Create"))
            .RequirePermission(TenantsPermissions.Create);
    }
}
