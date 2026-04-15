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
using SimpleModule.Core.Caching;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Core.Health;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Core.RateLimiting;
using SimpleModule.Core.Security;
using SimpleModule.Database;
using SimpleModule.Database.Health;
using SimpleModule.Database.Interceptors;
using SimpleModule.DevTools;
using SimpleModule.Hosting.Inertia;
using SimpleModule.Hosting.Middleware;
using SimpleModule.Hosting.RateLimiting;

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

        builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));

        BridgeAspireConnectionString(builder.Configuration);
        options.DatabaseProvider = ValidateDatabaseConfiguration(builder.Configuration);

        builder.Services.Configure<ForwardedHeadersOptions>(fhOptions =>
        {
            fhOptions.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Allow any proxy in containerized/cloud environments
            fhOptions.KnownIPNetworks.Clear();
            fhOptions.KnownProxies.Clear();
        });

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        if (options.EnableSwagger)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }

        builder.Services.AddSingleton<IInertiaPageRenderer, HtmlFileInertiaPageRenderer>();

        // Unified caching abstraction (ICacheStore) shared across all modules.
        builder.Services.AddSimpleModuleCaching();

        builder.Services.AddSingleton<BackgroundEventChannel>();
        builder.Services.AddHostedService<BackgroundEventDispatcher>();
        builder.Services.AddScoped<IEventBus, EventBus>();
        // Lazy<IEventBus> lets services break factory-lambda cycles
        // (e.g. SettingsService ↔ AuditingEventBus via ISettingsContracts).
        builder.Services.AddScoped(sp => new Lazy<IEventBus>(() =>
            sp.GetRequiredService<IEventBus>()
        ));
        builder.Services.AddScoped<InertiaSharedData>();

        // Required by EntityInterceptor to access the current HTTP context
        builder.Services.AddHttpContextAccessor();

        // Entity framework interceptors for automatic entity field population
        builder.Services.AddScoped<ISaveChangesInterceptor, EntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DomainEventInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, EntityChangeInterceptor>();

        // Authentication is configured by modules via their ConfigureServices
        // (e.g., OpenIddict registers SmartAuth policy scheme).
        // Register a baseline so the middleware pipeline works even without an auth module.
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddAntiforgery();

        // Register default IPublicMenuProvider if no module provides one
        builder.Services.TryAddScoped<IPublicMenuProvider, DefaultPublicMenuProvider>();

        builder.Services.AddScoped<ICspNonce, CspNonce>();

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
        var smOptions = app.Services.GetRequiredService<SimpleModuleOptions>();
        if (
            !app.Environment.IsProduction()
            || smOptions.DatabaseProvider == DatabaseProvider.Sqlite
        )
        {
            using var scope = app.Services.CreateScope();
            var infos = scope.ServiceProvider.GetServices<ModuleDbContextInfo>();

            foreach (var info in infos)
            {
                if (info.ModuleName != DatabaseConstants.HostModuleName)
                    continue;

                if (scope.ServiceProvider.GetService(info.DbContextType) is DbContext db)
                {
                    await db.Database.MigrateAsync();
                }
            }
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
        app.MapStaticAssets();

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

    private static IResult RenderErrorPage(int statusCode)
    {
        var (title, message) = statusCode switch
        {
            403 => (ErrorMessages.ForbiddenTitle, ErrorMessages.DefaultForbiddenMessage),
            404 => (ErrorMessages.NotFoundTitle, ErrorMessages.DefaultNotFoundMessage),
            _ => (ErrorMessages.InternalServerErrorTitle, ErrorMessages.UnexpectedError),
        };

        return SimpleModule.Core.Inertia.Inertia.Render(
            $"Error/{statusCode}",
            new
            {
                status = statusCode,
                title,
                message,
            }
        );
    }

    private static void BridgeAspireConnectionString(ConfigurationManager configuration)
    {
        var aspireConnectionString = configuration.GetConnectionString("simplemoduledb");
        if (!string.IsNullOrEmpty(aspireConnectionString))
        {
            configuration["Database:DefaultConnection"] = aspireConnectionString;
        }
    }

    private static DatabaseProvider ValidateDatabaseConfiguration(
        ConfigurationManager configuration
    )
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

        return DatabaseProviderDetector.Detect(connString, dbOptions.Provider);
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

    private static async Task WriteHealthCheckResponse(
        HttpContext context,
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report
    )
    {
        context.Response.ContentType = "application/json";

        var entries = report
            .Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                data = e.Value.Data,
            })
            .ToList();

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = entries,
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
