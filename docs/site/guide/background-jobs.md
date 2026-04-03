---
outline: deep
---

# Background Jobs

SimpleModule provides a background job system built on [TickerQ](https://github.com/nickofc/TickerQ) for scheduling and executing long-running tasks outside the HTTP request pipeline. Jobs support progress reporting, structured logging, retries, and CRON-based recurring schedules.

## Defining a Job

Implement `IModuleJob` and register it in your module:

```csharp
public class GenerateReportJob : IModuleJob
{
    public async Task ExecuteAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var data = context.GetData<ReportRequest>();
        context.ReportProgress(0, "Starting report generation");

        // Do work...
        context.Log("Processing 500 records");
        context.ReportProgress(50, "Halfway done");

        // More work...
        context.ReportProgress(100, "Report complete");
    }
}
```

Register the job in your module's `ConfigureServices`:

```csharp
services.AddModuleJob<GenerateReportJob>();
```

## Scheduling Jobs

Inject `IBackgroundJobs` to enqueue, schedule, or create recurring jobs:

### Immediate Execution

```csharp
var jobId = await backgroundJobs.EnqueueAsync<GenerateReportJob>(
    new ReportRequest { Type = "monthly" });
```

### Delayed Execution

```csharp
var jobId = await backgroundJobs.ScheduleAsync<GenerateReportJob>(
    executeAt: DateTimeOffset.UtcNow.AddHours(1),
    data: new ReportRequest { Type = "monthly" });
```

### Recurring Jobs (CRON)

```csharp
var recurringId = await backgroundJobs.AddRecurringAsync<GenerateReportJob>(
    name: "Monthly Report",
    cronExpression: "0 0 1 * *",  // 1st of every month at midnight
    data: new ReportRequest { Type = "monthly" });
```

CRON expressions use the standard 5-field format: `minute hour day month dayOfWeek`.

### Managing Jobs

```csharp
// Cancel a running or pending job
await backgroundJobs.CancelAsync(jobId);

// Check job status
var status = await backgroundJobs.GetStatusAsync(jobId);

// Toggle a recurring job on/off
await backgroundJobs.ToggleRecurringAsync(recurringId);

// Remove a recurring job
await backgroundJobs.RemoveRecurringAsync(recurringId);
```

## Job Execution Context

During execution, `IJobExecutionContext` provides:

| Method | Purpose |
|--------|---------|
| `GetData<T>()` | Deserialize the job's input data |
| `ReportProgress(percentage, message?)` | Update progress (0-100) with optional message |
| `Log(message)` | Add a timestamped log entry |
| `JobId` | The unique identifier for this execution |

Progress updates are batched and flushed to the database periodically (configurable via `ProgressFlushInterval`).

## Job States

| State | Description |
|-------|-------------|
| `Pending` | Queued, waiting to execute |
| `Running` | Currently executing |
| `Completed` | Finished successfully |
| `Failed` | Execution threw an exception |
| `Cancelled` | Cancelled by user or system |
| `Skipped` | Skipped (e.g., overlapping recurring run) |

## API Endpoints

All endpoints require `BackgroundJobs.ViewJobs` or `BackgroundJobs.ManageJobs` permissions.

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/jobs/` | List jobs with optional state/type filters |
| `GET` | `/api/jobs/{id}` | Job detail with logs and progress |
| `POST` | `/api/jobs/{id}/cancel` | Cancel a pending or running job |
| `POST` | `/api/jobs/{id}/retry` | Retry a failed job |
| `GET` | `/api/jobs/recurring` | List all recurring jobs |
| `POST` | `/api/jobs/recurring/{id}/toggle` | Enable/disable a recurring job |
| `DELETE` | `/api/jobs/recurring/{id}` | Delete a recurring job |

## Admin UI

The module includes four admin pages at `/admin/jobs`:

- **Dashboard** -- overview with active, failed, and recurring job counts
- **Job List** -- paginated, filterable list of all jobs with state and progress
- **Job Detail** -- real-time progress bar, logs, error messages, and retry/cancel actions (auto-refreshes every 2 seconds while running)
- **Recurring Jobs** -- manage recurring schedules with toggle and delete actions

## Configuration

```csharp
public class BackgroundJobsModuleOptions : IModuleOptions
{
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public int ProgressFlushBatchSize { get; set; } = 50;
    public TimeSpan ProgressFlushInterval { get; set; } = TimeSpan.FromSeconds(2);
    public int MaxLogEntries { get; set; } = 1000;
}
```

## Contract Interface

Query job data from other modules via `IBackgroundJobsContracts`:

```csharp
public interface IBackgroundJobsContracts
{
    Task<PagedResult<JobSummaryDto>> GetJobsAsync(JobFilter filter, CancellationToken ct);
    Task<JobDetailDto?> GetJobDetailAsync(JobId id, CancellationToken ct);
    Task<IReadOnlyList<RecurringJobDto>> GetRecurringJobsAsync(CancellationToken ct);
    Task RetryAsync(JobId id, CancellationToken ct);
}
```

## Next Steps

- [Modules](/guide/modules) -- module structure and service registration
- [Permissions](/guide/permissions) -- protecting job management endpoints
- [Events](/guide/events) -- triggering jobs from event handlers
