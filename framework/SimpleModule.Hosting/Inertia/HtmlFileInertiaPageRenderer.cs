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
    }

    public Task RenderPageAsync(HttpContext httpContext, string pageJson)
    {
        var nonce = httpContext.RequestServices.GetRequiredService<ICspNonce>().Value;

        var devScript = _isDevelopment ? GetLiveReloadScript(nonce) : "";

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        return httpContext.Response.WriteAsync(
            string.Concat(
                _beforePlaceholder.Replace(NoncePlaceholder, nonce, StringComparison.Ordinal),
                $"<script data-page=\"app\" type=\"application/json\" nonce=\"{nonce}\">{pageJson}</script>",
                devScript,
                _afterPlaceholder.Replace(NoncePlaceholder, nonce, StringComparison.Ordinal)
            )
        );
    }

    private static string GetLiveReloadScript(string nonce) =>
        "<script nonce=\"" + nonce + "\">" + LiveReloadClientScript + "</script>";

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
