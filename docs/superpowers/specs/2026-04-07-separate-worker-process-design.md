# Separate Worker Process for Background Jobs

**Date:** 2026-04-07
**Status:** Approved — ready for implementation plan

## Summary

Add a standalone .NET worker process (`SimpleModule.Worker`) that can run as multiple independent instances, consuming background jobs from a shared database-backed queue. The existing `IBackgroundJobs` API (used by modules to enqueue work) stays unchanged; only the underlying transport changes. TickerQ is removed and replaced by a new `IJobQueue` abstraction with a PostgreSQL/SQLite implementation. A sample email test endpoint demonstrates end-to-end flow through the existing `SendEmailJob`.

## Goals

- Run job processing in a separate process from the web host
- Scale workers horizontally by launching N instances with zero coordination config
- Keep the producer API (`IBackgroundJobs.EnqueueAsync<TJob>`) unchanged for module authors
- Provide a pluggable transport so the DB-backed queue can later be swapped for Redis/RabbitMQ
- Verify with a sample email test job that flows through the existing Email module

## Non-Goals

- No Redis/RabbitMQ implementation in this work (abstraction only)
- No changes to the existing `IModuleJob`, `IJobExecutionContext`, or job registration API
- No distributed tracing/coordination features beyond claim-based dequeue
- No leader election — all workers are equal consumers

## Architecture Overview

```
┌─────────────────┐         ┌──────────────────────┐         ┌─────────────────┐
│ SimpleModule.   │ enqueue │  JobQueueEntries     │ dequeue │ SimpleModule.   │
│ Host (web)      ├────────▶│  (Postgres/SQLite)   │◀────────┤ Worker × N      │
│                 │         │                      │         │                 │
│ IBackgroundJobs │         │  Claim w/ SKIP LOCKED│         │ JobProcessor    │
└─────────────────┘         └──────────────────────┘         └─────────────────┘
```

- **Web host (producer):** Runs `BackgroundJobsService` which writes to `IJobQueue`. Never executes jobs.
- **Queue:** Database table with claim-based dequeue (`SELECT ... FOR UPDATE SKIP LOCKED` on Postgres).
- **Worker (consumer):** Generic Host console app that loads the same modules as the web host, runs `JobProcessorService`, and executes jobs via the existing `IModuleJob` interface.

## Components

### 1. `IJobQueue` (new, in `SimpleModule.BackgroundJobs.Contracts`)

```csharp
public interface IJobQueue
{
    Task EnqueueAsync(JobQueueEntry entry, CancellationToken ct = default);
    Task<JobQueueEntry?> DequeueAsync(string workerId, CancellationToken ct = default);
    Task CompleteAsync(Guid entryId, CancellationToken ct = default);
    Task FailAsync(Guid entryId, string error, CancellationToken ct = default);
    Task RequeueStalledAsync(TimeSpan timeout, CancellationToken ct = default);
}

public record JobQueueEntry(
    Guid Id,
    string JobTypeName,
    string? SerializedData,
    DateTimeOffset? ScheduledAt,
    string? CronExpression,
    string? RecurringName,
    DateTimeOffset CreatedAt);
```

The interface lives in Contracts so both the web host and worker reference it without pulling in the implementation module.

### 2. Database Schema: `JobQueueEntries`

Added to existing `BackgroundJobsDbContext`.

| Column | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `JobTypeName` | string(500) | AssemblyQualifiedName, resolved via `JobTypeRegistry` |
| `SerializedData` | text | JSON payload |
| `ScheduledAt` | timestamptz | Eligibility time (for both immediate and scheduled) |
| `State` | int | `Pending`, `Claimed`, `Completed`, `Failed` |
| `ClaimedBy` | string(100)? | Worker ID |
| `ClaimedAt` | timestamptz? | For stall detection |
| `AttemptCount` | int | Retry counter, incremented on each claim |
| `Error` | text? | Last failure message |
| `CreatedAt` | timestamptz | |
| `CronExpression` | string(100)? | Non-null for recurring entries |
| `RecurringName` | string(200)? | Stable identifier for recurring jobs |

**Index:** `(State, ScheduledAt)` — primary polling path.

**Claim dequeue (PostgreSQL):**

