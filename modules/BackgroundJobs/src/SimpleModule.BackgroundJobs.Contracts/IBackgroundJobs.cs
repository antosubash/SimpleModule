namespace SimpleModule.BackgroundJobs.Contracts;

public interface IBackgroundJobs
{
    Task<JobId> EnqueueAsync<TJob>(object? data = null, CancellationToken ct = default)
        where TJob : IModuleJob;

    Task<JobId> ScheduleAsync<TJob>(
        DateTimeOffset executeAt,
        object? data = null,
        CancellationToken ct = default
    )
        where TJob : IModuleJob;

    Task<RecurringJobId> AddRecurringAsync<TJob>(
        string name,
        string cronExpression,
        object? data = null,
        CancellationToken ct = default
    )
        where TJob : IModuleJob;

    Task RemoveRecurringAsync(RecurringJobId id, CancellationToken ct = default);

    Task<bool> ToggleRecurringAsync(RecurringJobId id, CancellationToken ct = default);

    Task CancelAsync(JobId jobId, CancellationToken ct = default);

    Task<JobStatusDto?> GetStatusAsync(JobId jobId, CancellationToken ct = default);
}
