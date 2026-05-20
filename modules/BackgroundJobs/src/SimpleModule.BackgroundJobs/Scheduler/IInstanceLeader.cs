namespace SimpleModule.BackgroundJobs.Scheduler;

public interface IInstanceLeader
{
    /// <summary>
    /// Try to acquire (or renew) a named lease for <paramref name="ownerId"/>.
    /// Returns true when the caller now holds the lease.
    /// </summary>
    Task<bool> TryAcquireAsync(
        string name,
        string ownerId,
        TimeSpan ttl,
        CancellationToken ct = default
    );
}
