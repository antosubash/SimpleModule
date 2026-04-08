using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Entities;
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

    // Menu items removed — accessible via Admin hub page

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        app.UseMiddleware<Middleware.TenantResolutionMiddleware>();
    }
}
