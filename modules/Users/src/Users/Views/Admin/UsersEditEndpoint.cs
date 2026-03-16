using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Views.Admin;

public class UsersEditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/users/{id}/edit",
                async (
                    string id,
                    UserManager<ApplicationUser> userManager,
                    RoleManager<ApplicationRole> roleManager
                ) =>
                {
                    var user = await userManager.FindByIdAsync(id);
                    if (user is null)
                        return Results.NotFound();

                    var userRoles = await userManager.GetRolesAsync(user);
                    var allRoles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

                    return Inertia.Render(
                        "Users/Admin/UsersEdit",
                        new
                        {
                            user = new
                            {
                                id = user.Id,
                                displayName = user.DisplayName,
                                email = user.Email,
                                emailConfirmed = user.EmailConfirmed,
                                isLockedOut = user.LockoutEnd.HasValue
                                    && user.LockoutEnd > DateTimeOffset.UtcNow,
                                createdAt = user.CreatedAt.ToString("O"),
                                lastLoginAt = user.LastLoginAt?.ToString("O"),
                            },
                            userRoles = userRoles.ToList(),
                            allRoles = allRoles
                                .Select(r => new
                                {
                                    id = r.Id,
                                    name = r.Name,
                                    description = r.Description,
                                })
                                .ToList(),
                        }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
