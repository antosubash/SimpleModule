using SimpleModule.Core;

namespace SimpleModule.Notifications.Contracts;

[Dto]
public class QueryNotificationsRequest
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public bool? UnreadOnly { get; set; }
    public string? Channel { get; set; }
    public string? Type { get; set; }

    public int EffectivePage => Page is > 0 ? Page.Value : 1;
    public int EffectivePageSize => PageSize is > 0 and <= 100 ? PageSize.Value : 20;
}
