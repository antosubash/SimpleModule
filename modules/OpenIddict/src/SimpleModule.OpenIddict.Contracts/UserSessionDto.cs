using SimpleModule.Core;

namespace SimpleModule.OpenIddict.Contracts;

[Dto]
public class UserSessionDto
{
    public string TokenId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ApplicationName { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
}
