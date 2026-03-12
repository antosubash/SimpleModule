using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using SimpleModule.Api.Components;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Api.Inertia;

internal sealed class InertiaPageRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
    : IInertiaPageRenderer
{
    public async Task RenderPageAsync(HttpContext httpContext, string pageJson)
    {
        await using var renderer = new HtmlRenderer(services, loggerFactory);
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<InertiaShell>(
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        ["PageJson"] = pageJson,
                        ["HttpContext"] = httpContext,
                    }
                )
            );
            return output.ToHtmlString();
        });

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync(html);
    }
}
