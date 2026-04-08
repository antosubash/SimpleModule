using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Security;
using SimpleModule.DevTools;

namespace SimpleModule.Hosting.Inertia;

public sealed class HtmlFileInertiaPageRenderer : IInertiaPageRenderer
{
    private const string PagePlaceholder = "<!--INERTIA_PAGE_DATA-->";
    private const string NoncePlaceholder = "<!--CSP_NONCE-->";
    private const string VersionPlaceholder = "<!--DEPLOY_VERSION-->";
    private const string ModuleCssPlaceholder = "<!--MODULE_CSS_LINKS-->";

    private readonly string _beforePlaceholder;
    private readonly string _afterPlaceholder;
    private readonly string _beforePlaceholderViteDev;
    private readonly string _afterPlaceholderViteDev;
    private readonly bool _isDevelopment;

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

        // Inject <link> tags for every module RCL that ships its own CSS file
        // (e.g. _content/SimpleModule.PageBuilder/pagebuilder.css). Discovered
        // once at startup by walking the WebRootFileProvider, which sees both
        // the host's physical wwwroot and every RCL's static web assets.
        html = html.Replace(
            ModuleCssPlaceholder,
            BuildModuleCssLinks(env, InertiaMiddleware.Version),
            StringComparison.Ordinal
        );

        var idx = html.IndexOf(PagePlaceholder, StringComparison.Ordinal);
        if (idx < 0)
            throw new InvalidOperationException(
                $"index.html must contain the placeholder '{PagePlaceholder}'"
            );

        _beforePlaceholder = html[..idx];
        _afterPlaceholder = html[(idx + PagePlaceholder.Length)..];
        _isDevelopment = env.IsDevelopment();

