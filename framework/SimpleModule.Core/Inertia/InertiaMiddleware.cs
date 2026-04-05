using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Inertia;

public static class InertiaMiddleware
{
    private const string DeploymentVersionEnvVar = "DEPLOYMENT_VERSION";

    /// <summary>
    /// Inertia protocol version. Must match CacheBuster for 409 stale-version detection.
    /// Checks DEPLOYMENT_VERSION environment variable first, falls back to assembly version.
    /// This ensures rolling deployments consistently invalidate stale client caches.
    /// </summary>
    public static readonly string Version = GetVersion();

    public static IApplicationBuilder UseInertia(this IApplicationBuilder app)
    {
        return app.Use(
            async (context, next) =>
            {
                context.Response.Headers["X-Inertia-Version"] = Version;

                if (
                    context.Request.Headers.ContainsKey("X-Inertia")
                    && context.Request.Method == "GET"
                    && context.Request.Headers["X-Inertia-Version"].FirstOrDefault() != Version
                )
                {
                    context.Response.StatusCode = 409;
                    context.Response.Headers["X-Inertia-Location"] =
                        context.Request.GetEncodedUrl();
                    return;
                }

                await next();

                // Inertia protocol: convert 302 redirects to 303 for
                // PUT/PATCH/DELETE so the browser follows with GET
                if (
                    context.Request.Headers.ContainsKey("X-Inertia")
                    && context.Response.StatusCode == 302
                    && context.Request.Method != "GET"
                )
                {
                    context.Response.StatusCode = 303;
                }
            }
        );
    }

    private static string GetVersion()
    {
        var deploymentVersion = Environment.GetEnvironmentVariable(DeploymentVersionEnvVar);
        if (!string.IsNullOrEmpty(deploymentVersion))
        {
            return deploymentVersion;
        }

        // No explicit version set — generate one from the entry assembly's build timestamp.
        // This changes on every recompile/publish, ensuring cache-busting without manual config.
        var entryAssembly =
            System.Reflection.Assembly.GetEntryAssembly() ?? typeof(InertiaMiddleware).Assembly;
        var buildTime = File.GetLastWriteTimeUtc(entryAssembly.Location);
        return buildTime.ToString(
            "yyyyMMddHHmmss",
            System.Globalization.CultureInfo.InvariantCulture
        );
    }

    private static string GetEncodedUrl(this HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
    }
}
