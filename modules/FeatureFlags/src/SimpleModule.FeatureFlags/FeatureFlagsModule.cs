using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.FeatureFlags;
using SimpleModule.Database;
using SimpleModule.FeatureFlags.Contracts;

namespace SimpleModule.FeatureFlags;

[Module(
    FeatureFlagsConstants.ModuleName,
    RoutePrefix = FeatureFlagsConstants.RoutePrefix,
    ViewPrefix = FeatureFlagsConstants.ViewPrefix
)]
public class FeatureFlagsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<FeatureFlagsDbContext>(
            configuration,
            FeatureFlagsConstants.ModuleName
        );
        services.AddScoped<FeatureFlagService>();
        services.AddScoped<IFeatureFlagContracts>(sp =>
            sp.GetRequiredService<FeatureFlagService>()
        );
        services.AddScoped<IFeatureFlagService>(sp => sp.GetRequiredService<FeatureFlagService>());
        services.AddHostedService<FeatureFlagSyncService>();
    }

    // Menu items removed — accessible via Admin hub page

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        app.UseMiddleware<Middleware.FeatureFlagMiddleware>();
    }
}
