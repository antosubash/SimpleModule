using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Entities;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants;

[Module(
    TenantsConstants.ModuleName,
    RoutePrefix = TenantsConstants.RoutePrefix,
    ViewPrefix = "/tenants"
)]
public class TenantsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddModuleDbContext<TenantsDbContext>(configuration, TenantsConstants.ModuleName);
        services.AddScoped<ITenantContracts, TenantService>();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<Resolvers.HostNameTenantResolver>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Tenants",
                Url = "/tenants/manage",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M3.75 21h16.5M4.5 3h15M5.25 3v18m13.5-18v18M9 6.75h1.5m-1.5 3h1.5m-1.5 3h1.5m3-6H15m-1.5 3H15m-1.5 3H15M9 21v-3.375c0-.621.504-1.125 1.125-1.125h3.75c.621 0 1.125.504 1.125 1.125V21"/></svg>""",
                Order = 50,
                Section = MenuSection.AdminSidebar,
            }
        );
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        app.UseMiddleware<Middleware.TenantResolutionMiddleware>();
    }
}
