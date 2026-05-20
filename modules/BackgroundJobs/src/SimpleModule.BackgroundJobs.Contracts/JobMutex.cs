using SimpleModule.Core;

namespace SimpleModule.BackgroundJobs.Contracts;

/// <summary>
/// Per-job mutex row supporting <c>WithoutOverlapping</c>. Acquired by the scheduler
/// before enqueueing and released by the worker after execution finishes.
/// </summary>
[NoDtoGeneration]
public class JobMutex
{
    public string Name { get; set; } = string.Empty;
    public string OwnerWorkerId { get; set; } = string.Empty;
    public DateTimeOffset AcquiredAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
