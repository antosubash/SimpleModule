using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Menu;

namespace SimpleModule.Core.Inertia;

public static class AdminSidebarMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminSidebarSharedData(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var sharedData = context.RequestServices.GetService<InertiaSharedData>();
            if (sharedData is not null && context.User.Identity?.IsAuthenticated == true)
            {
                var menuRegistry = context.RequestServices.GetRequiredService<IMenuRegistry>();
                var items = menuRegistry.GetItems(MenuSection.AdminSidebar);
                sharedData.Set("adminSidebarMenu", items.Select(i => new
                {
                    label = i.Label,
                    url = i.Url,
                    icon = i.Icon,
                    order = i.Order,
                }).ToList());
            }
            await next();
        });
    }
}
