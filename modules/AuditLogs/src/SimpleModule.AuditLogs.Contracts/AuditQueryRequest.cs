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

    /// <summary>
    /// Opt-in keyset cursor. When set (and the default Timestamp-descending ordering
    /// is used), the page is fetched via <c>WHERE Timestamp &lt; Before</c> instead of
    /// OFFSET, skipping the per-request <c>COUNT(*)</c> and the O(offset) row-skip that
    /// make deep pages slow. Pass the <c>Timestamp</c> of the last item from the
    /// previous page to fetch the next one.
    /// </summary>
    public DateTimeOffset? Before { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDescending { get; set; }

    public int EffectivePage => Page ?? 1;
    public int EffectivePageSize => PageSize is > 0 and <= 200 ? PageSize.Value : 50;
    public string EffectiveSortBy => SortBy ?? "Timestamp";
    public bool EffectiveSortDescending => SortDescending ?? true;
}
