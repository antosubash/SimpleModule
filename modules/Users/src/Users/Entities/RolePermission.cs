namespace SimpleModule.Users.Entities;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - Permission is intentional for entity
public class RolePermission
#pragma warning restore CA1711
{
    public string RoleId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;

    public ApplicationRole? Role { get; set; }
}
