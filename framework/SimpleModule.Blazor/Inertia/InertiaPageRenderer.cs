using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Blazor.Inertia;

public sealed class InertiaPageRenderer(
    IServiceProvider services,
    ILoggerFactory loggerFactory,
    IOptions<InertiaOptions> options
) : IInertiaPageRenderer
{
    /// <summary>
    /// Cache buster identifier to prevent cache desync in rolling deployments.
    /// Checks DEPLOYMENT_VERSION environment variable first, falls back to assembly version.
    /// This ensures all instances have the same cache buster value for cache coherence.
    /// </summary>
    private static string CacheBuster => GetCacheBuster();

    public async Task RenderPageAsync(HttpContext httpContext, string pageJson)
    {
        await using var renderer = new HtmlRenderer(services, loggerFactory);
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync(
                options.Value.ShellComponent,
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        ["PageJson"] = pageJson,
                        ["HttpContext"] = httpContext,
                        ["CacheBuster"] = CacheBuster,
                    }
                )
            );
            return output.ToHtmlString();
        });

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync(html);
    }

    private static string GetCacheBuster()
    {
        var deploymentVersion = Environment.GetEnvironmentVariable("DEPLOYMENT_VERSION");
        if (!string.IsNullOrEmpty(deploymentVersion))
        {
            return deploymentVersion;
        }

        var assemblyVersion = typeof(InertiaPageRenderer).Assembly
            .GetName()
            .Version
            ?.ToString(3) ?? "1.0.0";
        return assemblyVersion;
    }
}
