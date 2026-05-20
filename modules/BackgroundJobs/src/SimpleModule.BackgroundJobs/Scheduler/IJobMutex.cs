namespace SimpleModule.BackgroundJobs.Scheduler;

public interface IJobMutex
{
    Task<bool> TryAcquireAsync(
        string name,
        string ownerId,
        TimeSpan ttl,
        CancellationToken ct = default
    );

    Task ReleaseAsync(string name, CancellationToken ct = default);
}
