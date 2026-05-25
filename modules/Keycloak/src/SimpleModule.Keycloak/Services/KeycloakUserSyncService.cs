using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Extensions;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Keycloak.Services;

public sealed partial class KeycloakUserSyncService(
    IUserContracts userContracts,
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    ILogger<KeycloakUserSyncService> logger
)
{
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

        var keycloakRoles = principal
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Where(r => !string.IsNullOrEmpty(r))
            .ToList();

        var existingUser = await userContracts.GetUserByIdAsync(UserId.From(userId));

        if (existingUser is null)
        {
            LogCreatingShadowUser(logger, userId, email);

            await userContracts.CreateUserAsync(
                new CreateUserRequest
                {
                    Id = userId,
                    Email = email,
                    DisplayName = displayName,
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

        await SyncRolesAsync(userId, keycloakRoles);
    }

    private async Task SyncRolesAsync(string userId, List<string> keycloakRoles)
    {
        foreach (var roleName in keycloakRoles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                LogCreatingRole(logger, roleName);
                await roleManager.CreateAsync(
                    new ApplicationRole
                    {
                        Name = roleName,
                        Description = $"Synced from Keycloak",
                        CreatedAt = DateTime.UtcNow,
                    }
                );
            }
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return;

        var currentRoles = await userManager.GetRolesAsync(user);
        var rolesToAdd = keycloakRoles
            .Except(currentRoles, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var rolesToRemove = currentRoles
            .Except(keycloakRoles, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rolesToAdd.Count > 0)
        {
            LogSyncingRoles(logger, userId, rolesToAdd.Count);
            await userManager.AddToRolesAsync(user, rolesToAdd);
        }

        if (rolesToRemove.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, rolesToRemove);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating local role: {RoleName}")]
    private static partial void LogCreatingRole(ILogger logger, string roleName);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Syncing roles for user {UserId}: adding {Count} role(s)"
    )]
    private static partial void LogSyncingRoles(ILogger logger, string userId, int count);
}
