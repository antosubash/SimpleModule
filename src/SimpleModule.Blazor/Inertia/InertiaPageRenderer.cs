using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Blazor.Inertia;

public sealed class InertiaPageRenderer(
    IServiceProvider services,
    ILoggerFactory loggerFactory,
    IOptions<InertiaOptions> options,
    IModuleCssProvider cssProvider
) : IInertiaPageRenderer
{
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
                        ["ModuleCssPaths"] = cssProvider.CssPaths,
                    }
                )
            );
            return output.ToHtmlString();
        });

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync(html);
    }
}
