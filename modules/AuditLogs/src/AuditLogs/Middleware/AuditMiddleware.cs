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

    public async Task InvokeAsync(HttpContext context)
    {
        var channel = context.RequestServices.GetService<AuditChannel>();
        if (channel is null)
        {
            await next(context);
            return;
        }

        // Check if HTTP capture is enabled (default: true when no setting exists)
        var settings = context.RequestServices.GetService<ISettingsContracts>();
        if (settings is not null)
        {
            var raw = await settings.GetSettingAsync(
                "auditlogs.capture.http",
                Core.Settings.SettingScope.System
            );
            if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }
        }

        // Check excluded paths
        var path = context.Request.Path.Value ?? "";
        if (IsExcludedPath(path))
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

        // Read request body if applicable (default: true)
        string? requestBody = null;
        var captureBody =
            settings is null
            || !string.Equals(
                await settings.GetSettingAsync(
                    "auditlogs.capture.requestbodies",
                    Core.Settings.SettingScope.System
                ),
                "false",
                StringComparison.OrdinalIgnoreCase
            );

        if (
            captureBody
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

        // Capture query string setting (default: true)
        string? queryString = null;
        var captureQs =
            settings is null
            || !string.Equals(
                await settings.GetSettingAsync(
                    "auditlogs.capture.querystrings",
                    Core.Settings.SettingScope.System
                ),
                "false",
                StringComparison.OrdinalIgnoreCase
            );
        if (captureQs)
        {
            queryString = context.Request.QueryString.Value;
        }

        // Capture user agent setting (default: false)
        string? userAgent = null;
        if (settings is not null)
        {
            var captureUa = await settings.GetSettingAsync(
                "auditlogs.capture.useragent",
                Core.Settings.SettingScope.System
            );
            if (string.Equals(captureUa, "true", StringComparison.OrdinalIgnoreCase))
            {
                userAgent = context.Request.Headers.UserAgent.ToString();
            }
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

    private static bool IsExcludedPath(string path)
    {
        ReadOnlySpan<string> defaults =
        [
            "/health",
            "/metrics",
            "/_content",
            "/js/",
            "/css/",
            "/favicon",
        ];
        foreach (var prefix in defaults)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
