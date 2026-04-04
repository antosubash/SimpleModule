using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Security;

namespace SimpleModule.Hosting.Inertia;

public sealed class HtmlFileInertiaPageRenderer : IInertiaPageRenderer
{
    private const string PagePlaceholder = "<!--INERTIA_PAGE_DATA-->";
    private const string NoncePlaceholder = "<!--CSP_NONCE-->";

    private readonly string _beforePlaceholder;
    private readonly string _afterPlaceholder;

    public HtmlFileInertiaPageRenderer(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.WebRootPath, "index.html");
        var html = File.ReadAllText(path);

        var idx = html.IndexOf(PagePlaceholder, StringComparison.Ordinal);
        if (idx < 0)
            throw new InvalidOperationException(
                $"index.html must contain the placeholder '{PagePlaceholder}'"
            );

        _beforePlaceholder = html[..idx];
        _afterPlaceholder = html[(idx + PagePlaceholder.Length)..];
    }

    public Task RenderPageAsync(HttpContext httpContext, string pageJson)
    {
        var nonce = httpContext.RequestServices.GetRequiredService<ICspNonce>().Value;

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        return httpContext.Response.WriteAsync(
            string.Concat(
                _beforePlaceholder.Replace(NoncePlaceholder, nonce, StringComparison.Ordinal),
                $"<script data-page=\"app\" type=\"application/json\" nonce=\"{nonce}\">{pageJson}</script>",
                _afterPlaceholder.Replace(NoncePlaceholder, nonce, StringComparison.Ordinal)
            )
        );
    }
}
