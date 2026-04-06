using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Pages;

public class CreateEndpoint : IViewEndpoint
{
    public const string Route = TenantsConstants.Routes.Views.Create;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, () => Inertia.Render("Tenants/Create"))
            .RequirePermission(TenantsPermissions.Create);
    }
}
