namespace SimpleModule.AuditLogs.Contracts;

public class AuditEntry
{
    public AuditEntryId Id { get; set; }
    public Guid CorrelationId { get; set; }
    public AuditSource Source { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? HttpMethod { get; set; }
    public string? Path { get; set; }
    public string? QueryString { get; set; }
    public int? StatusCode { get; set; }
    public long? DurationMs { get; set; }
    public string? RequestBody { get; set; }
    public string? Module { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public AuditAction? Action { get; set; }
    public string? Changes { get; set; }
    public string? Metadata { get; set; }
}
