using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Admin.Pages.Admin;

public class HubEndpoint : IViewEndpoint
{
    public const string Route = AdminConstants.Routes.Hub;

    // Tile URLs whose presence we probe before rendering. Anything not in the
    // app's endpoint table is filtered out so we don't surface 404 tiles when
    // the corresponding peer module isn't installed.
    private static readonly string[] CandidateUrls =
    [
        "/admin/users",
        "/admin/roles",
        "/openiddict/clients",
        "/tenants/manage",
        "/pages/manage",
        "/email/templates",
        "/email/history",
        "/settings/menus",
        "/feature-flags/manage",
        "/rate-limiting/manage",
        "/admin/jobs",
        "/audit-logs/browse",
        "/settings/manage",
    ];

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                (EndpointDataSource endpointDataSource) =>
                {
                    var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var endpoint in endpointDataSource.Endpoints)
                    {
                        if (endpoint is RouteEndpoint route)
                        {
                            registered.Add("/" + route.RoutePattern.RawText?.TrimStart('/'));
                        }
                    }

                    var availableUrls = CandidateUrls.Where(registered.Contains).ToArray();
                    return Inertia.Render("Admin/Admin/Hub", new { availableUrls });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
