using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SimpleModule.DevTools;

/// <summary>
/// Middleware that proxies development asset requests to the Vite dev server
/// and transforms HTML responses to use Vite HMR.
/// Only active when the Vite dev server is detected.
/// </summary>
public sealed partial class ViteDevMiddleware : IDisposable
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ViteDevMiddleware> _logger;
    private readonly HttpClient _httpClient;
    private readonly Uri _viteBaseUri;
    private readonly int _vitePort;
    private bool _viteDetected;
    private DateTime _lastCheck = DateTime.MinValue;

    /// <summary>Path prefixes that should be proxied to Vite dev server.</summary>
    private static readonly string[] ViteProxyPrefixes =
    [
        "/@vite/",
        "/@react-refresh",
        "/@id/",
        "/@fs/",
        "/node_modules/",
    ];

    public ViteDevMiddleware(
        RequestDelegate next,
        ILogger<ViteDevMiddleware> logger,
        int vitePort = 5173
    )
    {
        _next = next;
        _logger = logger;
        _vitePort = vitePort;
        _viteBaseUri = new Uri($"http://localhost:{vitePort}");
        _httpClient = new HttpClient
        {
            BaseAddress = _viteBaseUri,
            Timeout = TimeSpan.FromSeconds(5),
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Periodically check if Vite dev server is running (every 10 seconds)
        if (DateTime.UtcNow - _lastCheck > TimeSpan.FromSeconds(10))
        {
            _viteDetected = await IsViteRunningAsync().ConfigureAwait(false);
            _lastCheck = DateTime.UtcNow;
            if (_viteDetected)
            {
                LogViteDetected(_logger, _vitePort);
            }
        }

        if (!_viteDetected)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Signal to HtmlFileInertiaPageRenderer that Vite dev server is active
        context.Items["ViteDevServer"] = true;

        // Proxy Vite-specific requests
        if (ShouldProxy(path))
        {
            await ProxyToViteAsync(context).ConfigureAwait(false);
            return;
        }

        // Proxy source file requests (.tsx, .ts, .css, .jsx, .js — not built assets)
        if (IsSourceFileRequest(path))
        {
            await ProxyToViteAsync(context).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool ShouldProxy(string path)
    {
        foreach (var prefix in ViteProxyPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSourceFileRequest(string path)
    {
        // Proxy requests for the app entry point (app.tsx)
        if (
            path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        return false;
    }

    private async Task ProxyToViteAsync(HttpContext context)
    {
        try
        {
            var requestUri = context.Request.Path.Value + context.Request.QueryString;

            using var proxyRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);

            // Forward relevant headers
            if (context.Request.Headers.TryGetValue("Accept", out var accept))
            {
                proxyRequest.Headers.TryAddWithoutValidation("Accept", accept.ToString());
            }

            using var response = await _httpClient
                .SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            context.Response.StatusCode = (int)response.StatusCode;

            // Forward content type
            if (response.Content.Headers.ContentType is not null)
            {
                context.Response.ContentType = response.Content.Headers.ContentType.ToString();
            }

            // Forward CORS headers from Vite
            foreach (var header in response.Headers)
            {
                if (header.Key.StartsWith("Access-Control", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            await response.Content.CopyToAsync(context.Response.Body).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogProxyFailed(_logger, ex, context.Request.Path.Value ?? "");
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
        }
    }

    private async Task<bool> IsViteRunningAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "/");
            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
            return true;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            return false;
        }
#pragma warning restore CA1031
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Vite dev server detected on port {Port}"
    )]
    private static partial void LogViteDetected(ILogger logger, int port);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to proxy request to Vite dev server: {Path}"
    )]
    private static partial void LogProxyFailed(ILogger logger, Exception ex, string path);
}