```sql
UPDATE job_queue_entries
SET state = 'Claimed',
    claimed_by = @workerId,
    claimed_at = NOW(),
    attempt_count = attempt_count + 1
WHERE id = (
    SELECT id FROM job_queue_entries
    WHERE state = 'Pending' AND scheduled_at <= NOW()
    ORDER BY scheduled_at
    LIMIT 1
    FOR UPDATE SKIP LOCKED
)
RETURNING *;
```

**SQLite fallback:** `BEGIN IMMEDIATE` transaction + select-then-update by id. Serialized but acceptable for single-worker local dev.

**Stall recovery:** `StalledJobSweeperService` periodically calls `RequeueStalledAsync(TimeSpan.FromMinutes(5))` which transitions rows where `State=Claimed AND claimed_at < NOW() - timeout` back to `Pending`. Runs on every worker instance; the claim update is atomic so duplicate sweeps are harmless.

**Recurring jobs:** On successful completion of an entry where `CronExpression != null`, the worker computes the next fire time via `Cronos` and inserts a new `Pending` row with the same `RecurringName`, `CronExpression`, and `SerializedData`. This keeps recurring scheduling simple and consistent with one-shot jobs.

### 3. Web Host Changes (Producer)

- **`BackgroundJobsService`** is rewritten to write to `IJobQueue` instead of TickerQ's `ITimeTickerManager` / `ICronTickerManager`. The public `IBackgroundJobs` interface and all its methods stay identical.
- **TickerQ removed** from `BackgroundJobsModule` entirely. No more `AddTickerQ`, `UseTickerQ`, `JobExecutionBridge`, `TimeTickerEntity`, or `CronTickerEntity`.
- **`BackgroundJobsModuleOptions`** gains a `WorkerMode` property: `Producer` (default for web host) or `Consumer` (for worker). Producer mode registers `IJobQueue` + `BackgroundJobsService` only. Consumer mode additionally registers `JobProcessorService` and `StalledJobSweeperService`.
- `BackgroundJobsContractsService` (the read-side queries for the admin UI) is updated to read from `JobQueueEntries` instead of `TimeTickers`/`CronTickers`.

### 4. Worker Project: `SimpleModule.Worker`

Location: `template/SimpleModule.Worker/`

**`SimpleModule.Worker.csproj`** uses `Microsoft.NET.Sdk.Worker`. Project references mirror `SimpleModule.Host` exactly — every module project — so the worker has access to all `IModuleJob` implementations across the system. It does NOT reference `SimpleModule.Hosting`'s web-only pieces (Inertia, static files, etc.) — those are gated in the new `AddSimpleModuleWorker` extension.

