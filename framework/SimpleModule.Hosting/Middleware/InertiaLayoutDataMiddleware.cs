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

            // Menu items (filtered by user roles)
            var menuRegistry = context.RequestServices.GetService<IMenuRegistry>();
            if (menuRegistry is not null)
            {
                IReadOnlyList<MenuItem> Filter(MenuSection section)
                {
                    var items = menuRegistry.GetItems(section);
                    if (!isAuthenticated)
                    {
                        return items.Where(m => !m.RequiresAuth).ToList();
                    }

                    return items
                        .Where(m => m.Roles.Count == 0 || m.Roles.Any(r => user.IsInRole(r)))
                        .ToList();
                }

                sharedData.Set(
                    "menus",
                    new
                    {
                        sidebar = Filter(MenuSection.AppSidebar),
                        adminSidebar = Filter(MenuSection.AdminSidebar),
                        userDropdown = Filter(MenuSection.UserDropdown),
                        navbar = Filter(MenuSection.Navbar),
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
