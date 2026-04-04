using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Hosting.Inertia;

public sealed class HtmlFileInertiaPageRenderer : IInertiaPageRenderer
{
    private const string Placeholder = "<!--INERTIA_PAGE_DATA-->";

    private readonly string _beforePlaceholder;
    private readonly string _afterPlaceholder;

    public HtmlFileInertiaPageRenderer(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.WebRootPath, "index.html");
        var html = File.ReadAllText(path);

        var idx = html.IndexOf(Placeholder, StringComparison.Ordinal);
        if (idx < 0)
            throw new InvalidOperationException(
                $"index.html must contain the placeholder '{Placeholder}'"
            );

        _beforePlaceholder = html[..idx];
        _afterPlaceholder = html[(idx + Placeholder.Length)..];
    }

    public Task RenderPageAsync(HttpContext httpContext, string pageJson)
    {
        httpContext.Response.ContentType = "text/html; charset=utf-8";
        return httpContext.Response.WriteAsync(
            string.Concat(
                _beforePlaceholder,
                $"<script data-page=\"app\" type=\"application/json\">{pageJson}</script>",
                _afterPlaceholder
            )
        );
    }
}
