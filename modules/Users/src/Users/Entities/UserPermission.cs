namespace SimpleModule.Users.Entities;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - Permission is intentional for entity
public class UserPermission
#pragma warning restore CA1711
{
    public string UserId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }
}
