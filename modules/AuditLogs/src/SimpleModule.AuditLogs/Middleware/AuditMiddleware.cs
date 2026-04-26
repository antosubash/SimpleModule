using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.Enrichment;
using SimpleModule.AuditLogs.Pipeline;
using SimpleModule.Settings.Contracts;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.AuditLogs.Middleware;

public sealed class AuditMiddleware(RequestDelegate next, IFusionCache cache)
{
    private static readonly string[] ExcludedMethodsForBody = ["HEAD", "OPTIONS"];

    private static readonly string[] DefaultExcludedPaths =
    [
        "/health",
        "/metrics",
        "/_content",
        "/js/",
        "/css/",
        "/favicon",
    ];

    private static readonly FusionCacheEntryOptions AuditConfigCacheOptions = new()
    {
        Duration = TimeSpan.FromSeconds(60),
    };

    private static readonly AuditRequestSettings FallbackSettings = new(
        CaptureHttp: true,
        CaptureRequestBodies: true,
        CaptureQueryStrings: true,
        CaptureUserAgent: false,
        ExcludedPaths: DefaultExcludedPaths
    );

    private sealed record AuditRequestSettings(
        bool CaptureHttp,
        bool CaptureRequestBodies,
        bool CaptureQueryStrings,
        bool CaptureUserAgent,
        string[] ExcludedPaths
    );

    public async Task InvokeAsync(HttpContext context)
    {
        var channel = context.RequestServices.GetService<AuditChannel>();
        if (channel is null)
        {
            await next(context);
            return;
        }

        // Single composite cache entry — avoids 5 separate FusionCache lookups per
        // request and resolves the scoped ISettingsContracts only on cache miss.
        var auditSettings = await LoadSettingsAsync(context.RequestServices, cache);

        // Check if HTTP capture is enabled
        if (!auditSettings.CaptureHttp)
        {
            await next(context);
            return;
        }

        // Check excluded paths
        var path = context.Request.Path.Value ?? "";
        if (IsExcludedPath(path, auditSettings.ExcludedPaths))
        {
            await next(context);
            return;
        }

        // Populate audit context with user info
        var auditContext = context.RequestServices.GetService<IAuditContext>();
        if (auditContext is not null)
        {
            auditContext.UserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            auditContext.UserName = context.User.FindFirstValue(ClaimTypes.Name);
            auditContext.IpAddress = context.Connection.RemoteIpAddress?.ToString();
        }

        // Read request body if applicable
        string? requestBody = null;
        if (
            auditSettings.CaptureRequestBodies
            && !ExcludedMethodsForBody.Contains(
                context.Request.Method,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            requestBody = SensitiveFieldRedactor.Redact(requestBody);
        }

        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        // Capture query string if enabled
        string? queryString = null;
        if (auditSettings.CaptureQueryStrings)
        {
            queryString = context.Request.QueryString.Value;
        }

        // Capture user agent if enabled
        string? userAgent = null;
        if (auditSettings.CaptureUserAgent)
        {
            userAgent = context.Request.Headers.UserAgent.ToString();
        }

        var entry = new AuditEntry
        {
            CorrelationId = auditContext?.CorrelationId ?? Guid.NewGuid(),
            Source = AuditSource.Http,
            Timestamp = DateTimeOffset.UtcNow,
            UserId = auditContext?.UserId ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = auditContext?.UserName ?? context.User.FindFirstValue(ClaimTypes.Name),
            IpAddress = auditContext?.IpAddress ?? context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = userAgent,
            HttpMethod = context.Request.Method,
            Path = path,
            QueryString = queryString,
            StatusCode = context.Response.StatusCode,
            DurationMs = stopwatch.ElapsedMilliseconds,
            RequestBody = requestBody,
        };

        channel.Enqueue(entry);
    }

    private static async Task<AuditRequestSettings> LoadSettingsAsync(
        IServiceProvider services,
        IFusionCache cache
    )
    {
        return await cache.GetOrSetAsync<AuditRequestSettings>(
            AuditCacheKeys.RequestConfig,
            async (_, _) =>
            {
                var settings = services.GetService<ISettingsContracts>();
                return settings is null ? FallbackSettings : await BuildSettingsAsync(settings);
            },
            AuditConfigCacheOptions
        );
    }

    private static async Task<AuditRequestSettings> BuildSettingsAsync(ISettingsContracts settings)
    {
        var (captureHttp, captureBody, captureQs, captureUa, excludedPathsRaw) =
            await LoadAllSettingsAsync(settings);

        var excludedPaths = ParseExcludedPaths(excludedPathsRaw);

        return new AuditRequestSettings(
            CaptureHttp: captureHttp,
            CaptureRequestBodies: captureBody,
            CaptureQueryStrings: captureQs,
            CaptureUserAgent: captureUa,
            ExcludedPaths: excludedPaths
        );
    }

    private static async Task<(bool, bool, bool, bool, string?)> LoadAllSettingsAsync(
        ISettingsContracts settings
    )
    {
        var results = await Task.WhenAll(
            settings.GetSettingAsync("auditlogs.capture.http", Core.Settings.SettingScope.System),
            settings.GetSettingAsync(
                "auditlogs.capture.requestbodies",
                Core.Settings.SettingScope.System
            ),
            settings.GetSettingAsync(
                "auditlogs.capture.querystrings",
                Core.Settings.SettingScope.System
            ),
            settings.GetSettingAsync(
                "auditlogs.capture.useragent",
                Core.Settings.SettingScope.System
            ),
            settings.GetSettingAsync("auditlogs.excluded.paths", Core.Settings.SettingScope.System)
        );

        var captureHttp = !string.Equals(results[0], "false", StringComparison.OrdinalIgnoreCase);

        var captureBody = !string.Equals(results[1], "false", StringComparison.OrdinalIgnoreCase);

        var captureQs = !string.Equals(results[2], "false", StringComparison.OrdinalIgnoreCase);

        var captureUa = string.Equals(results[3], "true", StringComparison.OrdinalIgnoreCase);

        return (captureHttp, captureBody, captureQs, captureUa, results[4]);
    }

    private static string[] ParseExcludedPaths(string? rawPaths)
    {
        if (string.IsNullOrWhiteSpace(rawPaths))
        {
            return DefaultExcludedPaths;
        }

        var configured = rawPaths.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        if (configured.Length == 0)
        {
            return DefaultExcludedPaths;
        }

        var merged = new string[DefaultExcludedPaths.Length + configured.Length];
        DefaultExcludedPaths.CopyTo(merged, 0);
        configured.CopyTo(merged, DefaultExcludedPaths.Length);
        return merged;
    }

    private static bool IsExcludedPath(string path, string[] configuredPaths)
    {
        foreach (var prefix in configuredPaths)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
