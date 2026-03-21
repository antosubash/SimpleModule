namespace SimpleModule.AuditLogs.Contracts;

public class AuditExportRequest : AuditQueryRequest
{
    public string? Format { get; set; }

    public string EffectiveFormat => Format ?? "csv";
}
