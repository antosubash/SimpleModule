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
    private const string VersionPlaceholder = "<!--DEPLOY_VERSION-->";

    private readonly string _beforePlaceholder;
    private readonly string _afterPlaceholder;

    public HtmlFileInertiaPageRenderer(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.WebRootPath, "index.html");
        var html = File.ReadAllText(path);

        // Inject deployment version for cache-busting JS/CSS imports.
        // Uses the same version as the Inertia protocol so stale clients
        // are detected and forced to do full page reloads.
        html = html.Replace(
            VersionPlaceholder,
            InertiaMiddleware.Version,
            StringComparison.Ordinal
        );

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
