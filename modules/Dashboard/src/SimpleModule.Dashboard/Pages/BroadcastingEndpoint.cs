using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Broadcasting;
using SimpleModule.Core.Extensions;
using SimpleModule.Core.Inertia;
using SimpleModule.Dashboard.Contracts;

namespace SimpleModule.Dashboard.Pages;

/// <summary>
/// Live demo of the broadcasting framework. Renders a page that subscribes
/// to the current user's private channel and counts ticks fired from the
/// companion POST endpoint. Visible to developers as a smoke test that the
/// hub + Echo client are wired end-to-end.
/// </summary>
public class BroadcastingEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                DashboardConstants.Routes.Views.Broadcasting,
                (ClaimsPrincipal principal) =>
                {
                    var userId = principal.GetUserId();
                    var channel = userId is null ? null : BroadcastChannels.ForUser(userId);
                    return Inertia.Render(
                        "Dashboard/Broadcasting",
                        new
                        {
                            channel,
                            userId,
                            fireUrl = DashboardConstants.Routes.Api.FireBroadcastTick,
                        }
                    );
                }
            )
            .ExcludeFromDescription();

        app.MapPost(
                DashboardConstants.Routes.Api.FireBroadcastTick,
                async (
                    ClaimsPrincipal principal,
                    IBroadcaster broadcaster,
                    CancellationToken cancellationToken
                ) =>
                {
                    var userId = principal.GetUserId();
                    if (userId is null)
                    {
                        return Results.Unauthorized();
                    }

                    await broadcaster.ToUserAsync(
                        userId,
                        "demo.tick",
                        new { at = DateTimeOffset.UtcNow },
                        cancellationToken
                    );
                    return Results.NoContent();
                }
            )
            // The demo page POSTs without a CSRF header. Disabling the
            // antiforgery requirement is acceptable because the route fires
            // only at the authenticated principal's own broadcast channel —
            // it's a no-side-effect smoke test, not a state mutation.
            .DisableAntiforgery()
            .ExcludeFromDescription();
    }
}