**`Program.cs`:**

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddSimpleModuleWorker();
await builder.Build().RunAsync();
```

**`AddSimpleModuleWorker()`** (new in `SimpleModule.Hosting`):
- Calls the source-generated `AddModules()` so every module's `ConfigureServices` runs — this gives the worker access to every `IModuleJob`, DbContext, and contract registration
- Sets `BackgroundJobsModuleOptions.WorkerMode = Consumer`
- Registers: `IJobQueue`, `JobProcessorService` (hosted), `StalledJobSweeperService` (hosted), `IEventBus`, EF interceptors, `BackgroundEventDispatcher`, `ProgressChannel`, `ProgressFlushService`
- Skips: endpoint mapping, middleware, Inertia, auth pipeline, antiforgery, CSP, health check HTTP endpoint

**Worker ID:** `$"{Environment.MachineName}-{Environment.ProcessId}-{Guid.NewGuid():N}"[..32]`. Registered as a singleton `WorkerIdentity` record.

**Multiple instances:** Run the worker executable N times. Coordination is handled entirely by the SQL claim — no config needed. For Aspire, add `.WithReplicas(2)` to the worker project registration in `AppHost.cs`.

### 5. `JobProcessorService` (the worker loop)

```csharp
protected override async Task ExecuteAsync(CancellationToken ct)
{
    var semaphore = new SemaphoreSlim(_options.MaxConcurrency);
    while (!ct.IsCancellationRequested)
    {
        await semaphore.WaitAsync(ct);
        var entry = await _queue.DequeueAsync(_workerId, ct);
        if (entry is null)
        {
            semaphore.Release();
            await Task.Delay(_options.PollInterval, ct);
            continue;
        }
        _ = Task.Run(async () =>
        {
            try { await ExecuteJobAsync(entry, ct); }
            finally { semaphore.Release(); }
        }, ct);
    }
}
```

`ExecuteJobAsync` reuses the existing execution path:

1. Create async DI scope
2. Resolve `Type` via existing `JobTypeRegistry`
3. Get job instance from scope, build `DefaultJobExecutionContext`
4. Call `job.ExecuteAsync(context, ct)`
5. On success → `_queue.CompleteAsync(entry.Id)`; if recurring, enqueue next occurrence computed via Cronos
6. On failure → if `AttemptCount < MaxAttempts`, schedule a retry with exponential backoff; otherwise `_queue.FailAsync(entry.Id, ex.Message)`
7. `ProgressChannel` writes are consumed by the existing `ProgressFlushService` running in the same worker process

`IJobExecutionContext`, `DefaultJobExecutionContext`, `ProgressChannel`, `JobTypeRegistry`, and all `IModuleJob` implementations remain unchanged.

**Options (`BackgroundJobsWorkerOptions`):**
- `MaxConcurrency` (default: `Environment.ProcessorCount`)
- `PollInterval` (default: 1 second)
- `StallTimeout` (default: 5 minutes)
- `MaxAttempts` (default: 3)
- `RetryBaseDelay` (default: 10 seconds, exponential)

**Graceful shutdown:** On `StopAsync`, stop dequeuing, wait up to the host shutdown timeout for in-flight jobs to drain via the semaphore. Any job still running when the host is forcibly stopped will have its row requeued by the stall sweeper on the next worker startup cycle.

### 6. Sample Email Test Job

**New endpoint** in the Email module: `POST /api/email/test-send`

```csharp
public sealed class SendTestEmailEndpoint : IEndpoint
{
    // Admin-only via [RequirePermission(EmailPermissions.Send)]
    // Body: { to, subject, body }
    // Handler:
    //   1. Create EmailMessage row (status = Pending) in EmailDbContext
    //   2. backgroundJobs.EnqueueAsync<SendEmailJob>(new SendEmailJobData(message.Id))
    //   3. Return { jobId, messageId }
}
```

**`LogOnlyEmailProvider`:** A new `IEmailProvider` implementation that writes the email to the logger instead of sending via SMTP. Registered in the Email module when `Email:Provider = "Log"` in config. This allows the verification test to run without any SMTP server.

**`SendEmailJob` is unchanged.** It is registered via `AddModuleJob<SendEmailJob>()` in `EmailModule.ConfigureServices`, which the worker also calls (same module wiring via the source generator), so the worker automatically picks it up.

**Manual verification flow:**

```bash
# Terminal 1: web host
dotnet run --project template/SimpleModule.Host

# Terminals 2 & 3: two worker instances
dotnet run --project template/SimpleModule.Worker
dotnet run --project template/SimpleModule.Worker

# Terminal 4: trigger
curl -X POST https://localhost:5001/api/email/test-send \
  -H "Content-Type: application/json" \
  -d '{"to":"test@example.com","subject":"Hi","body":"Hello"}'
