using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Views.Admin;

public class RolesEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/roles",
                async (
                    RoleManager<ApplicationRole> roleManager,
                    UserManager<ApplicationUser> userManager,
                    IPermissionContracts permissionContracts
                ) =>
                {
                    var roles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

                    var roleList = new List<object>();
                    foreach (var role in roles)
                    {
                        var usersInRole = await userManager.GetUsersInRoleAsync(role.Name!);
                        var rolePermissions = await permissionContracts.GetPermissionsForRoleAsync(
                            RoleId.From(role.Id)
                        );
                        var permissionCount = rolePermissions.Count;

                        roleList.Add(
                            new
                            {
                                id = role.Id,
                                name = role.Name,
                                description = role.Description,
                                userCount = usersInRole.Count,
                                permissionCount,
                                createdAt = role.CreatedAt.ToString("O"),
                            }
                        );
                    }

                    return Inertia.Render("Admin/Admin/Roles", new { roles = roleList });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
