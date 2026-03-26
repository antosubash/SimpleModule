using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Admin.Contracts;
using SimpleModule.Admin.Services;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.Database;

namespace SimpleModule.Admin;

[Module(AdminConstants.ModuleName)]
public class AdminModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<AdminDbContext>(configuration, AdminConstants.ModuleName);
        services.AddScoped<AuditService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Users",
                Url = "/admin/users",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/></svg>""",
                Order = 10,
                Section = MenuSection.AdminSidebar,
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Roles",
                Url = "/admin/roles",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>""",
                Order = 11,
                Section = MenuSection.AdminSidebar,
            }
        );
    }
}
