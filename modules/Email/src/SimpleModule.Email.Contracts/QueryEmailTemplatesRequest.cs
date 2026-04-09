using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

[Dto]
public class QueryEmailTemplatesRequest
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Search { get; set; }

    public int EffectivePage => Page is > 0 ? Page.Value : 1;
    public int EffectivePageSize => PageSize is > 0 and <= 100 ? PageSize.Value : 20;
}