```

Expected behavior:
- Web host log: `Enqueued job {id}`
- Exactly **one** of the two workers logs: `Claimed job {id} → SendEmailJob → Sending email ... → Completed`
- The other worker continues polling (proves claim isolation)
- `EmailMessages` row transitions `Pending → Sent`
- `JobQueueEntries` row transitions `Pending → Claimed → Completed`

### 7. Testing

**Unit tests** (`SimpleModule.BackgroundJobs.Tests`):
- `DatabaseJobQueueTests` — enqueue, dequeue, complete, fail, stall requeue, concurrent claim isolation
- Recurring job advances correctly via Cronos

**Integration tests** (`SimpleModule.BackgroundJobs.Tests`):
- `WorkerIntegrationTests.EnqueuedJobExecutes` — spin up `JobProcessorService` in-process with a SQLite `BackgroundJobsDbContext`, enqueue a test `IModuleJob` that sets a `TaskCompletionSource`, assert TCS fires within 5s and queue state = Completed
- `WorkerIntegrationTests.TwoProcessorsDoNotDoubleExecute` — register two `JobProcessorService` instances pointing at the same DB, enqueue 10 jobs, assert all 10 run and none run twice (verifies claim isolation)
- `WorkerIntegrationTests.StalledJobRequeues` — manually mark an entry Claimed with an old `ClaimedAt`, run sweeper, assert entry transitions back to Pending and is picked up
- `EmailModuleTests.TestSendEndpointEnqueuesJob` — hit the test endpoint, assert row inserted into `JobQueueEntries` with correct job type

## Error Handling

- **Job throws:** Caught in `ExecuteJobAsync`. If `AttemptCount < MaxAttempts`, insert a new `Pending` entry with `ScheduledAt = NOW() + backoff` and mark the current one `Failed` with the error. Otherwise mark `Failed` permanently. All transitions logged.
- **Worker crashes mid-job:** Row stays `Claimed`. Stall sweeper on any worker requeues it after `StallTimeout`. Job implementations must be idempotent (existing constraint, documented in `IModuleJob`).
- **Queue DB unreachable:** Producer's `EnqueueAsync` throws (propagated to caller). Worker's dequeue loop catches, logs, and backs off with exponential delay before retry.
- **Unknown job type during dequeue:** Worker logs an error, marks the entry `Failed` with `"Unknown job type: {name}"`, and moves on. Happens if a job type is removed while old entries still exist.

## Migration / Compatibility

This is a breaking change to the internal implementation of `BackgroundJobsModule`:
- TickerQ tables (`TimeTickers`, `CronTickers`) are no longer used. The EF migration drops them and creates `JobQueueEntries`.
- Any in-flight TickerQ jobs at upgrade time will be lost — this is a dev framework, so a one-line note in the upgrade guide is sufficient.
- `IBackgroundJobs` public API is unchanged — no module code needs to change.
- `IModuleJob` and `IJobExecutionContext` are unchanged — no job code needs to change.

## Files to Create

- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs.Contracts/IJobQueue.cs`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs.Contracts/JobQueueEntry.cs`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Queue/DatabaseJobQueue.cs`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Queue/JobQueueEntryEntity.cs`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Queue/JobQueueEntryConfiguration.cs`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Worker/JobProcessorService.cs`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Worker/StalledJobSweeperService.cs`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Worker/WorkerIdentity.cs`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Worker/BackgroundJobsWorkerOptions.cs`
- `framework/SimpleModule.Hosting/SimpleModuleWorkerExtensions.cs` (`AddSimpleModuleWorker`)
- `template/SimpleModule.Worker/SimpleModule.Worker.csproj`
- `template/SimpleModule.Worker/Program.cs`
- `template/SimpleModule.Worker/appsettings.json`
- `modules/Email/src/SimpleModule.Email/Endpoints/SendTestEmailEndpoint.cs`
- `modules/Email/src/SimpleModule.Email/Providers/LogOnlyEmailProvider.cs`
- `modules/BackgroundJobs/tests/SimpleModule.BackgroundJobs.Tests/Worker/JobProcessorServiceTests.cs`
- `modules/BackgroundJobs/tests/SimpleModule.BackgroundJobs.Tests/Queue/DatabaseJobQueueTests.cs`

## Files to Modify

- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/BackgroundJobsModule.cs` — remove TickerQ, add `WorkerMode` switch, register `IJobQueue`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Services/BackgroundJobsService.cs` — rewrite to use `IJobQueue`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Services/BackgroundJobsContractsService.cs` — read from `JobQueueEntries`
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/Data/BackgroundJobsDbContext.cs` — add `JobQueueEntries` DbSet, remove TickerQ entity configuration
- `modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/SimpleModule.BackgroundJobs.csproj` — remove TickerQ package references
- `modules/Email/src/SimpleModule.Email/EmailModule.cs` — conditionally register `LogOnlyEmailProvider`
- `SimpleModule.slnx` — add `template/SimpleModule.Worker/SimpleModule.Worker.csproj` under `/template/`
- `SimpleModule.AppHost/AppHost.cs` — add worker project with `.WithReplicas(2)` and DB reference
- Delete TickerQ bridge files: `JobExecutionBridge.cs`, `JobExceptionHandler.cs`, `NoOpTickerManagerFactory.cs`, `JobDispatchPayload.cs` (replaced by `JobQueueEntry`)

## Open Questions

None — all clarifications resolved during brainstorming.
