using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Dashboard.Views;

public class HomeEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/",
                (HttpContext context, IHostEnvironment env) =>
                {
                    var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
                    var displayName = context.User?.Identity?.Name ?? "User";
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
