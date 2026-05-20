using SimpleModule.Core;

namespace SimpleModule.BackgroundJobs.Contracts;

/// <summary>
/// Single-leader lease row used by <c>OnOneServer</c>. Hosts call
/// <c>IInstanceLeader.TryAcquireAsync</c>; only the holder runs the tick.
/// </summary>
[NoDtoGeneration]
public class JobLease
{
    public string Name { get; set; } = string.Empty;
    public string OwnerWorkerId { get; set; } = string.Empty;
    public DateTimeOffset AcquiredAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
