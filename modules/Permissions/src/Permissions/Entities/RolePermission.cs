using SimpleModule.Permissions.Contracts;

namespace SimpleModule.Permissions.Entities;

#pragma warning disable CA1711
public class RolePermission
#pragma warning restore CA1711
{
    public RoleId RoleId { get; set; }
    public string Permission { get; set; } = string.Empty;
}
