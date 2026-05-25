using SimpleModule.Core;

namespace SimpleModule.Identity.Contracts;

[Dto]
public class SessionDto
{
    public string TokenId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ApplicationName { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public bool IsCurrent { get; set; }
}
