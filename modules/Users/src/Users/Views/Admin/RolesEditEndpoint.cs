using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Views.Admin;

public class RolesEditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/roles/{id}/edit",
                async (
                    string id,
                    RoleManager<ApplicationRole> roleManager,
                    UserManager<ApplicationUser> userManager
                ) =>
                {
                    var role = await roleManager.FindByIdAsync(id);
                    if (role is null)
                        return Results.NotFound();

                    var usersInRole = role.Name is not null
                        ? await userManager.GetUsersInRoleAsync(role.Name)
                        : [];

                    return Inertia.Render(
                        "Users/Admin/RolesEdit",
                        new
                        {
                            role = new
                            {
                                id = role.Id,
                                name = role.Name,
                                description = role.Description,
                                createdAt = role.CreatedAt.ToString("O"),
                            },
                            users = usersInRole
                                .Select(u => new
                                {
                                    id = u.Id,
                                    displayName = u.DisplayName,
                                    email = u.Email,
                                })
                                .ToList(),
                        }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
