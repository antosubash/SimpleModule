using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;

namespace SimpleModule.Dashboard;

[Module(DashboardConstants.ModuleName, RoutePrefix = DashboardConstants.RoutePrefix)]
public class DashboardModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }

    public void ConfigureMenu(IMenuBuilder menus) { }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/",
            (HttpContext context) =>
            {
                var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
                var displayName = context.User?.Identity?.Name ?? "User";
                var isDevelopment =
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

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
        );
    }
}
