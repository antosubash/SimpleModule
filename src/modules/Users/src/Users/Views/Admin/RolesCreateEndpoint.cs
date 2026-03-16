using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Users.Views.Admin;

public class RolesCreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/roles/create", () => Inertia.Render("Users/Admin/RolesCreate"))
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
