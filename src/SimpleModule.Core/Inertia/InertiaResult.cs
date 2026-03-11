using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Inertia;

public static class Inertia
{
    public static IResult Render(string component, object? props = null)
        => new InertiaResult(component, props);
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
        var escapedJson = System.Net.WebUtility.HtmlEncode(pageJson);

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync($$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <meta name="color-scheme" content="light dark" />
                <link rel="preconnect" href="https://fonts.googleapis.com" />
                <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
                <link href="https://fonts.googleapis.com/css2?family=DM+Sans:ital,opsz,wght@0,9..40,300..700;1,9..40,300..700&family=JetBrains+Mono:wght@400;500;600&family=Sora:wght@400;500;600;700;800&display=swap" rel="stylesheet" />
                <link rel="stylesheet" href="/css/app.css" />
                <title>SimpleModule</title>
                <script type="importmap">
                {
                    "imports": {
                        "react": "/js/vendor/react.js",
                        "react-dom": "/js/vendor/react-dom.js",
                        "react/jsx-runtime": "/js/vendor/react-jsx-runtime.js",
                        "react-dom/client": "/js/vendor/react-dom-client.js",
                        "@inertiajs/react": "/js/vendor/inertiajs-react.js"
                    }
                }
                </script>
            </head>
            <body>
                <div id="app" data-page="{{escapedJson}}"></div>
                <script type="module" src="/js/app.js"></script>
            </body>
            </html>
            """);
    }
}
