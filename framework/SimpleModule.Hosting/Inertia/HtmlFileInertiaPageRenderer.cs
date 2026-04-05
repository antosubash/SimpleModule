using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Security;

namespace SimpleModule.Hosting.Inertia;

public sealed class HtmlFileInertiaPageRenderer : IInertiaPageRenderer
{
    private const string PagePlaceholder = "<!--INERTIA_PAGE_DATA-->";
    private const string NoncePlaceholder = "<!--CSP_NONCE-->";

    private readonly string _beforePlaceholder;
    private readonly string _afterPlaceholder;
    private readonly string _beforePlaceholderViteDev;
    private readonly string _afterPlaceholderViteDev;
    private readonly bool _isDevelopment;

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
        _isDevelopment = env.IsDevelopment();

        if (_isDevelopment)
        {
            // Pre-compute the Vite dev mode HTML transformation:
            // 1. Strip import map (Vite handles module resolution)
            // 2. Strip /css/app.css link (Tailwind is served via Vite)
            // 3. Replace /js/app.js with Vite entry point
            _beforePlaceholderViteDev = TransformForViteDev(_beforePlaceholder);
            _afterPlaceholderViteDev = TransformForViteDev(_afterPlaceholder)
                .Replace(
                    "<script type=\"module\" src=\"/js/app.js\"></script>",
                    ViteEntryScripts,
                    StringComparison.Ordinal
                );
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

        // Detect Vite dev mode via request header set by ViteDevMiddleware
        var useViteDev = _isDevelopment && httpContext.Items.ContainsKey("ViteDevServer");

        string before;
        string after;
        string devScript;

        if (useViteDev)
        {
            before = _beforePlaceholderViteDev;
            after = _afterPlaceholderViteDev;
            devScript = "";
        }
        else if (_isDevelopment)
        {
            before = _beforePlaceholder;
            after = _afterPlaceholder;
            devScript = "<script nonce=\"" + nonce + "\">" + LiveReloadClientScript + "</script>";
        }
        else
        {
            before = _beforePlaceholder;
            after = _afterPlaceholder;
            devScript = "";
        }

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

    /// <summary>
    /// Transforms HTML for Vite dev server mode by stripping import maps
    /// and the pre-built CSS link.
    /// </summary>
    private static string TransformForViteDev(string html)
    {
        // Remove the import map script block (Vite handles module resolution)
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

        // Remove the pre-built CSS link (Tailwind is served via @tailwindcss/vite)
        html = html.Replace(
            "<link rel=\"stylesheet\" href=\"/css/app.css\" />",
            "",
            StringComparison.Ordinal
        );

        return html;
    }

    /// <summary>
    /// Script tags injected in Vite dev mode to load the HMR client and
    /// the app entry point from Vite dev server (proxied through ASP.NET).
    /// </summary>
    private const string ViteEntryScripts = """
        <script type="module" src="/@vite/client"></script>
        <script type="module" src="/app.tsx"></script>
        """;

    /// <summary>
    /// Fallback live reload script for when Vite dev server is not running
    /// (e.g. running just <c>dotnet run</c> with the file-watch service).
    /// </summary>
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
