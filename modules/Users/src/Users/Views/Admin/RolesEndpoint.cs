using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Views.Admin;

public class RolesEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/roles",
                async (
                    RoleManager<ApplicationRole> roleManager,
                    UserManager<ApplicationUser> userManager
                ) =>
                {
                    var roles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

                    var roleList = new List<object>();
                    foreach (var role in roles)
                    {
                        var usersInRole = role.Name is not null
                            ? await userManager.GetUsersInRoleAsync(role.Name)
                            : [];
                        roleList.Add(
                            new
                            {
                                id = role.Id,
                                name = role.Name,
                                description = role.Description,
                                userCount = usersInRole.Count,
                                createdAt = role.CreatedAt.ToString("O"),
                            }
                        );
                    }

                    return Inertia.Render("Users/Admin/Roles", new { roles = roleList });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
