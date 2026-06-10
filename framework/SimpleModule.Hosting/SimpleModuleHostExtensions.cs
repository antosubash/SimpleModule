using JasperFx.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Exceptions;
using SimpleModule.Core.Health;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Maintenance;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Modules;
using SimpleModule.Core.RateLimiting;
using SimpleModule.Core.Security;
using SimpleModule.Database;
using SimpleModule.Database.Health;
using SimpleModule.Database.Interceptors;
using SimpleModule.DevTools;
using SimpleModule.Hosting.Broadcasting;
using SimpleModule.Hosting.Inertia;
using SimpleModule.Hosting.Maintenance;
using SimpleModule.Hosting.Middleware;
using SimpleModule.Hosting.Modules;
using SimpleModule.Hosting.RateLimiting;
using Wolverine;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.Hosting;

public static partial class SimpleModuleHostExtensions
{
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

        builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));

        BridgeAspireConnectionString(builder.Configuration);
        options.DatabaseProvider = ValidateDatabaseConfiguration(builder.Configuration);

        builder.Services.Configure<ForwardedHeadersOptions>(fhOptions =>
        {
            fhOptions.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Proxy trust must be explicit. The previous behavior cleared
            // KnownProxies/KnownIPNetworks, which let any client spoof
            // X-Forwarded-For and bypass per-IP rate limiting. By default only
            // loopback is trusted (the ASP.NET Core default); deployments behind
            // a reverse proxy list it under ForwardedHeaders:KnownProxies /
            // ForwardedHeaders:KnownNetworks, or — for closed networks where the
            // proxy address is not static — opt into
            // ForwardedHeaders:TrustAllProxies.
            var section = builder.Configuration.GetSection("ForwardedHeaders");

            if (section.GetValue<bool>("TrustAllProxies"))
            {
                fhOptions.KnownIPNetworks.Clear();
                fhOptions.KnownProxies.Clear();
                return;
            }

            foreach (var proxy in ReadConfigList(section, "KnownProxies"))
            {
                if (!System.Net.IPAddress.TryParse(proxy, out var address))
                {
                    throw new InvalidOperationException(
                        $"ForwardedHeaders:KnownProxies contains '{proxy}', which is not a valid "
                            + "IP address."
                    );
                }

                fhOptions.KnownProxies.Add(address);
            }

            foreach (var network in ReadConfigList(section, "KnownNetworks"))
            {
                if (!System.Net.IPNetwork.TryParse(network, out var ipNetwork))
                {
                    throw new InvalidOperationException(
                        $"ForwardedHeaders:KnownNetworks contains '{network}', which is not a valid "
                            + "CIDR network (e.g. 10.0.0.0/8)."
                    );
                }

                fhOptions.KnownIPNetworks.Add(ipNetwork);
            }
        });

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        if (options.EnableSwagger)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }

        builder.Services.AddSingleton<IInertiaPageRenderer, HtmlFileInertiaPageRenderer>();

        // Compile-time module manifests, read from each module assembly's
        // [assembly: ModuleManifest] attribute. Resolved lazily so registration
        // order relative to the generated AddModules() does not matter.
        builder.Services.AddSingleton<IModuleManifestRegistry>(sp => new ModuleManifestRegistry(
            sp.GetServices<IModule>()
        ));

        // Unified caching abstraction (IFusionCache) shared across all modules.
        // Stampede-safe GetOrSetAsync built in; five-minute default entry duration.
        builder
            .Services.AddFusionCache()
            .WithDefaultEntryOptions(o => o.Duration = TimeSpan.FromMinutes(5));

        var dbConnectionString =
            builder.Configuration["Database:DefaultConnection"]
            ?? throw new InvalidOperationException(
                "Database:DefaultConnection must be configured for Wolverine durable messaging."
            );

        builder.Host.UseWolverine(opts =>
            WolverineConfiguration.Configure(
                opts,
                options.ModuleAssemblies,
                options.DatabaseProvider,
                dbConnectionString
            )
        );

        builder.Services.AddSimpleModuleBroadcasting();

        builder.Host.UseResourceSetupOnStartup();
        // Lazy<IMessageBus> lets services break factory-lambda cycles
        // (e.g. SettingsService ↔ AuditingMessageBus via ISettingsContracts).
        builder.Services.AddScoped(sp => new Lazy<IMessageBus>(() =>
            sp.GetRequiredService<IMessageBus>()
        ));
        builder.Services.AddScoped<InertiaSharedData>();

        // Required by EntityInterceptor to access the current HTTP context
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        builder.Services.AddScoped<ISaveChangesInterceptor, EntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, EntityChangeInterceptor>();

        // Authentication is configured by modules via their ConfigureServices
        // (e.g., OpenIddict registers SmartAuth policy scheme).
        // Register a baseline so the middleware pipeline works even without an auth module.
        builder.Services.AddAuthentication();
        // Authenticated-by-default. Endpoints that genuinely need to be public
        // (login, health probes, error/404 fallbacks) opt out with .AllowAnonymous().
        // Without a fallback policy, plain app.MapGet(...) outside a module group
        // is silently public — the wrong default for a business app.
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        builder.Services.AddAntiforgery();

        // Register default IPublicMenuProvider if no module provides one
        builder.Services.TryAddScoped<IPublicMenuProvider, DefaultPublicMenuProvider>();

        builder.Services.AddScoped<ICspNonce, CspNonce>();

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ISignedUrlGenerator, SignedUrlGenerator>();

        // Maintenance mode — file-based sentinel poll, written by `sm down` /
        // cleared by `sm up`. Resolved as singleton because it caches state
        // for a short interval.
        builder.Services.Configure<MaintenanceModeOptions>(_ => { });
        builder.Services.TryAddSingleton<
            IMaintenanceStateProvider,
            FileSystemMaintenanceStateProvider
        >();

        if (options.EnableHealthChecks)
        {
            builder
                .Services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>(
                    HealthCheckConstants.DatabaseCheckName,
                    tags: [HealthCheckConstants.ReadyTag]
                )
                .AddCheck<ModuleHealthCheck>(
                    HealthCheckConstants.ModulesCheckName,
                    tags: [HealthCheckConstants.ReadyTag]
                );
        }

        if (options.EnableDevTools && builder.Environment.IsDevelopment())
        {
            builder.Services.AddDevTools();
        }

        if (options.ValidateModuleGraph)
        {
            builder.Services.AddHostedService<ModuleGraphValidator>();
        }

        return builder;
    }

    /// <summary>
    /// Configures all non-generated SimpleModule middleware.
    /// Called by the source-generated <c>UseSimpleModule()</c> method.
    /// </summary>
    public static async Task UseSimpleModuleInfrastructure(this WebApplication app)
    {
        // Database initialization
        // SQLite (file-based) always needs auto-initialization since the DB file may not exist.
        // Managed databases (PostgreSQL, SQL Server) skip this in production — apply migrations externally.
        // SIMPLEMODULE_MIGRATE_ONLY=1 is the CLI's migration entry point (`sm add`/`sm upgrade`):
        // it forces database initialization regardless of environment, then exits without
        // serving traffic — the deterministic migration hook for installed packaged modules.
        var migrateOnly = Environment.GetEnvironmentVariable("SIMPLEMODULE_MIGRATE_ONLY") == "1";
        var smOptions = app.Services.GetRequiredService<SimpleModuleOptions>();
        if (
            migrateOnly
            || !app.Environment.IsProduction()
            || smOptions.DatabaseProvider == DatabaseProvider.Sqlite
        )
        {
            using var scope = app.Services.CreateScope();
            var infos = scope.ServiceProvider.GetServices<ModuleDbContextInfo>();

            foreach (var info in infos)
            {
                if (scope.ServiceProvider.GetService(info.DbContextType) is not DbContext db)
                    continue;

                // DbContexts with EF migrations use MigrateAsync; those without (e.g. scaffolded
                // module contexts that ship no migrations) fall back to EnsureCreatedAsync so
                // their tables exist on first run. In-repo module contexts ship no migrations
                // and rely on the unified HostDbContext for schema, but packaged (installed)
                // modules MUST bundle their own EF migrations — EnsureCreated cannot evolve
                // an existing database across module versions.
                var hasMigrations = db.Database.GetMigrations().Any();
                if (info.ModuleName == DatabaseConstants.HostModuleName)
                {
                    if (hasMigrations)
                    {
                        await db.Database.MigrateAsync();
                    }
                    else
                    {
                        await db.Database.EnsureCreatedAsync();
                    }
                }
                else if (hasMigrations)
                {
                    await db.Database.MigrateAsync();
                }
            }
        }

        if (migrateOnly)
        {
            Console.WriteLine(
                "SIMPLEMODULE_MIGRATE_ONLY=1: database initialization complete; exiting without starting the server."
            );
            Environment.Exit(0);
        }

        app.UseForwardedHeaders();

        var errorHtmlPath = Path.Combine(app.Environment.WebRootPath, "error.html");
        var errorHtmlBytes = File.Exists(errorHtmlPath)
            ? await File.ReadAllBytesAsync(errorHtmlPath)
            : null;

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                if (context.Response.HasStarted)
                    return;

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/html";
                if (errorHtmlBytes is not null)
                {
                    await context.Response.Body.WriteAsync(errorHtmlBytes);
                }
                else
                {
                    await context.Response.WriteAsync("<h1>500 Internal Server Error</h1>");
                }
            });
        });

        var options = app.Services.GetRequiredService<SimpleModuleOptions>();
        if (options.EnableSwagger && app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        var isDevelopment = app.Environment.IsDevelopment();
        var cspOptions = options.Csp;

        // Directives never change after startup, so build everything except the
        // per-request nonce once. Per request we only do a single concat.
        var connectSrc = isDevelopment
            ? $"'self' ws: wss: https: {string.Join(' ', cspOptions.ConnectSources)}"
            : $"'self' https: {string.Join(' ', cspOptions.ConnectSources)}";

        var cspPrefix = "default-src 'none'; script-src 'self' 'nonce-";
        var cspSuffix =
            $"'; style-src 'self' 'unsafe-inline' fonts.googleapis.com rsms.me {string.Join(' ', cspOptions.StyleSources)}; "
            + $"font-src 'self' fonts.gstatic.com rsms.me {string.Join(' ', cspOptions.FontSources)}; "
            + $"worker-src 'self' blob: {string.Join(' ', cspOptions.WorkerSources)}; "
            + $"connect-src {connectSrc}; "
            + $"img-src 'self' data: https: {string.Join(' ', cspOptions.ImgSources)}; "
            + "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none';";
        var cspSuffixHttps = cspSuffix + " upgrade-insecure-requests;";

        app.Use(
            async (context, next) =>
            {
                var nonce = context.RequestServices.GetRequiredService<ICspNonce>().Value;
                var isHttps = context.Request.IsHttps;
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    headers["X-Content-Type-Options"] = "nosniff";
                    headers["X-Frame-Options"] = "SAMEORIGIN";
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    headers["X-Permitted-Cross-Domain-Policies"] = "none";
                    headers["Content-Security-Policy"] = string.Concat(
                        cspPrefix,
                        nonce,
                        isHttps ? cspSuffixHttps : cspSuffix
                    );
                    return Task.CompletedTask;
                });
                await next();
            }
        );
        // Vite dev server proxy — intercepts /@vite/, /@fs/, .tsx requests and
        // proxies them to the Vite dev server. Also sets HttpContext.Items["ViteDevServer"]
        // so downstream middleware (Inertia renderer) can adapt the HTML.
        if (options.EnableDevTools && isDevelopment)
        {
            app.UseMiddleware<ViteDevMiddleware>();
        }

        app.UseInertia();
        UseStaticFileCaching(app);
        // MapStaticAssets registers endpoints, which would otherwise inherit the
        // RequireAuthenticatedUser fallback policy — that breaks JS bundle / CSS /
        // favicon loads on anonymous pages like /Identity/Account/Login. Static
        // files are intentionally public.
        app.MapStaticAssets().AllowAnonymous();

        // Maintenance gate runs after static assets (so the 503 page can load
        // its CSS) but before auth (so anonymous users get 503 rather than a
        // login redirect). Health probes are exempt inside the middleware.
        app.UseMiddleware<MaintenanceModeMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSimpleModuleRateLimiting();
        app.UseMiddleware<InertiaLayoutDataMiddleware>();

        if (options.EnableDevTools && app.Environment.IsDevelopment())
        {
            app.MapLiveReload();
        }

        // Module middleware is added by the source-generated UseSimpleModule()
        // via IModule.ConfigureMiddleware() calls.

        UseHomePageRewrite(app);
        app.UseAntiforgery();

        if (options.EnableHealthChecks)
        {
            // Health probes are intentionally anonymous so kubelet / load balancers
            // can hit them without credentials. The framework owns these — do not
            // remove the .AllowAnonymous() calls when sweeping anonymous routes
            // out of an application.
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
                        ResponseWriter = WriteHealthCheckResponse,
                    }
                )
                .AllowAnonymous();
        }

        // Broadcast hub — authenticated by default (the [Authorize] attribute on
        // BroadcastHub kicks the FallbackPolicy back in for the WebSocket / SSE
        // upgrade itself).
        app.MapSimpleModuleBroadcasting();

        app.MapGet("/error/{statusCode:int}", (int statusCode) => RenderErrorPage(statusCode))
            .AllowAnonymous()
            .ExcludeFromDescription();

        // Catch-all for unmatched GET requests — renders a 404 Inertia page
        // for browser navigation to non-existent URLs. Does NOT fire on
        // matched endpoints that return bare 401/403 from auth, so API tests
        // that verify bare status codes remain unaffected.
        app.MapFallback(
                "{**catchAll}",
                (HttpContext context) =>
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return RenderErrorPage(404);
                }
            )
            .AllowAnonymous()
            .ExcludeFromDescription();
    }
}
