namespace SimpleModule.BackgroundJobs.Scheduler;

public sealed class SchedulerOptions
{
    public const string LeaseName = "scheduler";
    public const string ScheduledJobSentinel = "schedule:";
    public const string MutexPrefix = "mutex:";

    /// <summary>How often the scheduler tick runs.</summary>
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Lease TTL for <c>OnOneServer</c> election; should comfortably exceed TickInterval.</summary>
    public TimeSpan LeaseTtl { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Default mutex TTL applied when <c>WithoutOverlapping</c> is set.</summary>
    public TimeSpan MutexTtl { get; set; } = TimeSpan.FromHours(1);
}
