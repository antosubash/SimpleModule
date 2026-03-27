using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Views.Admin;

[ViewPage("Admin/Admin/Roles")]
public class RolesEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/roles",
                async (IRoleAdminContracts roleAdmin, IPermissionContracts permissionContracts) =>
                {
                    var roles = await roleAdmin.GetAllRolesAsync();

                    var roleList = new List<object>();
                    foreach (var role in roles)
                    {
                        var rolePermissions = await permissionContracts.GetPermissionsForRoleAsync(
                            RoleId.From(role.Id)
                        );

                        roleList.Add(
                            new
                            {
                                role.Id,
                                role.Name,
                                role.Description,
                                role.UserCount,
                                permissionCount = rolePermissions.Count,
                                role.CreatedAt,
                            }
                        );
                    }

                    return Inertia.Render("Admin/Admin/Roles", new { roles = roleList });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
