---
outline: deep
---

# Background Jobs

SimpleModule provides a background job system for scheduling and executing long-running tasks outside the HTTP request pipeline. It is built in-house on top of a database-backed queue (`DatabaseJobQueue`), a worker hosted service (`JobProcessorService`), a stalled-job sweeper (`StalledJobSweeperService`), and [Cronos](https://github.com/HangfireIO/Cronos) for CRON expression parsing. Jobs support progress reporting, structured logging, retries, and CRON-based recurring schedules.

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

The module mounts its endpoints under `RoutePrefix = "/api/jobs"`. Route IDs are `guid`-constrained.

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/jobs/` | List jobs with optional state/type filters |
| `GET` | `/api/jobs/{id:guid}` | Job detail with logs and progress |
| `POST` | `/api/jobs/{id:guid}/cancel` | Cancel a pending or running job |
| `POST` | `/api/jobs/{id:guid}/retry` | Retry a failed job |
| `GET` | `/api/jobs/recurring` | List all recurring jobs |
| `POST` | `/api/jobs/recurring/{id:guid}/toggle` | Enable/disable a recurring job |
| `DELETE` | `/api/jobs/recurring/{id:guid}` | Delete a recurring job |

## Admin UI

The module includes four admin pages at `/admin/jobs`:

- **Dashboard** -- overview with active, failed, and recurring job counts
- **Job List** -- paginated, filterable list of all jobs with state and progress
- **Job Detail** -- real-time progress bar, logs, error messages, and retry/cancel actions (auto-refreshes every 2 seconds while running)
- **Recurring Jobs** -- manage recurring schedules with toggle and delete actions

## Configuration

```csharp
public enum BackgroundJobsWorkerMode
{
    Producer = 0,
    Consumer = 1,
}

public class BackgroundJobsModuleOptions : IModuleOptions
{
    public BackgroundJobsWorkerMode WorkerMode { get; set; } = BackgroundJobsWorkerMode.Producer;
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public int ProgressFlushBatchSize { get; set; } = 50;
    public TimeSpan ProgressFlushInterval { get; set; } = TimeSpan.FromSeconds(2);
    public int MaxLogEntries { get; set; } = 1000;
}
```

### Worker Mode and Split Deployments

`WorkerMode` controls whether this host actually executes jobs. This matters for split deployments where the web tier enqueues work but a separate worker tier processes it.

- **`Producer`** (default) — the host can enqueue, schedule, and query jobs, but does **not** run `JobProcessorService` or `StalledJobSweeperService`. Use this for web-only instances.
- **`Consumer`** — the host registers the worker identity and runs `JobProcessorService` + `StalledJobSweeperService`, picking up and executing queued jobs. Use this for dedicated worker processes.

`ProgressFlushService` runs in both modes so that any host owning the module can flush queued progress updates.

## Contract Interface

Query job data from other modules via `IBackgroundJobsContracts`:

```csharp
public interface IBackgroundJobsContracts
{
    Task<PagedResult<JobSummaryDto>> GetJobsAsync(JobFilter filter, CancellationToken ct = default);
    Task<JobDetailDto?> GetJobDetailAsync(JobId id, CancellationToken ct = default);
    Task<IReadOnlyList<RecurringJobDto>> GetRecurringJobsAsync(CancellationToken ct = default);
    Task<int> GetRecurringCountAsync(CancellationToken ct = default);
    Task RetryAsync(JobId id, CancellationToken ct = default);
}
```

## Next Steps

- [Modules](/guide/modules) -- module structure and service registration
- [Permissions](/guide/permissions) -- protecting job management endpoints
- [Events](/guide/events) -- triggering jobs from event handlers
