using SimpleModule.Users.Contracts;

namespace SimpleModule.Permissions.Entities;

#pragma warning disable CA1711
public class UserPermission
#pragma warning restore CA1711
{
    public UserId UserId { get; set; }
    public string Permission { get; set; } = string.Empty;
}
