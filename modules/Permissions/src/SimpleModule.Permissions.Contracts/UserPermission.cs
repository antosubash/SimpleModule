using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Permissions.Contracts;

#pragma warning disable CA1711
[NoDtoGeneration]
public class UserPermission
#pragma warning restore CA1711
{
    public UserId UserId { get; set; }
    public string Permission { get; set; } = string.Empty;
}
