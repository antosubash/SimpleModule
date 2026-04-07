using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Admin.Pages.Admin;

public class HubEndpoint : IViewEndpoint
{
    public const string Route = AdminConstants.Routes.Hub;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, () => Inertia.Render("Admin/Admin/Hub"))
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
