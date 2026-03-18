using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Admin.Views.Admin;

public class RolesCreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/roles/create",
                (PermissionRegistry permissionRegistry) =>
                {
                    var permissionsByModule = permissionRegistry.ByModule
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.ToList()
                        );

                    return Inertia.Render("Admin/Admin/RolesCreate", new { permissionsByModule });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
