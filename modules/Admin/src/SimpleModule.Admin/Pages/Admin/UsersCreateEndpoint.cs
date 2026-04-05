using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Pages.Admin;

public class UsersCreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/users/create",
                async (IRoleAdminContracts roleAdmin) =>
                {
                    var allRoles = await roleAdmin.GetAllRolesAsync();

                    return Inertia.Render("Admin/Admin/UsersCreate", new { allRoles });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
