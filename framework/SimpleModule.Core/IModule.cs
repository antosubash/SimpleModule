using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core.Agents;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.FeatureFlags;
using SimpleModule.Core.Menu;
using SimpleModule.Core.RateLimiting;
using SimpleModule.Core.Settings;

namespace SimpleModule.Core;

public interface IModule
{
    virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
    virtual void ConfigureMiddleware(IApplicationBuilder app) { }
    virtual void ConfigureMenu(IMenuBuilder menus) { }
    virtual void ConfigurePermissions(PermissionRegistryBuilder builder) { }
    virtual void ConfigureSettings(ISettingsBuilder settings) { }
    virtual void ConfigureFeatureFlags(IFeatureFlagBuilder builder) { }
    virtual void ConfigureAgents(IAgentBuilder builder) { }
    virtual void ConfigureRateLimits(IRateLimitBuilder builder) { }

    /// <summary>
    /// Called once during application startup after all services are built but before
    /// hosted services start. Receives the host instance for configuring host-level
    /// integrations (e.g., TickerQ's UseTickerQ, database initialization).
    /// </summary>
    virtual void ConfigureHost(IHost host) { }

    /// <summary>
    /// Called once during application startup after all services are registered.
    /// Use for one-time initialization such as loading certificates, warming caches, or verifying external dependencies.
    /// </summary>
    virtual Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called during graceful application shutdown.
    /// Use for cleanup such as flushing buffers, closing connections, or draining background work.
    /// </summary>
    virtual Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Returns the current health status of this module.
    /// Called by the module health check endpoint to report per-module health.
    /// </summary>
    virtual Task<ModuleHealthStatus> CheckHealthAsync(CancellationToken cancellationToken) =>
        Task.FromResult(ModuleHealthStatus.Healthy);
}
