using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Menu;
using SimpleModule.Database;

namespace SimpleModule.Hosting;

public static partial class SimpleModuleHostExtensions
{
    private const int ImmutableAssetsCacheDurationSeconds = 31536000; // 365 days
    private const string VendorJsPathPrefix = "/js/vendor/";
    private const string ModuleContentPathPrefix = "/_content/";
    private const string ModuleScriptExtension = ".mjs";

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

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
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
