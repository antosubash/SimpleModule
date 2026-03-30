using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.FeatureFlags;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags;

[Module(
    FeatureFlagsConstants.ModuleName,
    RoutePrefix = FeatureFlagsConstants.RoutePrefix,
    ViewPrefix = "/feature-flags"
)]
public class FeatureFlagsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddModuleDbContext<FeatureFlagsDbContext>(
            configuration,
            FeatureFlagsConstants.ModuleName
        );
        services.AddScoped<FeatureFlagService>();
        services.AddScoped<IFeatureFlagContracts>(sp =>
            sp.GetRequiredService<FeatureFlagService>()
        );
        services.AddScoped<IFeatureFlagService>(sp =>
            sp.GetRequiredService<FeatureFlagService>()
        );
        services.AddHostedService<FeatureFlagSyncService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Feature Flags",
                Url = "/feature-flags",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M3 3v1.5M3 21v-6m0 0 2.77-.693a9 9 0 0 1 6.208.682l.108.054a9 9 0 0 0 6.086.71l3.114-.732a48.524 48.524 0 0 1-.005-10.499l-3.11.732a9 9 0 0 1-6.085-.711l-.108-.054a9 9 0 0 0-6.208-.682L3 4.5M3 15V4.5"/></svg>""",
                Order = 60,
                Section = MenuSection.AdminSidebar,
            }
        );
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        app.UseMiddleware<Middleware.FeatureFlagMiddleware>();
    }

}
