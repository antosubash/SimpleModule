using System.Security.Claims;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Core.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from claims, supporting both OpenIddict (sub) and ASP.NET Identity (NameIdentifier).
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Returns null for Admin users (no scoping — sees all resources), or the user ID for regular users.
    /// </summary>
    public static string? GetScopedUserId(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(WellKnownRoles.Admin) ? null : principal.GetUserId();
    }
}
