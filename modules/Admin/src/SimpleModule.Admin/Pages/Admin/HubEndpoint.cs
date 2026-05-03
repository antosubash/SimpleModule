using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Admin.Pages.Admin;

public class HubEndpoint : IViewEndpoint
{
    public const string Route = AdminConstants.Routes.Hub;

    // Must stay in sync with the `url` values in Hub.tsx's `groups`. URLs not
    // present in the app's endpoint table are filtered out so the hub doesn't
    // surface 404 tiles for peer modules that aren't installed.
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

    private string[]? _availableUrls;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                (EndpointDataSource endpointDataSource) =>
                {
                    _availableUrls ??= ComputeAvailableUrls(endpointDataSource);
                    return Inertia.Render(
                        "Admin/Admin/Hub",
                        new { availableUrls = _availableUrls }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole(WellKnownRoles.Admin));
    }

    private static string[] ComputeAvailableUrls(EndpointDataSource endpointDataSource)
    {
        var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var endpoint in endpointDataSource.Endpoints)
        {
            if (endpoint is RouteEndpoint route)
            {
                registered.Add("/" + route.RoutePattern.RawText?.TrimStart('/'));
            }
        }
        return CandidateUrls.Where(registered.Contains).ToArray();
    }
}
