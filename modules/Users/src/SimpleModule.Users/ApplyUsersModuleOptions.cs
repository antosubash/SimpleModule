using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace SimpleModule.Users;

/// <summary>
/// Bridges <see cref="UsersModuleOptions"/> into ASP.NET Identity's <see cref="IdentityOptions"/>,
/// allowing the host app to control password and lockout policies via the module options pattern.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by DI"
)]
internal sealed class ApplyUsersModuleOptions(IOptions<UsersModuleOptions> moduleOptions)
    : IPostConfigureOptions<IdentityOptions>
{
    public void PostConfigure(string? name, IdentityOptions options)
    {
        var opts = moduleOptions.Value;

        options.Password.RequiredLength = opts.PasswordMinLength;
        options.Password.RequireDigit = opts.PasswordRequireDigit;
        options.Password.RequireUppercase = opts.PasswordRequireUppercase;
        options.Password.RequireLowercase = opts.PasswordRequireLowercase;
        options.Password.RequireNonAlphanumeric = opts.PasswordRequireNonAlphanumeric;
        options.Lockout.DefaultLockoutTimeSpan = opts.LockoutDuration;
        options.Lockout.MaxFailedAccessAttempts = opts.MaxFailedAccessAttempts;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
    }
}
