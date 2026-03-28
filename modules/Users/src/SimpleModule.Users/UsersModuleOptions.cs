using SimpleModule.Core;

namespace SimpleModule.Users;

/// <summary>
/// Configurable options for the Users module.
/// </summary>
public class UsersModuleOptions : IModuleOptions
{
    /// <summary>
    /// Minimum required password length. Default: 8.
    /// </summary>
    public int PasswordMinLength { get; set; } = 8;

    /// <summary>
    /// Whether passwords must contain at least one digit. Default: true.
    /// </summary>
    public bool PasswordRequireDigit { get; set; } = true;

    /// <summary>
    /// Whether passwords must contain at least one uppercase letter. Default: true.
    /// </summary>
    public bool PasswordRequireUppercase { get; set; } = true;

    /// <summary>
    /// Whether passwords must contain at least one lowercase letter. Default: true.
    /// </summary>
    public bool PasswordRequireLowercase { get; set; } = true;

    /// <summary>
    /// Whether passwords must contain at least one non-alphanumeric character. Default: false.
    /// </summary>
    public bool PasswordRequireNonAlphanumeric { get; set; }

    /// <summary>
    /// Maximum number of failed login attempts before account lockout. Default: 5.
    /// </summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    /// <summary>
    /// Duration of account lockout after exceeding max failed attempts. Default: 5 minutes.
    /// </summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(5);
}
