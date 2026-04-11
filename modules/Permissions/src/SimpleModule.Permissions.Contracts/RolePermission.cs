using SimpleModule.Core;

namespace SimpleModule.Permissions.Contracts;

#pragma warning disable CA1711
[NoDtoGeneration]
public class RolePermission
#pragma warning restore CA1711
{
    public RoleId RoleId { get; set; }
    public string Permission { get; set; } = string.Empty;
}
