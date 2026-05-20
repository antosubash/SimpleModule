using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.Notifications.Tests.Unit;

internal sealed class TestBackgroundJobs : IBackgroundJobs
{
    public List<(Type JobType, object? Data)> EnqueuedJobs { get; } = [];

    public Task<JobId> EnqueueAsync<TJob>(object? data = null, CancellationToken ct = default)
        where TJob : IModuleJob
    {
        EnqueuedJobs.Add((typeof(TJob), data));
        return Task.FromResult(JobId.From(Guid.NewGuid()));
    }

    public Task<JobId> ScheduleAsync<TJob>(
        DateTimeOffset executeAt,
        object? data = null,
        CancellationToken ct = default
    )
        where TJob : IModuleJob => Task.FromResult(JobId.From(Guid.NewGuid()));

    public Task<RecurringJobId> AddRecurringAsync<TJob>(
        string name,
        string cronExpression,
        object? data = null,
        CancellationToken ct = default
    )
        where TJob : IModuleJob => Task.FromResult(RecurringJobId.From(Guid.NewGuid()));

    public Task RemoveRecurringAsync(RecurringJobId id, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<bool> ToggleRecurringAsync(RecurringJobId id, CancellationToken ct = default) =>
        Task.FromResult(true);

    public Task CancelAsync(JobId jobId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<JobStatusDto?> GetStatusAsync(JobId jobId, CancellationToken ct = default) =>
        Task.FromResult<JobStatusDto?>(null);
}
