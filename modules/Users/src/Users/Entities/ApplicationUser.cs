using Microsoft.AspNetCore.Identity;

namespace SimpleModule.Users.Entities;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
}
