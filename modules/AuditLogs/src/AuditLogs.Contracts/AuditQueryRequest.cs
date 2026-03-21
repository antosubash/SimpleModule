namespace SimpleModule.AuditLogs.Contracts;

public class AuditQueryRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? UserId { get; set; }
    public string? Module { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public AuditSource? Source { get; set; }
    public AuditAction? Action { get; set; }
    public int? StatusCode { get; set; }
    public string? SearchText { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDescending { get; set; }

    public int EffectivePage => Page ?? 1;
    public int EffectivePageSize => PageSize is > 0 and <= 200 ? PageSize.Value : 50;
    public string EffectiveSortBy => SortBy ?? "Timestamp";
    public bool EffectiveSortDescending => SortDescending ?? true;
}
