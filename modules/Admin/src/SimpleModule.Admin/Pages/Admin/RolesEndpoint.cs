using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Pages.Admin;

public class RolesEndpoint : IViewEndpoint
{
    public const string Route = AdminConstants.Routes.Roles;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
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
