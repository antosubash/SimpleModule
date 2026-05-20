# Task Scheduler API (Issue #159)

Branch: `task-scheduler`

## Goal

Layer a Laravel-style fluent task-scheduling API on top of the existing `BackgroundJobs` infrastructure so that modules can declare recurring jobs at startup time. The schema already has `ScheduledAt`/`CronExpression`; this work adds:

1. A fluent registration surface (`IScheduler` / `IScheduledJob<T>`).
2. A reconciler/poller hosted service that turns declared schedules into queued jobs.
3. Mutex (`WithoutOverlapping`) and leader (`OnOneServer`) primitives.
4. `sm jobs list-scheduled` CLI command.

The existing **runtime-added** recurring-jobs path (`IBackgroundJobs.AddRecurringAsync`) is untouched. The new `IScheduler` API is the **declarative**, code-defined path.

## Architecture

```
ConfigureServices time:                    Runtime (Consumer host):

  scheduler.Job<NightlyAuditPurge>()                ┌────────────────────┐
           .DailyAt("02:00")        ─────────►      │ SchedulerRegistry  │ (singleton)
           .Timezone("UTC");                        │   List<Definition> │
                                                    └─────────┬──────────┘
                                                              │
                                                              ▼
                                                    ┌────────────────────┐
                                                    │ SchedulerService   │ tick every 30s
                                                    │ (IHostedService)   │
                                                    └─────────┬──────────┘
                                                              │
                                              try-acquire LeaseAsync("scheduler") if OnOneServer
                                                              │
                                                              ▼
                                              for each def: compute NextRunAt
                                              if NextRunAt <= now:
                                                  if WithoutOverlapping:
                                                      mutex acquire by JobName
                                                      if held → skip
                                                  IJobQueue.EnqueueAsync(JobQueueEntry)
                                                  ScheduledJobState.LastRunAt = now
                                                  ScheduledJobState.NextRunAt = cron.GetNext(now)
```

## Phase 1 — Contracts

`modules/BackgroundJobs/src/SimpleModule.BackgroundJobs.Contracts/`

### New files

- `IScheduler.cs`

  ```csharp
  public interface IScheduler
  {
      IScheduledJob<TJob> Job<TJob>(string? name = null) where TJob : IModuleJob;
      IReadOnlyList<ScheduledJobDefinition> Definitions { get; }
  }
  ```

- `IScheduledJob.cs`

  ```csharp
  public interface IScheduledJob<TJob> where TJob : IModuleJob
  {
      IScheduledJob<TJob> Cron(string expression);
      IScheduledJob<TJob> EveryMinutes(int minutes);
      IScheduledJob<TJob> Hourly();
      IScheduledJob<TJob> Daily();
      IScheduledJob<TJob> DailyAt(string time);            // "HH:mm"
      IScheduledJob<TJob> Weekdays();                      // Mon-Fri
      IScheduledJob<TJob> Timezone(string tz);             // IANA id (UTC default)
      IScheduledJob<TJob> WithoutOverlapping();
      IScheduledJob<TJob> OnOneServer();
      IScheduledJob<TJob> WithPayload(object payload);
  }
  ```

- `ScheduledJobDefinition.cs` — mutable in-memory record returned by `Job<T>()`. Fields: `Name`, `JobType`, `CronExpression`, `TimeZoneId`, `Payload`, `WithoutOverlapping`, `OnOneServer`.

- `ScheduledJobDto.cs` — flat DTO for CLI / future endpoints (`[Dto]`). Fields: `Name`, `JobType`, `CronExpression`, `TimeZoneId`, `WithoutOverlapping`, `OnOneServer`, `LastRunAt`, `NextRunAt`, `IsEnabled`.

### Update `IBackgroundJobsContracts` — no change required (the scheduler is its own contract, parallel to existing recurring API).

## Phase 2 — Implementation

`modules/BackgroundJobs/src/SimpleModule.BackgroundJobs/`

### Schedule registry & DSL

