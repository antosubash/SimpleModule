using System.Linq;
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
                context.Response.Headers[InertiaHttpExtensions.InertiaVersionHeader] = Version;

                if (
                    context.Request.IsInertia()
                    && context.Request.Method == "GET"
                    && context
                        .Request.Headers[InertiaHttpExtensions.InertiaVersionHeader]
                        .FirstOrDefault() != Version
                )
                {
                    context.Response.StatusCode = 409;
                    context.Response.Headers[InertiaHttpExtensions.InertiaLocationHeader] =
                        context.Request.GetEncodedUrl();
                    return;
                }

                await next();

                // Inertia protocol: convert 302 redirects to 303 for
                // PUT/PATCH/DELETE so the browser follows with GET
                if (
                    context.Request.IsInertia()
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

        // Assembly.Location is empty for single-file–published apps; File.GetLastWriteTimeUtc("")
        // would throw and abort all Inertia rendering (this runs in a static initializer). Fall
        // back to the informational/assembly version so the app still starts.
        var location = entryAssembly.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location))
        {
            var buildTime = File.GetLastWriteTimeUtc(location);
            return buildTime.ToString(
                "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture
            );
        }

        var informationalVersion = entryAssembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()
            ?.InformationalVersion;

        return !string.IsNullOrEmpty(informationalVersion)
            ? informationalVersion
            : entryAssembly.GetName().Version?.ToString() ?? "1.0.0";
    }

    private static string GetEncodedUrl(this HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
    }
}
