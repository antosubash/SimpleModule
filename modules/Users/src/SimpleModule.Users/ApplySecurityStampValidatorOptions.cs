using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace SimpleModule.Users;

/// <summary>
/// Bridges <see cref="UsersModuleOptions.SecurityStampValidationInterval"/> into ASP.NET Identity's
/// <see cref="SecurityStampValidatorOptions"/>. This controls how quickly a "Sign out everywhere"
/// action propagates to other devices that hold a still-valid cookie.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by DI"
)]
internal sealed class ApplySecurityStampValidatorOptions(IOptions<UsersModuleOptions> moduleOptions)
    : IPostConfigureOptions<SecurityStampValidatorOptions>
{
    public void PostConfigure(string? name, SecurityStampValidatorOptions options)
    {
        options.ValidationInterval = moduleOptions.Value.SecurityStampValidationInterval;
    }
}