- `Scheduler/SchedulerRegistry.cs` — singleton implementing `IScheduler`. Owns `List<ScheduledJobDefinition>`.
- `Scheduler/ScheduledJobBuilder.cs` — generic fluent builder implementing `IScheduledJob<T>`. Each method mutates the underlying `ScheduledJobDefinition`. `DailyAt("HH:mm")` → cron `m H * * *`. `EveryMinutes(n)` → `*/n * * * *`. `Hourly` → `0 * * * *`. `Daily` → `0 0 * * *`. `Weekdays` → `0 0 * * MON-FRI`.
- Timezone validation via `TimeZoneInfo.FindSystemTimeZoneById` (cross-platform on .NET 10 — Windows IDs accepted via ICU mapping).

### DB entities

`Contracts/` (new):
- `ScheduledJobState.cs` — `Name` (PK), `JobTypeName`, `CronExpression`, `TimeZoneId`, `LastRunAt`, `NextRunAt`, `IsEnabled`, `WithoutOverlapping`, `OnOneServer`, `Payload`, `CreatedAt`, `UpdatedAt`.
- `JobMutex.cs` — `Name` (PK), `OwnerWorkerId`, `AcquiredAt`, `ExpiresAt`, `ConcurrencyStamp`.
- `JobLease.cs` — `Name` (PK), `OwnerWorkerId`, `AcquiredAt`, `ExpiresAt`, `ConcurrencyStamp`.

All three are `[NoDtoGeneration]` since they're internal state.

`EntityConfigurations/` (new):
- `ScheduledJobStateConfiguration.cs`, `JobMutexConfiguration.cs`, `JobLeaseConfiguration.cs`.

Update `BackgroundJobsDbContext` to add three `DbSet<>`s and apply the configurations.

### Scheduler service

- `Scheduler/SchedulerService.cs` — `BackgroundService`, polls `BackgroundJobsWorkerOptions.SchedulerTickInterval` (default 30s).
  Tick:
  1. Reconcile registry → `ScheduledJobState` (insert missing, update cron/timezone/options on existing, leave `LastRunAt` alone). Definitions removed in code do not auto-delete DB rows (keep `IsEnabled=true`; explicit cleanup is a future concern).
  2. For each `ScheduledJobState` where any definition has `OnOneServer=true`: try-acquire single `scheduler` lease via `IInstanceLeader` (TTL = 2× tick interval). If lease not held, skip the tick.
  3. For each enabled state where `NextRunAt <= now`:
     - If `WithoutOverlapping`: try `IMutex.TryAcquire(Name, ttl=1h)`. If already held → skip and re-compute next run. Mutex is **not** released by the scheduler — the worker releases it on completion/failure (hook into existing `JobProcessorService`).
     - Compute next-run via `CronExpression.GetNextOccurrence` using `TimeZoneInfo` for the definition's timezone.
     - Build `JobQueueEntry` with `RecurringName = "schedule:" + def.Name` (sentinel — distinguishes "scheduled" from existing "recurring"), serialise payload.
     - `IJobQueue.EnqueueAsync`; update `LastRunAt = now`, `NextRunAt = nextOccurrence`. Bump `UpdatedAt`.
  4. Per-definition `try/catch` → log + continue; never let one bad cron stop the loop.

- `Scheduler/IInstanceLeader.cs` + `DatabaseInstanceLeader.cs` — `Task<bool> TryAcquireAsync(string name, TimeSpan ttl, string ownerId, ct)`. SQL: `UPDATE JobLeases SET Owner=@me, Acquired=@now, Expires=@now+ttl WHERE Name=@n AND (Owner=@me OR Expires < @now) [+ insert if missing]`. Postgres uses upsert; SQLite uses transaction + select-for-update analogue.

- `Scheduler/IJobMutex.cs` + `DatabaseJobMutex.cs` — `Task<bool> TryAcquireAsync(...)`, `Task ReleaseAsync(name, ct)`. Same primitive as lease but explicit release.

- Hook into worker: after `IModuleJob.ExecuteAsync` completes (success **or** failure), if `entry.RecurringName` starts with `"schedule:"` and corresponds to a `ScheduledJobState` with `WithoutOverlapping=true`, release the mutex. Implement by introducing a `ScheduleSentinel` static helper and small additions in `JobProcessorService.ExecuteEntryAsync`.

### Module wiring

