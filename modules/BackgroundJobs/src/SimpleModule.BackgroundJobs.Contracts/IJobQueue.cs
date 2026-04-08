namespace SimpleModule.BackgroundJobs.Contracts;

public interface IJobQueue
{
    Task EnqueueAsync(JobQueueEntry entry, CancellationToken ct = default);
    Task<JobQueueEntry?> DequeueAsync(string workerId, CancellationToken ct = default);
    Task CompleteAsync(Guid entryId, CancellationToken ct = default);
    Task FailAsync(Guid entryId, string error, CancellationToken ct = default);
    Task<int> RequeueStalledAsync(TimeSpan timeout, CancellationToken ct = default);
}
