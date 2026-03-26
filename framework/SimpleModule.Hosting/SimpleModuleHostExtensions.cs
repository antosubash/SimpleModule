using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Blazor;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.Database.Health;
using SimpleModule.DevTools;

namespace SimpleModule.Hosting;

public static class SimpleModuleHostExtensions
{
    private const int ImmutableAssetsCacheDurationSeconds = 31536000; // 365 days
    private const string VendorJsPathPrefix = "/js/vendor/";
    private const string ModuleContentPathPrefix = "/_content/";
    private const string ModuleScriptExtension = ".mjs";

    /// <summary>
    /// Registers all non-generated SimpleModule infrastructure services.
    /// Called by the source-generated <c>AddSimpleModule()</c> method.
    /// </summary>
    public static WebApplicationBuilder AddSimpleModuleInfrastructure(
        this WebApplicationBuilder builder,
        Action<SimpleModuleOptions>? configure = null
    )
    {
        var options = new SimpleModuleOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(options);

        BridgeAspireConnectionString(builder.Configuration);
        ValidateDatabaseConfiguration(builder.Configuration);

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        if (options.EnableSwagger)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }

        builder.Services.AddRazorComponents();

        if (options.ShellComponent is not null)
        {
            builder.Services.AddSimpleModuleBlazor(o => o.ShellComponent = options.ShellComponent);
        }
        else
        {
            builder.Services.AddSimpleModuleBlazor();
        }

        builder.Services.AddScoped<IEventBus, EventBus>();
        builder.Services.AddScoped<InertiaSharedData>();

        // Authentication is configured by modules via their ConfigureServices
        // (e.g., OpenIddict registers SmartAuth policy scheme).
        // Register a baseline so the middleware pipeline works even without an auth module.
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();

        // Register default IPublicMenuProvider if no module provides one
        builder.Services.TryAddScoped<IPublicMenuProvider, DefaultPublicMenuProvider>();

        if (options.EnableHealthChecks)
        {
            builder
                .Services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>(
                    HealthCheckConstants.DatabaseCheckName,
                    tags: [HealthCheckConstants.ReadyTag]
                );
        }

        if (options.EnableDevTools && builder.Environment.IsDevelopment())
        {
            builder.Services.AddDevTools();
        }

        return builder;
    }

    /// <summary>
    /// Configures all non-generated SimpleModule middleware.
    /// Called by the source-generated <c>UseSimpleModule()</c> method.
    /// </summary>
    public static async Task UseSimpleModuleInfrastructure(this WebApplication app)
    {
        // Database initialization (non-production only)
        if (!app.Environment.IsProduction())
        {
            using var scope = app.Services.CreateScope();
            var infos = scope.ServiceProvider.GetServices<ModuleDbContextInfo>();

            foreach (var info in infos)
            {
                if (info.ModuleName != DatabaseConstants.HostModuleName)
                    continue;

                if (scope.ServiceProvider.GetService(info.DbContextType) is DbContext db)
                {
                    // Use MigrateAsync when migrations exist, EnsureCreated otherwise
                    // (projects scaffolded with sm new project have no migrations initially)
                    if ((await db.Database.GetPendingMigrationsAsync()).Any()
                        || (await db.Database.GetAppliedMigrationsAsync()).Any())
                    {
                        await db.Database.MigrateAsync();
                    }
                    else
                    {
                        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()
                            ?.CreateLogger("SimpleModule.Hosting");
                        logger?.LogInformation(
                            "No migrations found — using EnsureCreated to initialize database. " +
                            "Add migrations for production use.");
                        await db.Database.EnsureCreatedAsync();
                    }
                }
            }
        }

        app.UseExceptionHandler();

        var options = app.Services.GetRequiredService<SimpleModuleOptions>();
        if (options.EnableSwagger && app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseInertia();
        UseStaticFileCaching(app);
        app.MapStaticAssets();

        // Fallback for dynamically generated .mjs chunks (Vite watch rebuilds)
        // MapStaticAssets only knows about files present at build time;
        // Vite generates new hash-named chunks at runtime that need correct MIME types
        if (app.Environment.IsDevelopment())
        {
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".mjs"] = "application/javascript";
            app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        // Module middleware is added by the source-generated UseSimpleModule()
        // via IModule.ConfigureMiddleware() calls.

        UseHomePageRewrite(app);
        app.UseAntiforgery();

        if (options.EnableHealthChecks)
        {
            app.MapHealthChecks(
                    RouteConstants.HealthLive,
                    new HealthCheckOptions { Predicate = _ => false }
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
        }
    }

    private static void BridgeAspireConnectionString(ConfigurationManager configuration)
    {
        var aspireConnectionString = configuration.GetConnectionString("simplemoduledb");
        if (!string.IsNullOrEmpty(aspireConnectionString))
        {
            configuration["Database:DefaultConnection"] = aspireConnectionString;
        }
    }

    private static void ValidateDatabaseConfiguration(ConfigurationManager configuration)
    {
        var dbOptions =
            configuration.GetSection(DatabaseConstants.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();
        var connString = dbOptions.DefaultConnection;

        if (string.IsNullOrEmpty(connString))
        {
            throw new InvalidOperationException(
                "Database configuration is missing. "
                    + "Ensure 'Database:DefaultConnection' is configured in appsettings.json."
            );
        }

        _ = DatabaseProviderDetector.Detect(connString, dbOptions.Provider);
    }

    private static void UseStaticFileCaching(WebApplication app)
    {
        app.Use(
            async (context, next) =>
            {
                var path = context.Request.Path.Value;

                if (string.IsNullOrEmpty(path))
                {
                    await next();
                    return;
                }

                bool hasVersionParam = context.Request.Query.ContainsKey("v");
                bool isVendorJs = path.StartsWith(
                    VendorJsPathPrefix,
                    StringComparison.OrdinalIgnoreCase
                );
                bool isHashedChunk =
                    path.StartsWith(ModuleContentPathPrefix, StringComparison.OrdinalIgnoreCase)
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
    }

    private static void UseHomePageRewrite(WebApplication app)
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
    }
}
