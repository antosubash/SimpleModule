namespace SimpleModule.AuditLogs.Contracts;

public class AuditStats
{
    public int TotalEntries { get; set; }
    public int UniqueUsers { get; set; }
    public Dictionary<string, int> ByModule { get; set; } = new();
    public Dictionary<string, int> ByAction { get; set; } = new();
    public Dictionary<string, int> ByStatusCode { get; set; } = new();
}
