using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;

namespace SimpleModule.Hosting.Middleware;

public sealed class InertiaLayoutDataMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sharedData = context.RequestServices.GetService<InertiaSharedData>();
        if (sharedData is not null)
        {
            var user = context.User;
            var isAuthenticated = user.Identity?.IsAuthenticated == true;

            // Auth state
            sharedData.Set(
                "auth",
                new
                {
                    isAuthenticated,
                    userName = user.Identity?.Name,
                    roles = isAuthenticated
                        ? user
                            .Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                            .Select(c => c.Value)
                            .ToArray()
                        : Array.Empty<string>(),
                }
            );

            // Menu items
            var menuRegistry = context.RequestServices.GetService<IMenuRegistry>();
            if (menuRegistry is not null)
            {
                sharedData.Set(
                    "menus",
                    new
                    {
                        sidebar = menuRegistry.GetItems(MenuSection.AppSidebar),
                        adminSidebar = menuRegistry.GetItems(MenuSection.AdminSidebar),
                        userDropdown = menuRegistry.GetItems(MenuSection.UserDropdown),
                        navbar = menuRegistry.GetItems(MenuSection.Navbar),
                    }
                );
            }

            // Public menu (for unauthenticated layout)
            var publicMenuProvider = context.RequestServices.GetService<IPublicMenuProvider>();
            if (publicMenuProvider is not null)
            {
                sharedData.Set("publicMenu", await publicMenuProvider.GetMenuTreeAsync());
            }

            // CSRF token
            var antiforgery = context.RequestServices.GetService<IAntiforgery>();
            if (antiforgery is not null)
            {
                var tokens = antiforgery.GetAndStoreTokens(context);
                sharedData.Set("csrfToken", tokens.RequestToken);
            }
        }

        await next(context);
    }
}
