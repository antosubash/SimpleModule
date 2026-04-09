using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

[Dto]
public class QueryEmailTemplatesRequest
{
    // Nullable so ASP.NET Minimal API parameter binding ([AsParameters]) treats
    // them as optional. The endpoint/service applies defaults (page 1, size 20).
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Search { get; set; }
}
