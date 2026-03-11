using System.Security.Claims;
using OpenIddict.Abstractions;

namespace SimpleModule.Users.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from claims, supporting both OpenIddict (sub) and ASP.NET Identity (NameIdentifier).
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.GetClaim(OpenIddictConstants.Claims.Subject)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
