using SimpleModule.Core;

namespace SimpleModule.Users.Contracts;

[Dto]
public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsLockedOut { get; set; }
    public bool IsDeactivated { get; set; }
    public int AccessFailedCount { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? LastLoginAt { get; set; }
}
