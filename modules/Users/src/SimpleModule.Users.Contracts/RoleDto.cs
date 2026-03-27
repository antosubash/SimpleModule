using SimpleModule.Core;

namespace SimpleModule.Users.Contracts;

[Dto]
public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}
