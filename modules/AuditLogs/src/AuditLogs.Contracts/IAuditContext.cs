namespace SimpleModule.AuditLogs.Contracts;

public interface IAuditContext
{
    Guid CorrelationId { get; }
    string? UserId { get; set; }
    string? UserName { get; set; }
    string? IpAddress { get; set; }
}
