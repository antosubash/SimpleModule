using System.Security.Claims;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Extensions;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Keycloak.Services;

/// <summary>
/// Just-in-time (JIT) user provisioning service. When a user authenticates
/// via Keycloak, this service ensures a local <see cref="ApplicationUser"/>
/// shadow record exists so that the rest of the SimpleModule infrastructure
/// (permissions, settings, audit logs, etc.) can reference the user.
///
/// Called from <see cref="KeycloakClaimsTransformation"/> after claims are mapped.
/// </summary>
public sealed partial class KeycloakUserSyncService(
    IUserContracts userContracts,
    ILogger<KeycloakUserSyncService> logger
)
{
    /// <summary>
    /// Ensures a local user record exists for the authenticated principal.
    /// Creates a new shadow user if one does not exist; updates email/display
    /// name if they have changed in Keycloak.
    /// </summary>
    public async Task SyncUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default
    )
    {
        var userId = principal.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return;

        var email =
            principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? string.Empty;

        var displayName =
            principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("preferred_username")
            ?? principal.FindFirstValue("name")
            ?? email;

        var existingUser = await userContracts.GetUserByIdAsync(UserId.From(userId));

        if (existingUser is null)
        {
            LogCreatingShadowUser(logger, userId, email);

            await userContracts.CreateUserAsync(
                new CreateUserRequest
                {
                    Id = userId, // Keycloak sub — ensures lookup by ID finds the shadow user
                    Email = email,
                    DisplayName = displayName,
                    // Keycloak-managed users don't use local passwords.
                    // Set a random value that cannot be guessed.
                    Password = Guid.NewGuid().ToString("N") + "!Aa1",
                }
            );
        }
        else if (
            !string.Equals(existingUser.Email, email, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(existingUser.DisplayName, displayName, StringComparison.Ordinal)
        )
        {
            LogUpdatingShadowUser(logger, userId, email, displayName);

            await userContracts.UpdateUserAsync(
                UserId.From(userId),
                new UpdateUserRequest { Email = email, DisplayName = displayName }
            );
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Creating shadow user for Keycloak subject {UserId} ({Email})"
    )]
    private static partial void LogCreatingShadowUser(ILogger logger, string userId, string email);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updating shadow user {UserId}: email={Email}, displayName={DisplayName}"
    )]
    private static partial void LogUpdatingShadowUser(
        ILogger logger,
        string userId,
        string email,
        string displayName
    );
}
