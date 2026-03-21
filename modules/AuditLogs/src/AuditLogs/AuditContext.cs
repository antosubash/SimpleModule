using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs;

public class AuditContext : IAuditContext
{
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
}
