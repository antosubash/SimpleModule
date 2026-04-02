# Background Jobs Module — Design Spec

## Context

SimpleModule currently has no general-purpose background job system. Existing async work uses ad-hoc `BackgroundService` + `Channel<T>` patterns (audit writer, event dispatcher, seed services). Modules that need long-running tasks (data imports, report generation, bulk updates) have no standard way to enqueue, track, retry, or manage these jobs.

This design introduces a **BackgroundJobs module** backed by **TickerQ** — a source-generator-based .NET job scheduler with EF Core persistence. The framework layer provides clean abstractions so modules never depend on TickerQ directly.

## Architecture

**Hybrid approach:**

- **Framework layer** (`SimpleModule.Core/Jobs/`): Interfaces (`IBackgroundJobs`, `IModuleJob`, `IJobExecutionContext`) that modules code against. Zero dependency on TickerQ.
- **Module layer** (`modules/BackgroundJobs/`): TickerQ integration, progress tracking extension (TickerQ lacks progress reporting), and React admin UI via Inertia.

```
Module (e.g., Products)
  │  depends on
  ▼
SimpleModule.Core  (IBackgroundJobs, IModuleJob)
  │  implemented by
  ▼
BackgroundJobs Module  (TickerQ + progress table + admin UI)
  │  uses
  ▼
TickerQ  (job scheduling, persistence, retry, concurrency)
```

## Framework Interfaces (`SimpleModule.Core/Jobs/`)

### IBackgroundJobs

The primary contract for enqueuing and managing jobs. Registered in DI, injectable by any module endpoint or service.

```csharp
public interface IBackgroundJobs
{
    /// Enqueue a job for immediate execution.
    Task<JobId> EnqueueAsync<TJob>(object? data = null, CancellationToken ct = default)
        where TJob : IModuleJob;

    /// Schedule a job for future execution.
    Task<JobId> ScheduleAsync<TJob>(DateTimeOffset executeAt, object? data = null, CancellationToken ct = default)
        where TJob : IModuleJob;

    /// Add or update a recurring job with a cron expression.
    Task<RecurringJobId> AddRecurringAsync<TJob>(string name, string cronExpression, object? data = null, CancellationToken ct = default)
        where TJob : IModuleJob;

    /// Remove a recurring job.
    Task RemoveRecurringAsync(RecurringJobId id, CancellationToken ct = default);

    /// Cancel a running or pending job.
    Task CancelAsync(JobId jobId, CancellationToken ct = default);

    /// Get current status of a job.
    Task<JobStatusDto?> GetStatusAsync(JobId jobId, CancellationToken ct = default);
}
```

### IModuleJob

What modules implement to define a job. Resolved from DI — constructor injection works.

```csharp
public interface IModuleJob
{
    Task ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken);
}
```

### IJobExecutionContext

Passed to running jobs. Provides access to input data and progress reporting.

```csharp
public interface IJobExecutionContext
{
    JobId JobId { get; }
    T GetData<T>();
    void ReportProgress(int percentage, string? message = null);
    void Log(string message);
}
```

### Value Objects & DTOs

```csharp
[Vogen.ValueObject<Guid>]
public partial struct JobId { }

[Vogen.ValueObject<Guid>]
public partial struct RecurringJobId { }

public enum JobState { Pending, Running, Completed, Failed, Cancelled, Skipped }

public record JobStatusDto(
    JobId Id,
    string JobType,
    JobState State,
    int ProgressPercentage,
    string? ProgressMessage,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int RetryCount
);
```

## BackgroundJobs Module

### Module Registration

```csharp
[Module("BackgroundJobs", RoutePrefix = "/api/jobs", ViewPrefix = "/admin/jobs")]
public class BackgroundJobsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // TickerQ setup
        services.AddTickerQ(options =>
        {
            options.SetMaxConcurrency(Environment.ProcessorCount);
            options.SetExceptionHandler<JobExceptionHandler>();
            options.AddOperationalStore<TimeTickerEntity, CronTickerEntity>(ef =>
            {
                ef.UseApplicationDbContext<BackgroundJobsDbContext>(
                    ConfigurationType.UseModelCustomizer);
            });
        });

        // Module services
        services.AddScoped<IBackgroundJobs, BackgroundJobsService>();
        services.AddModuleDbContext<BackgroundJobsDbContext>(configuration, "BackgroundJobs");
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<BackgroundJobsPermissions>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(new MenuItem
        {
            Label = "Background Jobs",
            Url = "/admin/jobs",
            Icon = "...",
            Order = 95,
            Section = MenuSection.AdminSidebar,
        });
    }
}
```

