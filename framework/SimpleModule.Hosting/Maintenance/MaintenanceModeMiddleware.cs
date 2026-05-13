using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Hosting.Maintenance;

/// <summary>
/// Short-circuits requests with HTTP 503 when the maintenance sentinel is active,
/// unless the caller presents a valid bypass cookie or visits with a
/// <c>?sm_bypass=&lt;secret&gt;</c> query string (which sets the cookie and redirects).
/// </summary>
public sealed class MaintenanceModeMiddleware
{
    private const string BypassQueryParameter = "sm_bypass";

    private static readonly JsonSerializerOptions JsonResponseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RequestDelegate _next;
    private readonly IMaintenanceModeStore _store;
    private readonly MaintenanceModeOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly Lazy<byte[]?> _maintenanceHtml;

    public MaintenanceModeMiddleware(
        RequestDelegate next,
        IMaintenanceModeStore store,
        IOptions<MaintenanceModeOptions> options,
        IHostEnvironment environment
    )
    {
        _next = next;
        _store = store;
        _options = options.Value;
        _environment = environment;
        _maintenanceHtml = new Lazy<byte[]?>(LoadMaintenanceHtml);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Health probes always pass through; if the app is in maintenance the load
        // balancer still needs to know the process is alive.
        var path = context.Request.Path;
        if (
            path.StartsWithSegments(RouteConstants.HealthLive, StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(
                RouteConstants.HealthReady,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            await _next(context);
            return;
        }

        var state = _store.GetState();
        if (state is null)
        {
            await _next(context);
            return;
        }

        // ?sm_bypass=<secret> → set cookie + redirect to the same URL without the secret in it.
        if (context.Request.Query.TryGetValue(BypassQueryParameter, out var providedSecret))
        {
            if (state.SecretHash is not null && MatchesSecret(providedSecret.ToString(), state))
            {
                IssueBypassCookie(context);
                var redirect = StripBypassFromUrl(context.Request);
                context.Response.Redirect(redirect);
                return;
            }
            // Wrong secret — fall through to the 503 response.
        }

        if (HasValidBypassCookie(context, state))
        {
            await _next(context);
            return;
        }

        await WriteMaintenanceResponseAsync(context, state);
    }

    private bool HasValidBypassCookie(HttpContext context, MaintenanceModeState state)
    {
        if (state.SecretHash is null)
        {
            return false;
        }

        if (!context.Request.Cookies.TryGetValue(_options.BypassCookieName, out var cookieValue))
        {
            return false;
        }

        return string.Equals(cookieValue, state.SecretHash, StringComparison.Ordinal);
    }

    private void IssueBypassCookie(HttpContext context)
    {
        var state = _store.GetState();
        if (state?.SecretHash is null)
        {
            return;
        }

        context.Response.Cookies.Append(
            _options.BypassCookieName,
            state.SecretHash,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps || !_environment.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                MaxAge = _options.BypassCookieLifetime,
                Path = "/",
            }
        );
    }

    private static string StripBypassFromUrl(HttpRequest request)
    {
        var remaining = request
            .Query.Where(kv =>
                !string.Equals(kv.Key, BypassQueryParameter, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        var path = request.PathBase + request.Path;
        if (remaining.Count == 0)
        {
            return path;
        }

        var query = string.Join(
            '&',
            remaining.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"
            )
        );
        return $"{path}?{query}";
    }

    private static bool MatchesSecret(string provided, MaintenanceModeState state)
    {
        if (string.IsNullOrEmpty(provided) || state.SecretHash is null)
        {
            return false;
        }

        var providedHash = HashSecret(provided);
        var providedBytes = Encoding.ASCII.GetBytes(providedHash);
        var storedBytes = Encoding.ASCII.GetBytes(state.SecretHash);
        if (providedBytes.Length != storedBytes.Length)
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(providedBytes, storedBytes);
    }

    /// <summary>
    /// Hashes a bypass secret to the hex form persisted in the sentinel.
    /// Shared with the CLI so writes and reads agree on the format.
    /// </summary>
    public static string HashSecret(string secret)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        // Lowercase hex matches the Convert.ToHexStringLower form the .NET 9+ APIs use
        // and is what's persisted in the sentinel; uppercase would break ordinal comparisons.
        return Convert.ToHexStringLower(bytes);
    }

    private async Task WriteMaintenanceResponseAsync(
        HttpContext context,
        MaintenanceModeState state
    )
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers["Retry-After"] = state.RetryAfterSeconds.ToString(
            CultureInfo.InvariantCulture
        );

        var isInertia = context.Request.Headers.ContainsKey("X-Inertia");
        var acceptsJson = context.Request.Headers.Accept.Any(a =>
            a is not null && a.Contains("application/json", StringComparison.OrdinalIgnoreCase)
        );

        if (isInertia || acceptsJson)
        {
            await WriteJsonAsync(context, state);
            return;
        }

        await WriteHtmlAsync(context, state);
    }

    private static async Task WriteJsonAsync(HttpContext context, MaintenanceModeState state)
    {
        context.Response.ContentType = "application/json";
        var pageData = new
        {
            component = "System/Maintenance",
            props = new
            {
                message = state.Message,
                retryAfterSeconds = state.RetryAfterSeconds,
                until = state.Until,
            },
            url = context.Request.Path + context.Request.QueryString,
            version = InertiaMiddleware.Version,
        };

        if (context.Request.Headers.ContainsKey("X-Inertia"))
        {
            context.Response.Headers["X-Inertia"] = "true";
            context.Response.Headers["Vary"] = "X-Inertia";
        }

        await JsonSerializer.SerializeAsync(context.Response.Body, pageData, JsonResponseOptions);
    }

    private async Task WriteHtmlAsync(HttpContext context, MaintenanceModeState state)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var html = _maintenanceHtml.Value;
        if (html is not null)
        {
            await context.Response.Body.WriteAsync(html);
            return;
        }

        var message = string.IsNullOrWhiteSpace(state.Message)
            ? "We&#39;ll be back shortly."
            : System.Net.WebUtility.HtmlEncode(state.Message);
        var fallback =
            "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>Maintenance</title></head>"
            + $"<body><h1>503 Service Unavailable</h1><p>{message}</p></body></html>";
        await context.Response.WriteAsync(fallback);
    }

    private byte[]? LoadMaintenanceHtml()
    {
        try
        {
            var path = Path.Combine(_environment.ContentRootPath, "wwwroot", "maintenance.html");
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
