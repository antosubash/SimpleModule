using System.Security.Claims;

namespace SimpleModule.Core.Broadcasting;

/// <summary>
/// Context exposed to <see cref="IBroadcastEvent.Channel"/> and channel
/// authorizers so the same event record can route to a tenant-scoped or
/// user-scoped channel without hard-coding identifiers into the event itself.
/// </summary>
public interface IBroadcastContext
{
    /// <summary>
    /// Connected user, if the broadcast was initiated in an authenticated
    /// scope. Server-published events outside a request typically have a
    /// null principal — the caller is responsible for setting tenant/user
    /// ids on the event itself in that case.
    /// </summary>
    ClaimsPrincipal? User { get; }

    /// <summary>
    /// Active tenant id (e.g., from the request's claims or ambient tenant
    /// resolution). Null in single-tenant deployments.
    /// </summary>
    string? TenantId { get; }
}

public sealed class BroadcastContext(ClaimsPrincipal? user, string? tenantId) : IBroadcastContext
{
    public ClaimsPrincipal? User { get; } = user;
    public string? TenantId { get; } = tenantId;

    public static BroadcastContext Empty { get; } = new(null, null);
}
