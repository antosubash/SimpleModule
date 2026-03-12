using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Core.Inertia;

public static class Inertia
{
    public static IResult Render(string component, object? props = null) =>
        new InertiaResult(component, props);
}

internal sealed class InertiaResult : IResult
{
    private readonly string _component;
    private readonly object? _props;

    public InertiaResult(string component, object? props)
    {
        _component = component;
        _props = props;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var pageData = new
        {
            component = _component,
            props = _props ?? new { },
            url = httpContext.Request.Path + httpContext.Request.QueryString,
            version = InertiaMiddleware.Version,
        };

        if (httpContext.Request.Headers.ContainsKey("X-Inertia"))
        {
            httpContext.Response.Headers["X-Inertia"] = "true";
            httpContext.Response.Headers["Vary"] = "X-Inertia";
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(pageData);
            return;
        }

        var pageJson = JsonSerializer.Serialize(pageData);

        var renderer = httpContext.RequestServices.GetRequiredService<IInertiaPageRenderer>();
        await renderer.RenderPageAsync(httpContext, pageJson);
    }
}
