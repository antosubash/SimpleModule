namespace SimpleModule.Core.Maintenance;

/// <summary>
/// Snapshot of maintenance-mode state read by <c>MaintenanceModeMiddleware</c>.
/// <c>SecretHash</c> is the SHA-256 hash (hex) of the bypass secret — never the secret itself.
/// </summary>
public sealed record MaintenanceState
{
    public required bool Active { get; init; }

    public DateTimeOffset? Until { get; init; }

    public string? SecretHash { get; init; }

    public string? Message { get; init; }

    public int RetryAfterSeconds { get; init; } = 60;
}
