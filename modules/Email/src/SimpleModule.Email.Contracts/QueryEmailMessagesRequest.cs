using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

[Dto]
public class QueryEmailMessagesRequest
{
    // Nullable so ASP.NET Minimal API parameter binding ([AsParameters]) treats
    // them as optional. The service applies defaults (page 1, size 20).
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
