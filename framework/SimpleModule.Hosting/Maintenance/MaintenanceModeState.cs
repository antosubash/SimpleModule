namespace SimpleModule.Hosting.Maintenance;

/// <summary>
/// Snapshot of the maintenance-mode sentinel. <c>null</c> from <see cref="IMaintenanceModeStore"/>
/// means the application is up.
/// </summary>
public sealed record MaintenanceModeState
{
    /// <summary>SHA-256 hex of the bypass secret, or <c>null</c> if no bypass is configured.</summary>
    public string? SecretHash { get; init; }

    public string? Message { get; init; }

    public int RetryAfterSeconds { get; init; } = 60;

    /// <summary>Optional UTC timestamp after which the sentinel should be treated as inactive.</summary>
    public DateTimeOffset? Until { get; init; }

    /// <summary>UTC timestamp the sentinel was created at.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
