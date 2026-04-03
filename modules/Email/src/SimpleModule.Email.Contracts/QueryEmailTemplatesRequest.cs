using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

[Dto]
public class QueryEmailTemplatesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}
