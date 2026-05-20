using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.BackgroundJobs.Scheduler;

public sealed partial class DatabaseInstanceLeader(
    BackgroundJobsDbContext db,
    ILogger<DatabaseInstanceLeader> logger
) : IInstanceLeader
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
        var existing = await db.JobLeases.FirstOrDefaultAsync(l => l.Name == name, ct);

        if (existing is null)
        {
            db.JobLeases.Add(
                new JobLease
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Lease '{Name}' acquired by {Owner}")]
    private static partial void LogAcquired(ILogger logger, string name, string owner);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Lease '{Name}' contended: {Reason}")]
    private static partial void LogContended(ILogger logger, string name, string reason);
}
