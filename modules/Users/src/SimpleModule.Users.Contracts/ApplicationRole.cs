using Microsoft.AspNetCore.Identity;

namespace SimpleModule.Users.Contracts;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
