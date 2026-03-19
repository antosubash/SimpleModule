using SimpleModule.Admin.Entities;

namespace SimpleModule.Admin.Services;

public class AuditService(AdminDbContext db)
{
    public async Task LogAsync(
        string userId,
        string performedByUserId,
        string action,
        string? details = null
    )
    {
        db.AuditLogEntries.Add(
            new AuditLogEntry
            {
                UserId = userId,
                PerformedByUserId = performedByUserId,
                Action = action,
                Details = details,
                Timestamp = DateTimeOffset.UtcNow,
            }
        );
        await db.SaveChangesAsync();
    }
}
