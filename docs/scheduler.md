# Task Scheduler

The `BackgroundJobs` module exposes a fluent, code-declared scheduler so any
module can register recurring work at startup. Definitions live in C#, are
reconciled to the database at every tick, and end up as ordinary
`JobQueueEntry` rows that the worker picks up like any other job.

The scheduler is **declarative** — it complements (but does not replace) the
runtime API on `IBackgroundJobs` for ad-hoc recurring jobs created from
endpoints.

## Quick start

In your module's `ConfigureServices`, call `AddScheduledJobs`:

```csharp
using SimpleModule.BackgroundJobs.Contracts;

public class AuditLogsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleJob<NightlyAuditPurge>();
        services.AddModuleJob<DailyDigestEmail>();

        services.AddScheduledJobs(scheduler =>
        {
            scheduler.Job<NightlyAuditPurge>()
                .DailyAt("02:00")
                .Timezone("UTC")
                .WithoutOverlapping();

            scheduler.Job<DailyDigestEmail>("daily-digest")
                .Cron("0 8 * * MON-FRI")
                .Timezone("America/New_York")
                .OnOneServer();
        });
    }
}
```

Each `scheduler.Job<T>()` registers a single declarative schedule. The job type
must implement `IModuleJob` and be registered with `AddModuleJob<T>()` so the
worker can resolve it from the DI container.

## DSL reference

| Method | Cron produced | Notes |
|---|---|---|
| `Cron("0 8 * * MON-FRI")` | as given | Standard 5-field cron; 6-field with seconds also supported |
| `EveryMinutes(15)` | `*/15 * * * *` | 1–59 only |
| `Hourly()` | `0 * * * *` | Top of each hour |
| `Daily()` | `0 0 * * *` | Midnight |
| `DailyAt("13:45")` | `45 13 * * *` | 24h `HH:mm` |
| `Weekdays()` | `0 0 * * MON-FRI` | Midnight Mon–Fri |
| `Timezone("UTC")` | — | IANA timezone for cron evaluation (default `UTC`) |
| `WithoutOverlapping()` | — | Skip if the prior run is still in-flight |
| `OnOneServer()` | — | Single elected host enqueues when many run the scheduler |
| `WithPayload(obj)` | — | Serialised to JSON for `IJobExecutionContext.Data` |

## Semantics

### Reconciliation

Every tick (default 30s — configurable via `BackgroundJobs:Scheduler:TickInterval`)
the scheduler upserts each in-memory definition into the `ScheduledJobStates`
table. Changing cron/timezone in code is applied on the next tick; `LastRunAt`
and `NextRunAt` are preserved across reconciles unless the cron actually
changed.

A definition removed from code does **not** automatically delete its row — the
row keeps its `IsEnabled` value, the scheduler ignores rows whose definitions
are no longer in the registry, and the worker won't enqueue them again. Manually
truncate the table if you need to purge orphans.

### `WithoutOverlapping()`

Backed by a per-job mutex row in `JobMutexes`. Before enqueueing, the scheduler
calls `IJobMutex.TryAcquireAsync(name)`; if the mutex is held by an in-flight
run, the tick *skips* enqueueing (and advances `NextRunAt`). When the worker
finishes (success or failure), it releases the mutex so the next tick can
re-enqueue.

Mutex TTL defaults to 1 hour; configure via `BackgroundJobs:Scheduler:MutexTtl`.
A stuck worker can't block a schedule forever — the TTL provides the safety
net.

### `OnOneServer()`

Backed by a single `scheduler` lease row in `JobLeases`. On every tick a host
tries to acquire the lease; only the holder runs the rest of the tick. Lease
TTL defaults to 1 minute (≈2× tick interval); set via
`BackgroundJobs:Scheduler:LeaseTtl`.

If a definition has `OnOneServer()` *anywhere* in the registry, the lease is
required for that host's tick. Hosts without the lease skip silently.

### Failure isolation

A definition with a bad cron expression is logged at `Error` and the tick
continues with the rest. The bad definition's `NextRunAt` stays `null` and no
work is enqueued for it until the cron is fixed.

## Inspecting state

The host writes `ScheduledJobStates`, `JobMutexes`, and `JobLeases` to the
database configured by `Database:DefaultConnection`. From the CLI:

```bash
sm jobs list-scheduled                            # uses appsettings.json
sm jobs list-scheduled --connection "Data Source=app.db" --provider Sqlite
sm jobs list-scheduled --connection "Host=...;Database=..." --provider Postgres
```

The command prints Name, Job type, Cron, TZ, Next run, Last run, and flags
(`mutex`, `single`, `disabled`).

## Worker mode

The hosted `SchedulerService` is registered as `IHostedService` only when the
module is in `Consumer` mode (the same condition that registers
`JobProcessorService`). Producer-only hosts still register `IScheduler` so
`AddScheduledJobs` succeeds — but no ticks run there.

## Configuration

```jsonc
{
  "BackgroundJobs": {
    "Scheduler": {
      "TickInterval": "00:00:30",
      "LeaseTtl":     "00:01:00",
      "MutexTtl":     "01:00:00"
    }
  }
}
```

Sensible defaults are baked in; override only when tuning for a specific
deployment.
