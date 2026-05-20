using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.BackgroundJobs.Scheduler;

internal static partial class SchedulerReconciler
{
    /// <summary>
    /// Sync the in-memory <paramref name="definitions"/> into <c>ScheduledJobStates</c>.
    /// Inserts missing rows, refreshes mutable fields on existing rows, but leaves
    /// <c>LastRunAt</c>/<c>NextRunAt</c> alone (the tick path manages those).
    /// </summary>
    public static async Task ReconcileAsync(
        BackgroundJobsDbContext db,
        IReadOnlyList<ScheduledJobDefinition> definitions,
        DateTimeOffset now,
        ILogger logger,
        CancellationToken ct
    )
    {
        if (definitions.Count == 0)
            return;

        var names = definitions.Select(d => d.Name).ToHashSet(StringComparer.Ordinal);
        var existing = await db
            .ScheduledJobStates.Where(s => names.Contains(s.Name))
            .ToDictionaryAsync(s => s.Name, StringComparer.Ordinal, ct);

        foreach (var def in definitions)
        {
            try
            {
                ReconcileOne(db, def, existing, now);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogReconcileError(logger, def.Name, ex);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static void ReconcileOne(
        BackgroundJobsDbContext db,
        ScheduledJobDefinition def,
        Dictionary<string, ScheduledJobState> existing,
        DateTimeOffset now
    )
    {
        var payload = def.Payload is null
            ? null
            : JsonSerializer.Serialize(def.Payload, def.Payload.GetType());

        if (!existing.TryGetValue(def.Name, out var row))
        {
            var nextRun = TryGetNextOccurrence(def.CronExpression, def.TimeZoneId, now);
            db.ScheduledJobStates.Add(
                new ScheduledJobState
                {
                    Name = def.Name,
                    JobTypeName = def.JobType.AssemblyQualifiedName!,
                    CronExpression = def.CronExpression,
                    TimeZoneId = def.TimeZoneId,
                    Payload = payload,
                    WithoutOverlapping = def.WithoutOverlapping,
                    OnOneServer = def.OnOneServer,
                    IsEnabled = true,
                    NextRunAt = nextRun,
                    CreatedAt = now,
                    UpdatedAt = now,
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                }
            );
            return;
        }

        var cronChanged = !string.Equals(
            row.CronExpression,
            def.CronExpression,
            StringComparison.Ordinal
        );
        var tzChanged = !string.Equals(row.TimeZoneId, def.TimeZoneId, StringComparison.Ordinal);

        row.JobTypeName = def.JobType.AssemblyQualifiedName!;
        row.CronExpression = def.CronExpression;
        row.TimeZoneId = def.TimeZoneId;
        row.Payload = payload;
        row.WithoutOverlapping = def.WithoutOverlapping;
        row.OnOneServer = def.OnOneServer;
        row.UpdatedAt = now;

        if (cronChanged || tzChanged || row.NextRunAt is null)
        {
            row.NextRunAt = TryGetNextOccurrence(def.CronExpression, def.TimeZoneId, now);
        }
    }

    private static DateTimeOffset? TryGetNextOccurrence(
        string expression,
        string timeZoneId,
        DateTimeOffset now
    )
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;
        return CronCalculator.GetNextOccurrence(expression, timeZoneId, now);
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Scheduler reconcile failed for definition '{Name}'"
    )]
    private static partial void LogReconcileError(ILogger logger, string name, Exception ex);
}
