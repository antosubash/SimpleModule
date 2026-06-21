using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

[Dto]
public class QueryEmailMessagesRequest
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public EmailStatus? Status { get; set; }
    public string? To { get; set; }
    public string? Subject { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }

    /// <summary>
    /// Opt-in keyset cursor. When set (with the default CreatedAt-descending ordering),
    /// the page is fetched via <c>WHERE CreatedAt &lt; Before</c> instead of OFFSET,
    /// skipping the per-request <c>COUNT(*)</c> and the O(offset) row-skip. Pass the
    /// <c>CreatedAt</c> of the last item from the previous page to fetch the next one.
    /// </summary>
    public DateTimeOffset? Before { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDescending { get; set; }

    public int EffectivePage => Page is > 0 ? Page.Value : 1;
    public int EffectivePageSize => PageSize is > 0 and <= 100 ? PageSize.Value : 20;
    public string EffectiveSortBy => SortBy ?? "CreatedAt";
    public bool EffectiveSortDescending => SortDescending ?? true;
}
