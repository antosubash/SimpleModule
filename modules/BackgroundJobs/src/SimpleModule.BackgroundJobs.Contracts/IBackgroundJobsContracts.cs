using SimpleModule.Core;

namespace SimpleModule.BackgroundJobs.Contracts;

public interface IBackgroundJobsContracts
{
    Task<PagedResult<JobSummaryDto>> GetJobsAsync(JobFilter filter, CancellationToken ct = default);

    Task<JobDetailDto?> GetJobDetailAsync(JobId id, CancellationToken ct = default);

    Task<IReadOnlyList<RecurringJobDto>> GetRecurringJobsAsync(CancellationToken ct = default);

    Task<int> GetRecurringCountAsync(CancellationToken ct = default);

    Task RetryAsync(JobId id, CancellationToken ct = default);
}
