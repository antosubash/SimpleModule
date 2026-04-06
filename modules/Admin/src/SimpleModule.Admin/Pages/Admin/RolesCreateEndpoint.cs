using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Admin.Pages.Admin;

public class RolesCreateEndpoint : IViewEndpoint
{
    public const string Route = AdminConstants.Routes.RolesCreate;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                (PermissionRegistry permissionRegistry) =>
                {
                    var permissionsByModule = permissionRegistry.ByModule.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToList()
                    );

                    return Inertia.Render("Admin/Admin/RolesCreate", new { permissionsByModule });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
