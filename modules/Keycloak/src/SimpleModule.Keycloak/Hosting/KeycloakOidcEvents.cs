using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace SimpleModule.Keycloak.Hosting;

internal static class KeycloakOidcEvents
{
    private const string RealmAccessClaim = "realm_access";

    public static void OnTokenValidated(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return;

        var realmAccess = context.Principal.FindFirst(RealmAccessClaim);
        if (realmAccess is null)
            return;

        try
        {
            using var doc = JsonDocument.Parse(realmAccess.Value);
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
        catch (JsonException)
        {
            // Malformed realm_access — skip silently; KeycloakClaimsTransformation
            // will also attempt to parse and log the warning.
        }
    }
}