`BackgroundJobsModule.cs`:
- Always register `SchedulerRegistry` as singleton + `IScheduler`.
- Always register `IJobMutex` + `IInstanceLeader` as scoped.
- In Consumer mode also register `SchedulerService` as `IHostedService`.
- Add `BackgroundJobsWorkerOptions.SchedulerTickInterval` (default 30s) + `SchedulerLeaseTtl` (default 60s).

## Phase 3 — Tests

`modules/BackgroundJobs/tests/SimpleModule.BackgroundJobs.Tests/Scheduler/`:

- `ScheduledJobBuilderTests.cs` — DSL produces correct cron strings & flags; invalid timezone / cron rejected.
- `SchedulerServiceTickTests.cs` — uses `TestDbContextFactory` + a fake `IClock` (introduce `TimeProvider`). Cases:
  - Definition with NextRunAt past now → job enqueued.
  - Definition with NextRunAt future → no enqueue.
  - Bad cron in one definition → others still processed.
  - WithoutOverlapping: simulate mutex held → enqueue skipped.
  - OnOneServer: two ticks from two `ownerId`s → only one enqueues.
- `DatabaseJobMutexTests.cs` — concurrent TryAcquire only succeeds once until TTL or release.
- `DatabaseInstanceLeaderTests.cs` — same shape as mutex but no explicit release.
- `SchedulerReconciliationTests.cs` — definition added → row inserted; cron changed in code → row updated; existing LastRunAt preserved.

## Phase 4 — CLI

`cli/SimpleModule.Cli/Commands/Jobs/`:
- `JobsListScheduledCommand.cs` + `JobsListScheduledSettings.cs` (with optional `--connection`).
- Loads `appsettings.json` from solution context, opens a connection to the configured DB, reads `ScheduledJobStates` (use raw ADO.NET to avoid pulling the whole EF graph into the CLI), pretty-prints via Spectre `Table` with columns: Name, Type, Cron, TZ, Next, Last, Enabled, Flags (Mutex/Single).
- Register branch `jobs` in `Program.cs`.

## Phase 5 — Docs

- `docs/scheduler.md` — overview, DSL reference table, semantics of `WithoutOverlapping`/`OnOneServer`, recommended tick interval, idempotency notes, sample registration in a module's `ConfigureServices`.
- Link from `docs/CONSTITUTION.md` if scheduler imposes any new rule (it doesn't — just a feature).

## Verification

- `dotnet build` clean (TreatWarningsAsErrors).
- `dotnet test --filter "FullyQualifiedName~BackgroundJobs"`.
- `npm run check` (formatting/linting unaffected).
- Manual smoke: register a 1-minute schedule in `SimpleModule.Host`, run host, observe job execution.
- Open PR closing #159.

## Out of scope (for this PR)

- A view-endpoint UI for browsing schedules (the existing `/admin/jobs/recurring` page covers the legacy path; a separate page for declarative schedules can come later).
- Distributed mutex with row-level locking on Postgres — initial implementation uses the same compare-and-set pattern that works on both providers; can be optimised later if needed.

## Review

- [x] Contracts: `IScheduler` + `IScheduledJob<T>` + fluent builder + `ScheduledJobDefinition` + `ScheduledJobDto` + `ScheduledJobState`/`JobMutex`/`JobLease` entities + `SchedulerRegistry` + `AddScheduledJobs` DI extension.
- [x] Impl: `SchedulerService` (BackgroundService) reconciling registry → state and enqueueing due jobs every 30 s. Per-definition failure isolation. `WithoutOverlapping` via `IJobMutex` (release hooked into `JobProcessorService`). `OnOneServer` via `IInstanceLeader`.
- [x] EF: 3 new entity configurations applied, indices on `IsEnabled/NextRunAt`.
- [x] Tests: 31 new tests across `Scheduler/`. Coverage: DSL → cron rendering, validation; database mutex contention + TTL takeover; database leader election + renewal; tick path enqueues due, skips future, isolates bad cron, honours `WithoutOverlapping`, honours `OnOneServer`.
- [x] CLI: `sm jobs list-scheduled` reads `ScheduledJobStates` via raw ADO.NET against Sqlite or Postgres, falls back to `appsettings.json` for connection.
- [x] Docs: `docs/scheduler.md` with quick-start, DSL reference, semantics, configuration.
- [x] Build: `dotnet build` clean (TreatWarningsAsErrors). Full test suite green: **1015 tests passing, 0 failing**.
