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
            _beforePlaceholderViteDev = TransformForViteDev(_beforePlaceholder);
            _afterPlaceholderViteDev = TransformForViteDev(_afterPlaceholder)
                .Replace(
                    "<script type=\"module\" src=\"/js/app.js\"></script>",
                    ViteEntryPlaceholder,
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

        html = html.Replace(
            "<link rel=\"stylesheet\" href=\"/css/app.css\" />",
            "",
            StringComparison.Ordinal
        );

        return html;
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
