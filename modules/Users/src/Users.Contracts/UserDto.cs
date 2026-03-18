using SimpleModule.Core;

namespace SimpleModule.Users.Contracts;

[Dto]
public class UserDto
{
    public UserId Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
}
