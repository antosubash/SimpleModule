using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Keycloak.Services;

/// <summary>
/// Maps Keycloak-specific JWT claims to standard .NET claims so that the
/// rest of the application (especially <c>PermissionClaimsTransformation</c>)
/// can consume them uniformly.
///
/// Specifically:
/// <list type="bullet">
///   <item>Extracts roles from the <c>realm_access</c> JSON claim and adds
///         individual <see cref="ClaimTypes.Role"/> claims.</item>
///   <item>Maps <c>preferred_username</c> to <see cref="ClaimTypes.Name"/>
///         when no Name claim is already present.</item>
/// </list>
///
/// This transformation runs before <c>PermissionClaimsTransformation</c>,
/// which depends on Role claims being present to resolve permissions.
/// </summary>
public sealed class KeycloakClaimsTransformation(
    ILogger<KeycloakClaimsTransformation> logger,
    KeycloakUserSyncService syncService
) : IClaimsTransformation
{
    private const string RealmAccessClaim = "realm_access";
    private const string PreferredUsernameClaim = "preferred_username";
    private const string KeycloakRolesMarker = "keycloak_roles_mapped";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        // Guard against running twice on the same principal (ASP.NET Core
        // may invoke IClaimsTransformation multiple times per request).
        if (principal.HasClaim(c => c.Type == KeycloakRolesMarker))
            return principal;

        var identity = new ClaimsIdentity("Keycloak");

        // Map realm_access.roles -> ClaimTypes.Role
        var realmAccessClaim = principal.FindFirst(RealmAccessClaim);
        if (realmAccessClaim is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                if (
                    doc.RootElement.TryGetProperty("roles", out var rolesElement)
                    && rolesElement.ValueKind == JsonValueKind.Array
                )
                {
                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrEmpty(roleName))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse Keycloak realm_access claim");
            }
        }

        // Map preferred_username -> ClaimTypes.Name (if not already present)
        if (!principal.HasClaim(c => c.Type == ClaimTypes.Name))
        {
            var preferredUsername = principal.FindFirstValue(PreferredUsernameClaim);
            if (!string.IsNullOrEmpty(preferredUsername))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, preferredUsername));
            }
        }

        // Sentinel to prevent duplicate transformation
        identity.AddClaim(new Claim(KeycloakRolesMarker, "true"));

        principal.AddIdentity(identity);

        // JIT-provision or update the local shadow user record
        await syncService.SyncUserAsync(principal);

        return principal;
    }
}
