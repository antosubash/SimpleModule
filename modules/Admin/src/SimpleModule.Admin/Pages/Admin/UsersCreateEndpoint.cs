using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Pages.Admin;

public class UsersCreateEndpoint : IViewEndpoint
{
    public const string Route = AdminConstants.Routes.UsersCreate;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (IRoleAdminContracts roleAdmin) =>
                {
                    var allRoles = await roleAdmin.GetAllRolesAsync();

                    return Inertia.Render("Admin/Admin/UsersCreate", new { allRoles });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
