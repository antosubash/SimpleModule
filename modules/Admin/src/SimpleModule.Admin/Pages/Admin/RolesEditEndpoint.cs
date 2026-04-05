using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Pages.Admin;

public class RolesEditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/roles/{id}/edit",
                async (
                    string id,
                    IRoleAdminContracts roleAdmin,
                    IPermissionContracts permissionContracts,
                    PermissionRegistry permissionRegistry,
                    string? tab
                ) =>
                {
                    var role = await roleAdmin.GetRoleByIdAsync(id);
                    if (role is null)
                        return TypedResults.NotFound();

                    var rolePermissions = (
                        await permissionContracts.GetPermissionsForRoleAsync(RoleId.From(id))
                    ).ToList();

                    var permissionsByModule = permissionRegistry.ByModule.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToList()
                    );

                    return Inertia.Render(
                        "Admin/Admin/RolesEdit",
                        new
                        {
                            role,
                            users = Array.Empty<object>(),
                            rolePermissions,
                            permissionsByModule,
                            tab = tab ?? "details",
                        }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