        if (_isDevelopment)
        {
            _beforePlaceholderViteDev = TransformForViteDev(_beforePlaceholder);
            // Replace the app.js script tag (with or without ?v= cache-buster) with Vite entry
            _afterPlaceholderViteDev = ReplaceAppJsScript(TransformForViteDev(_afterPlaceholder));
        }
        else
        {
            _beforePlaceholderViteDev = _beforePlaceholder;
            _afterPlaceholderViteDev = _afterPlaceholder;
        }
    }

    public Task RenderPageAsync(HttpContext httpContext, string pageJson)
    {
        var nonce = httpContext.RequestServices.GetRequiredService<ICspNonce>().Value;
        var useViteDev =
            _isDevelopment && httpContext.Items.ContainsKey(DevToolsConstants.ViteDevServerKey);

        var before = useViteDev ? _beforePlaceholderViteDev : _beforePlaceholder;
        var after = useViteDev ? _afterPlaceholderViteDev : _afterPlaceholder;
        var devScript =
            _isDevelopment && !useViteDev
                ? "<script nonce=\"" + nonce + "\">" + LiveReloadClientScript + "</script>"
                : "";

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        return httpContext.Response.WriteAsync(
            string.Concat(
                before.Replace(NoncePlaceholder, nonce, StringComparison.Ordinal),
                $"<script data-page=\"app\" type=\"application/json\" nonce=\"{nonce}\">{pageJson}</script>",
                devScript,
                after.Replace(NoncePlaceholder, nonce, StringComparison.Ordinal)
            )
        );
    }

    private static string BuildModuleCssLinks(IWebHostEnvironment env, string version)
    {
        var contents = env.WebRootFileProvider.GetDirectoryContents("_content");
        if (!contents.Exists)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var entry in contents)
        {
            if (
                !entry.IsDirectory
                || !entry.Name.StartsWith("SimpleModule.", StringComparison.Ordinal)
            )
                continue;

            // Module Vite builds emit CSS as {assembly}.lowercase().css by convention
            // (see define-module-config.ts assetFileNames). The URL must match the
            // on-disk filename, so ToLowerInvariant is the correct behavior here, not
            // the security-focused ToUpperInvariant that CA1308 suggests.
#pragma warning disable CA1308
            var cssFileName = entry.Name.ToLowerInvariant() + ".css";
#pragma warning restore CA1308
            var cssPath = $"_content/{entry.Name}/{cssFileName}";
            if (!env.WebRootFileProvider.GetFileInfo(cssPath).Exists)
                continue;

            sb.Append("<link rel=\"stylesheet\" href=\"/")
                .Append(cssPath)
                .Append("?v=")
                .Append(version)
                .Append("\" />");
        }
        return sb.ToString();
    }

    private static string TransformForViteDev(string html)
    {
        var importMapStart = html.IndexOf("<script type=\"importmap\"", StringComparison.Ordinal);
        if (importMapStart >= 0)
        {
            var importMapEnd = html.IndexOf("</script>", importMapStart, StringComparison.Ordinal);
            if (importMapEnd >= 0)
            {
                html = string.Concat(
                    html.AsSpan(0, importMapStart),
                    html.AsSpan(importMapEnd + "</script>".Length)
                );
            }
        }

        // Strip the app.css link (with or without ?v= cache-buster) — Vite serves CSS via HMR
        html = StripTag(html, "<link", "/css/app.css", "/>");

        return html;
    }

    private static string ReplaceAppJsScript(string html)
    {
        return ReplaceTag(html, "<script", "/js/app.js", "</script>", ViteEntryPlaceholder);
    }

    private static string StripTag(string html, string tagStart, string marker, string tagEnd)
    {
        return ReplaceTag(html, tagStart, marker, tagEnd, "");
    }

    private static string ReplaceTag(
        string html,
        string tagStart,
        string marker,
        string tagEnd,
        string replacement
    )
    {
        var markerIdx = html.IndexOf(marker, StringComparison.Ordinal);
        if (markerIdx < 0)
            return html;

        var start = html.LastIndexOf(tagStart, markerIdx, StringComparison.Ordinal);
        if (start < 0)
            return html;

        var end = html.IndexOf(tagEnd, markerIdx, StringComparison.Ordinal);
        if (end < 0)
            return html;

        return string.Concat(html.AsSpan(0, start), replacement, html.AsSpan(end + tagEnd.Length));
    }

    /// <summary>
    /// Placeholder for Vite entry scripts — nonce is injected at render time
    /// via the <see cref="NoncePlaceholder"/> replacement.
    /// </summary>
    private const string ViteEntryPlaceholder =
        "<script type=\"module\" src=\"/@vite/client\" nonce=\""
        + NoncePlaceholder
        + "\"></script>\n"
        + "    <script type=\"module\" src=\"/app.tsx\" nonce=\""
        + NoncePlaceholder
        + "\"></script>";

    private const string LiveReloadClientScript = """
        (function(){
          var protocol=location.protocol==='https:'?'wss:':'ws:';
          var url=protocol+'//'+location.host+'/dev/live-reload';
          var reconnectDelay=1000;
          var maxReconnectDelay=10000;
          var cssVersion=0;

          function connect(){
            var ws=new WebSocket(url);
            ws.onopen=function(){
              console.log('[LiveReload] Connected');
              reconnectDelay=1000;
            };
            ws.onmessage=function(event){
              try{
                var msg=JSON.parse(event.data);
                if(msg.type==='CssOnly'){
                  reloadCss(msg.source);
                }else{
                  console.log('[LiveReload] Reloading ('+msg.source+')');
                  location.reload();
                }
              }catch(e){
                location.reload();
              }
            };
            ws.onclose=function(){
              console.log('[LiveReload] Disconnected, reconnecting in '+(reconnectDelay/1000)+'s...');
              setTimeout(connect,reconnectDelay);
              reconnectDelay=Math.min(reconnectDelay*2,maxReconnectDelay);
            };
          }

          function reloadCss(source){
            cssVersion++;
            var links=document.querySelectorAll('link[rel="stylesheet"]');
            links.forEach(function(link){
              var href=link.getAttribute('href');
              if(href){
                var base=href.split('?')[0];
                link.href=base+'?v='+cssVersion;
              }
            });
            console.log('[LiveReload] CSS refreshed ('+source+')');
          }

          connect();
        })();
        """;
}
