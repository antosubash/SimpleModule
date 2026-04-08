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
}
