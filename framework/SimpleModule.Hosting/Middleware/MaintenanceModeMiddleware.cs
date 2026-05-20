using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Maintenance;
using SimpleModule.Hosting.Maintenance;

namespace SimpleModule.Hosting.Middleware;

/// <summary>
/// Short-circuits requests with a 503 when the maintenance sentinel is active.
/// Health-check routes are exempt so probes can keep distinguishing
/// "deployment in progress" from "host is down". A bypass query parameter
/// (<c>?sm_bypass=&lt;secret&gt;</c>) verifies the secret hash and writes an
/// <c>sm_bypass</c> cookie so subsequent requests pass straight through.
/// </summary>
public sealed class MaintenanceModeMiddleware
{
    private const string BypassQueryParameter = "sm_bypass";

    private readonly RequestDelegate _next;
    private readonly IMaintenanceStateProvider _stateProvider;
    private readonly MaintenanceModeOptions _options;

    public MaintenanceModeMiddleware(
        RequestDelegate next,
        IMaintenanceStateProvider stateProvider,
        IOptions<MaintenanceModeOptions> options
    )
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExempt(context))
        {
            await _next(context);
            return;
        }

        var state = await _stateProvider.GetAsync(context.RequestAborted);
        if (state is not { Active: true })
        {
            await _next(context);
            return;
        }

        if (TryConsumeBypassQuery(context, state))
        {
            return; // already redirected
        }

        if (HasValidBypassCookie(context, state))
        {
            await _next(context);
            return;
        }

        await WriteMaintenanceResponseAsync(context, state);
    }

    private static bool IsExempt(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return path.StartsWith(RouteConstants.HealthLive, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(RouteConstants.HealthReady, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryConsumeBypassQuery(HttpContext context, MaintenanceState state)
    {
        if (!context.Request.Query.TryGetValue(BypassQueryParameter, out var provided))
        {
            return false;
        }

        var secret = provided.ToString();
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(state.SecretHash))
        {
            return false;
        }

        if (!HashSecret(secret).Equals(state.SecretHash, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        context.Response.Cookies.Append(
            _options.BypassCookieName,
            state.SecretHash,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.Add(_options.BypassCookieLifetime),
                IsEssential = true,
                Path = "/",
            }
        );

        var redirect = context.Request.Path.HasValue
            ? context.Request.Path.Value!
            : "/";
        context.Response.Redirect(redirect);
        return true;
    }

    private bool HasValidBypassCookie(HttpContext context, MaintenanceState state)
    {
        if (string.IsNullOrEmpty(state.SecretHash))
        {
            return false;
        }

        if (!context.Request.Cookies.TryGetValue(_options.BypassCookieName, out var cookieValue))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(cookieValue),
            Encoding.ASCII.GetBytes(state.SecretHash)
        );
    }

    private static async Task WriteMaintenanceResponseAsync(HttpContext context, MaintenanceState state)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = state.RetryAfterSeconds.ToString(
            CultureInfo.InvariantCulture
        );
        context.Response.Headers.CacheControl = "no-store";

        var payload = new
        {
            status = StatusCodes.Status503ServiceUnavailable,
            title = "Service unavailable",
            message = state.Message ?? "The application is undergoing scheduled maintenance.",
            retryAfterSeconds = state.RetryAfterSeconds,
            until = state.Until,
        };

        // Inertia / API callers want JSON; browsers navigating directly want
        // an HTML page they can render without a JS bundle (the JS bundle
        // itself may be cached, but we cannot assume).
        if (PrefersJson(context))
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            await JsonSerializer.SerializeAsync(context.Response.Body, payload);
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(RenderHtml(payload.title, payload.message, state));
    }

    private static bool PrefersJson(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("X-Inertia"))
        {
            return true;
        }

        var accept = context.Request.Headers.Accept.ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            && !accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static string RenderHtml(string title, string message, MaintenanceState state)
    {
        var encodedTitle = System.Net.WebUtility.HtmlEncode(title);
        var encodedMessage = System.Net.WebUtility.HtmlEncode(message);
        var retryHint = state.RetryAfterSeconds > 0
            ? "<p style=\"color:#666;font-size:0.85rem;margin-top:1.5rem;\">Please try again in about "
                + state.RetryAfterSeconds
                + " seconds.</p>"
            : string.Empty;

        return """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>__TITLE__</title>
              <style>
                body { font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; background: #f8fafc; color: #0f172a; display: grid; place-items: center; min-height: 100vh; margin: 0; padding: 1.5rem; }
                main { max-width: 28rem; text-align: center; }
                h1 { font-size: 5rem; font-weight: 700; color: rgba(15, 23, 42, 0.15); margin: 0; line-height: 1; }
                h2 { font-size: 1.5rem; margin: 0.5rem 0 0.75rem; }
                p { color: #475569; line-height: 1.5; }
              </style>
            </head>
            <body>
              <main>
                <h1>503</h1>
                <h2>__TITLE__</h2>
                <p>__MESSAGE__</p>
                __RETRY__
              </main>
            </body>
            </html>
            """
            .Replace("__TITLE__", encodedTitle, StringComparison.Ordinal)
            .Replace("__MESSAGE__", encodedMessage, StringComparison.Ordinal)
            .Replace("__RETRY__", retryHint, StringComparison.Ordinal);
    }

    /// <summary>
    /// SHA-256 hex (lowercase) of the bypass secret. Exposed so tests and
    /// out-of-band tooling can compute a hash that matches what the sentinel
    /// expects without re-implementing the algorithm.
    /// </summary>
    public static string HashSecret(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
