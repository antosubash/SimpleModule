using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.Enrichment;
using SimpleModule.AuditLogs.Pipeline;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.AuditLogs.Middleware;

public sealed class AuditMiddleware(RequestDelegate next)
{
    private static readonly string[] ExcludedMethodsForBody = ["GET", "HEAD", "OPTIONS"];

    private sealed record AuditRequestSettings(
        bool CaptureHttp,
        bool CaptureRequestBodies,
        bool CaptureQueryStrings,
        bool CaptureUserAgent,
        IReadOnlyList<string> ExcludedPaths
    );

    public async Task InvokeAsync(HttpContext context)
    {
        var channel = context.RequestServices.GetService<AuditChannel>();
        if (channel is null)
        {
            await next(context);
            return;
        }

        // Load all settings once at the start (parallel batch loading)
        var settings = context.RequestServices.GetService<ISettingsContracts>();
        var auditSettings = await LoadSettingsAsync(settings);

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

    private static async Task<AuditRequestSettings> LoadSettingsAsync(ISettingsContracts? settings)
    {
        if (settings is null)
        {
            return new AuditRequestSettings(
                CaptureHttp: true,
                CaptureRequestBodies: true,
                CaptureQueryStrings: true,
                CaptureUserAgent: false,
                ExcludedPaths: GetDefaultExcludedPaths()
            );
        }

        // Fetch all settings in parallel
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

    private static List<string> ParseExcludedPaths(string? rawPaths)
    {
        var paths = GetDefaultExcludedPaths();

        if (string.IsNullOrWhiteSpace(rawPaths))
        {
            return paths;
        }

        var configured = rawPaths
            .Split(',')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        paths.AddRange(configured);
        return paths;
    }

    private static List<string> GetDefaultExcludedPaths() =>
        ["/health", "/metrics", "/_content", "/js/", "/css/", "/favicon"];

    private static bool IsExcludedPath(string path, IEnumerable<string> configuredPaths)
    {
        foreach (var prefix in configuredPaths)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
