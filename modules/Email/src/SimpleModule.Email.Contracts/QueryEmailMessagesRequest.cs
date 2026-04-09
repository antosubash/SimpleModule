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
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDescending { get; set; }

    public int EffectivePage => Page is > 0 ? Page.Value : 1;
    public int EffectivePageSize => PageSize is > 0 and <= 100 ? PageSize.Value : 20;
    public string EffectiveSortBy => SortBy ?? "CreatedAt";
    public bool EffectiveSortDescending => SortDescending ?? true;
}
