using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Dashboard.Pages;

public class HomeEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/",
                (ClaimsPrincipal principal, IHostEnvironment env) =>
                {
                    var isAuthenticated = principal.Identity?.IsAuthenticated == true;
                    var displayName = principal.Identity?.Name ?? "User";
                    var isDevelopment = env.IsDevelopment();

                    return Inertia.Render(
                        "Dashboard/Home",
                        new
                        {
                            isAuthenticated,
                            displayName,
                            isDevelopment,
                        }
                    );
                }
            )
            .ExcludeFromDescription()
            .AllowAnonymous();
    }
}
