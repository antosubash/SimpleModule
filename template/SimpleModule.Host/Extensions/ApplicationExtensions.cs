using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Host.Components;
using SimpleModule.OpenIddict.Contracts;

namespace SimpleModule.Host;

public static class ApplicationExtensions
{
    private const int ImmutableAssetsCacheDurationSeconds = 31536000; // 365 days
    private const string VendorJsPathPrefix = "/js/vendor/";
    private const string ModuleContentPathPrefix = "/_content/";
    private const string ModuleScriptExtension = ".mjs";

    /// <summary>
    /// Applies database migrations for non-production environments.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HostDbContext>();
            await db.Database.MigrateAsync();
        }
    }

    /// <summary>
    /// Registers cache headers for immutable assets: vendor JS, hashed .mjs chunks, and versioned resources.
    /// </summary>
    public static WebApplication UseStaticFileCaching(this WebApplication app)
    {
        app.Use(
            async (context, next) =>
            {
                var path = context.Request.Path.Value;

                // Guard: empty path doesn't need caching headers
                if (string.IsNullOrEmpty(path))
                {
                    await next();
                    return;
                }

                bool hasVersionParam = context.Request.Query.ContainsKey("v");
                bool isVendorJs = path.StartsWith(VendorJsPathPrefix, StringComparison.OrdinalIgnoreCase);
                bool isHashedChunk = path.StartsWith(ModuleContentPathPrefix, StringComparison.OrdinalIgnoreCase)
                    && path.EndsWith(ModuleScriptExtension, StringComparison.OrdinalIgnoreCase);

                if (hasVersionParam || isVendorJs || isHashedChunk)
                {
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers.CacheControl =
                            $"public, max-age={ImmutableAssetsCacheDurationSeconds}, immutable";
                        return Task.CompletedTask;
                    });
                }

                await next();
            }
        );

        return app;
    }

    /// <summary>
    /// Rewrites GET / to the public menu's home page URL (internal rewrite, URL stays /).
    /// Requires IPublicMenuProvider to be registered.
    /// </summary>
    public static WebApplication UseHomePageRewrite(this WebApplication app)
    {
        app.Use(
            async (context, next) =>
            {
                if (context.Request.Path == "/" && HttpMethods.IsGet(context.Request.Method))
                {
                    var menuProvider = context.RequestServices.GetService<IPublicMenuProvider>();
                    if (menuProvider is not null)
                    {
                        var homeUrl = await menuProvider.GetHomePageUrlAsync();
                        if (homeUrl is not null && homeUrl != "/")
                        {
                            context.Request.Path = homeUrl;
                        }
                    }
                }

                await next();
            }
        );

        return app;
    }

    /// <summary>
    /// Maps health check endpoints: liveness (/health/live) and readiness (/health/ready).
    /// </summary>
    public static WebApplication MapModuleHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(
                RouteConstants.HealthLive,
                new HealthCheckOptions
                {
                    Predicate = _ => false, // No checks — just confirms the process is running
                }
            )
            .AllowAnonymous();

        app.MapHealthChecks(
                RouteConstants.HealthReady,
                new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains(HealthCheckConstants.ReadyTag),
                }
            )
            .AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Maps Razor components and discovers component assemblies from modules.
    /// </summary>
    public static WebApplication MapModuleComponents(this WebApplication app)
    {
        app.MapRazorComponents<App>().AddModuleAssemblies();
        return app;
    }
}
