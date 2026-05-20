using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.BackgroundJobs.Scheduler;

internal sealed partial class DatabaseJobMutex(
    BackgroundJobsDbContext db,
    ILogger<DatabaseJobMutex> logger
) : IJobMutex
{
    public async Task<bool> TryAcquireAsync(
        string name,
        string ownerId,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var now = DateTimeOffset.UtcNow;
        var expires = now + ttl;
        var existing = await db.JobMutexes.FirstOrDefaultAsync(m => m.Name == name, ct);

        if (existing is null)
        {
            db.JobMutexes.Add(
                new JobMutex
                {
                    Name = name,
                    OwnerWorkerId = ownerId,
                    AcquiredAt = now,
                    ExpiresAt = expires,
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                }
            );
            try
            {
                await db.SaveChangesAsync(ct);
                LogAcquired(logger, name, ownerId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                LogContended(logger, name, ex.Message);
                return false;
            }
        }

        if (existing.ExpiresAt > now && !string.Equals(existing.OwnerWorkerId, ownerId, StringComparison.Ordinal))
        {
            return false;
        }

        existing.OwnerWorkerId = ownerId;
        existing.AcquiredAt = now;
        existing.ExpiresAt = expires;
        existing.ConcurrencyStamp = Guid.NewGuid().ToString("N");

        try
        {
            await db.SaveChangesAsync(ct);
            LogAcquired(logger, name, ownerId);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            LogContended(logger, name, "concurrency");
            return false;
        }
    }

    public async Task ReleaseAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var row = await db.JobMutexes.FirstOrDefaultAsync(m => m.Name == name, ct);
        if (row is null)
            return;
        db.JobMutexes.Remove(row);
        try
        {
            await db.SaveChangesAsync(ct);
            LogReleased(logger, name);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another worker beat us to release; that's fine — mutex is gone either way.
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Mutex '{Name}' acquired by {Owner}")]
    private static partial void LogAcquired(ILogger logger, string name, string owner);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Mutex '{Name}' contended: {Reason}")]
    private static partial void LogContended(ILogger logger, string name, string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Mutex '{Name}' released")]
    private static partial void LogReleased(ILogger logger, string name);
}
