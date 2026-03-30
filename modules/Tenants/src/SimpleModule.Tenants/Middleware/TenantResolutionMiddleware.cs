using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleModule.Tenants.Contracts;
using SimpleModule.Tenants.Resolvers;

namespace SimpleModule.Tenants.Middleware;

public sealed partial class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger
)
{
    private static readonly HashSet<string> SkipExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css",
        ".js",
        ".mjs",
        ".map",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".svg",
        ".ico",
        ".woff",
        ".woff2",
        ".ttf",
        ".eot",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (ShouldSkip(path))
        {
            await next(context);
            return;
        }

        var tenantContext = context.RequestServices.GetRequiredService<TenantContext>();

        try
        {
            var hostResolver =
                context.RequestServices.GetRequiredService<HostNameTenantResolver>();
            var tenantId = await hostResolver.ResolveAsync(context);
            tenantId ??= HeaderTenantResolver.Resolve(context);
            tenantId ??= ClaimTenantResolver.Resolve(context);

            if (tenantId is not null)
            {
                tenantContext.TenantId = tenantId;
                LogTenantResolved(logger, tenantId, path);
            }
        }
        catch (DbException ex)
        {
            LogResolutionFailed(logger, path, ex);
        }
        catch (InvalidOperationException ex)
        {
            LogResolutionFailed(logger, path, ex);
        }

        await next(context);
    }

    private static bool ShouldSkip(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (path.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var lastDot = path.LastIndexOf('.');
        if (lastDot >= 0)
        {
            var extension = path[lastDot..];
            if (SkipExtensions.Contains(extension))
            {
                return true;
            }
        }

        if (path.StartsWith("/api/tenants", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/tenants", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Tenant {TenantId} resolved for request {Path}"
    )]
    private static partial void LogTenantResolved(
        ILogger logger,
        string tenantId,
        string? path
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Tenant resolution failed for request {Path}"
    )]
    private static partial void LogResolutionFailed(
        ILogger logger,
        string? path,
        Exception exception
    );
}
