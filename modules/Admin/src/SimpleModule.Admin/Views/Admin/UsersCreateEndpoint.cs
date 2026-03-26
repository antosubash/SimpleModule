using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Views.Admin;

public class UsersCreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/users/create",
                async (RoleManager<ApplicationRole> roleManager) =>
                {
                    var allRoles = await roleManager
                        .Roles.OrderBy(r => r.Name)
                        .Select(r => new
                        {
                            id = r.Id,
                            name = r.Name,
                            description = r.Description,
                        })
                        .ToListAsync();

                    return Inertia.Render("Admin/Admin/UsersCreate", new { allRoles });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
