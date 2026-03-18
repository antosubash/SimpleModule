using SimpleModule.Core;

namespace SimpleModule.Admin.Contracts;

[Dto]
public class AuditLogEntryDto
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PerformedByUserId { get; set; } = string.Empty;
    public string PerformedByName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