### Database Schema

**Uses TickerQ's built-in tables** (managed by TickerQ's EF Core configurations):

| Table | Source | Purpose |
|---|---|---|
| `ticker.TimeTickers` | TickerQ | One-off and scheduled jobs |
| `ticker.CronTickers` | TickerQ | Recurring job definitions |
| `ticker.CronTickerOccurrences` | TickerQ | Individual runs of recurring jobs |

**Adds one extension table** (in BackgroundJobs module's DbContext):

#### `JobProgress` table

Fills the gap TickerQ doesn't cover — progress reporting for long-running tasks.

| Column | Type | Purpose |
|---|---|---|
| `Id` | Guid PK | = TickerQ's TimeTicker/Occurrence ID (shared key) |
| `JobTypeName` | string | Fully qualified IModuleJob type name |
| `ModuleName` | string | Owning module name |
| `ProgressPercentage` | int | 0–100 |
| `ProgressMessage` | string? | Human-readable status text |
| `Data` | string? (JSON) | Serialized input data |
| `Logs` | string? (JSON) | Array of log entries with timestamps |
| `UpdatedAt` | DateTimeOffset | Last progress update |

This table uses the **same primary key** as the TickerQ entity it tracks. No FK constraint (to avoid coupling EF models), but the IDs match 1:1.

### TickerQ Bridge — JobExecutionBridge

TickerQ discovers jobs via `[TickerFunction]` attributes. We use a single dispatcher function that resolves the actual `IModuleJob` from DI:

```csharp
public class JobExecutionBridge(IServiceProvider serviceProvider)
{
    [TickerFunction("ModuleJobDispatcher")]
    public async Task Execute(TickerFunctionContext<JobDispatchPayload> context, CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var jobType = Type.GetType(context.Request.JobTypeName)
            ?? throw new InvalidOperationException($"Job type not found: {context.Request.JobTypeName}");

        var job = (IModuleJob)provider.GetRequiredService(jobType);
        var tracker = provider.GetRequiredService<ProgressTracker>();
        var executionContext = new JobExecutionContext(
            JobId.From(context.Id), context.Request, tracker);

        try
        {
            executionContext.ReportProgress(0, "Starting");
            await job.ExecuteAsync(executionContext, ct);
            executionContext.ReportProgress(100, "Completed");
        }
        catch
        {
            await tracker.FlushAsync(executionContext); // persist last known progress
            throw;
        }
    }
}

public record JobDispatchPayload(string JobTypeName, string? SerializedData);
```

### ProgressTracker

Batches progress updates and periodically flushes to the `JobProgress` table to avoid excessive DB writes during tight loops.

```csharp
public class ProgressTracker(BackgroundJobsDbContext db)
{
    private readonly ConcurrentDictionary<JobId, ProgressEntry> _pending = new();

    public void Update(JobId id, int percentage, string? message)
    {
        _pending.AddOrUpdate(id,
            new ProgressEntry(percentage, message, DateTimeOffset.UtcNow),
            (_, _) => new ProgressEntry(percentage, message, DateTimeOffset.UtcNow));
    }

    public void AddLog(JobId id, string message) { /* append to log buffer */ }

    public async Task FlushAsync(JobExecutionContext context)
    {
        // Upsert to JobProgress table
    }
}
```

Flushing strategy: flush every ~2 seconds or on completion/failure — not on every `ReportProgress` call.

### BackgroundJobsService (implements IBackgroundJobs)

Wraps TickerQ's `ITimeTickerManager` and `ICronTickerManager`:

- `EnqueueAsync<TJob>()` → creates a `TimeTickerEntity` with `ExecutionTime = DateTime.UtcNow` and payload containing the job type name + serialized data. Also creates a `JobProgress` row.
- `ScheduleAsync<TJob>()` → same but with future `ExecutionTime`.
- `AddRecurringAsync<TJob>()` → creates/updates a `CronTickerEntity` with the cron expression and payload.
- `CancelAsync()` → uses TickerQ's cancellation mechanism.
- `GetStatusAsync()` → queries TickerQ entity for state + `JobProgress` table for progress.

### Job Registration

Modules register their `IModuleJob` implementations in DI during `ConfigureServices`. The source generator could optionally auto-discover `IModuleJob` implementors, but for v1 we use explicit registration:

```csharp
// In Products module
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<ImportProductsJob>();
    // ...
}
```

### Contracts (`BackgroundJobs.Contracts`)

```csharp
public interface IBackgroundJobsContracts
{
    Task<PagedResult<JobSummaryDto>> GetJobsAsync(JobFilter filter, CancellationToken ct = default);
    Task<JobDetailDto?> GetJobDetailAsync(JobId id, CancellationToken ct = default);
    Task<IReadOnlyList<RecurringJobDto>> GetRecurringJobsAsync(CancellationToken ct = default);
    Task RetryAsync(JobId id, CancellationToken ct = default);
}

public sealed class BackgroundJobsPermissions : IModulePermissions
{
    public const string ViewJobs = "BackgroundJobs.ViewJobs";
    public const string ManageJobs = "BackgroundJobs.ManageJobs";
}
```

### Admin UI Pages (React + Inertia)

| Route | Page Component | Description |
|---|---|---|
| `/admin/jobs` | `BackgroundJobs/Dashboard` | Active jobs count, recent failures, upcoming scheduled. Cards with summary stats. |
| `/admin/jobs/list` | `BackgroundJobs/List` | Filterable/sortable table: job type, state, progress bar, timestamps. Polling refresh. |
| `/admin/jobs/{id}` | `BackgroundJobs/Detail` | Single job: progress bar, log entries, input data, error details. Cancel/retry buttons. |
| `/admin/jobs/recurring` | `BackgroundJobs/Recurring` | Recurring job definitions: name, cron expression, enabled toggle, last/next run, edit/delete. |

**Pages registry** (`Pages/index.ts`):
```typescript
export const pages: Record<string, any> = {
    "BackgroundJobs/Dashboard": () => import("../Views/Dashboard"),
    "BackgroundJobs/List": () => import("../Views/List"),
    "BackgroundJobs/Detail": () => import("../Views/Detail"),
    "BackgroundJobs/Recurring": () => import("../Views/Recurring"),
};
```

**Progress display:** The List and Detail pages poll `/api/jobs/{id}/status` every 2 seconds for running jobs. No SignalR for v1 — polling is simpler and sufficient for an admin UI.

### API Endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/jobs` | List jobs (paged, filterable by state/type) |
| GET | `/api/jobs/{id}` | Get job detail with progress |
| POST | `/api/jobs/{id}/cancel` | Cancel a running/pending job |
| POST | `/api/jobs/{id}/retry` | Retry a failed job |
| GET | `/api/jobs/recurring` | List recurring job definitions |
| POST | `/api/jobs/recurring/{id}/toggle` | Enable/disable a recurring job |
| DELETE | `/api/jobs/recurring/{id}` | Remove a recurring job |

All endpoints require `BackgroundJobs.ViewJobs` or `BackgroundJobs.ManageJobs` permission.

### View Endpoints

| Method | Route | Renders |
|---|---|---|
| GET | `/admin/jobs` | `BackgroundJobs/Dashboard` |
| GET | `/admin/jobs/list` | `BackgroundJobs/List` |
| GET | `/admin/jobs/{id}` | `BackgroundJobs/Detail` |
| GET | `/admin/jobs/recurring` | `BackgroundJobs/Recurring` |

## Module File Structure

```
modules/BackgroundJobs/
├── src/
│   ├── SimpleModule.BackgroundJobs.Contracts/
│   │   ├── SimpleModule.BackgroundJobs.Contracts.csproj
│   │   ├── IBackgroundJobsContracts.cs
│   │   ├── BackgroundJobsPermissions.cs
│   │   ├── JobSummaryDto.cs
│   │   ├── JobDetailDto.cs
│   │   └── RecurringJobDto.cs
│   └── SimpleModule.BackgroundJobs/
│       ├── SimpleModule.BackgroundJobs.csproj
│       ├── BackgroundJobsModule.cs
│       ├── BackgroundJobsDbContext.cs
│       ├── BackgroundJobsConstants.cs
│       ├── package.json
│       ├── vite.config.ts
│       ├── tsconfig.json
│       ├── Services/
│       │   ├── BackgroundJobsService.cs       (IBackgroundJobs impl)
│       │   ├── BackgroundJobsContractsService.cs (IBackgroundJobsContracts impl)
│       │   ├── JobExecutionBridge.cs           (TickerQ dispatcher)
│       │   ├── ProgressTracker.cs              (progress batching)
│       │   └── JobExceptionHandler.cs          (ITickerExceptionHandler)
│       ├── Entities/
│       │   └── JobProgress.cs
│       ├── Endpoints/
│       │   └── Jobs/
│       │       ├── GetAllEndpoint.cs
│       │       ├── GetByIdEndpoint.cs
│       │       ├── CancelEndpoint.cs
│       │       ├── RetryEndpoint.cs
│       │       ├── GetRecurringEndpoint.cs
│       │       ├── ToggleRecurringEndpoint.cs
│       │       └── DeleteRecurringEndpoint.cs
│       ├── Views/
│       │   ├── DashboardEndpoint.cs
│       │   ├── ListEndpoint.cs
│       │   ├── DetailEndpoint.cs
│       │   └── RecurringEndpoint.cs
│       └── Pages/
│           ├── index.ts
│           └── Views/
│               ├── Dashboard.tsx
│               ├── List.tsx
│               ├── Detail.tsx
│               └── Recurring.tsx
├── tests/
│   └── SimpleModule.BackgroundJobs.Tests/
│       ├── SimpleModule.BackgroundJobs.Tests.csproj
│       ├── BackgroundJobsServiceTests.cs
│       ├── JobExecutionBridgeTests.cs
│       └── ProgressTrackerTests.cs
```

## Usage Example

### Defining a job (in Products module)

```csharp
public class ImportProductsJob(ProductsDbContext db, ILogger<ImportProductsJob> logger) : IModuleJob
{
    public async Task ExecuteAsync(IJobExecutionContext context, CancellationToken ct)
    {
        var data = context.GetData<ImportProductsData>();
        var records = await ParseCsvAsync(data.FileId, ct);

        for (int i = 0; i < records.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await db.Products.AddAsync(records[i], ct);
            context.ReportProgress(
                (i + 1) * 100 / records.Count,
                $"Imported {i + 1} of {records.Count} products");
        }

        await db.SaveChangesAsync(ct);
        context.Log("Import completed successfully");
    }
}

public record ImportProductsData(Guid FileId);
```

### Enqueuing from an endpoint

```csharp
public class ImportEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/import", async (IBackgroundJobs jobs, ImportRequest req) =>
        {
            var jobId = await jobs.EnqueueAsync<ImportProductsJob>(
                new ImportProductsData(req.FileId));
            return Results.Accepted(value: new { jobId = jobId.Value });
        })
        .RequirePermission(ProductsPermissions.Manage);
}
```

### Scheduling a recurring job

```csharp
// In module startup or an admin endpoint
await jobs.AddRecurringAsync<CleanupExpiredProductsJob>(
    name: "cleanup-expired-products",
    cronExpression: "0 2 * * *", // 2am daily
    data: new CleanupConfig { DaysOld = 90 });
```

## NuGet Packages Required

| Package | Purpose |
|---|---|
| `TickerQ` | Core job scheduling engine |
| `TickerQ.EntityFrameworkCore` | EF Core persistence for job state |
| `TickerQ.SourceGenerator` | Compile-time discovery of `[TickerFunction]` |

**Not used:** `TickerQ.Dashboard` (we build our own React UI), `TickerQ.Caching.StackExchangeRedis` (not needed for single-instance).

## Testing Strategy

- **Unit tests:** `BackgroundJobsService` with mocked TickerQ managers, `ProgressTracker` with in-memory DbContext, `JobExecutionBridge` with mock `IModuleJob`.
- **Integration tests:** Use `SimpleModuleWebApplicationFactory` with SQLite. Enqueue a test job, verify it executes and progress is tracked. Test cancel/retry flows.
- **Endpoint tests:** Standard endpoint tests for all API routes using `CreateAuthenticatedClient`.

## Future Considerations (not in v1)

- **SignalR real-time progress** — replace polling with push updates
- **Job chaining** — leverage TickerQ's `ParentId`/`RunCondition` for dependent jobs
- **Distributed locking** — TickerQ supports multi-node via `LockHolder`; enable for horizontal scaling
- **Source generator auto-discovery** — extend SimpleModule.Generator to discover `IModuleJob` implementors and auto-register them in DI
- **Job priority** — expose TickerQ's `TickerTaskPriority` through framework interfaces
- **Dashboard widgets** — allow modules to embed job status in their own pages
