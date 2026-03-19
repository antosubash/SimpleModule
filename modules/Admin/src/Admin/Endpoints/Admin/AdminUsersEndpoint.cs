using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Services;
using SimpleModule.Core;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Endpoints.Admin;

public class AdminUsersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/users")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .DisableAntiforgery();

        // POST /admin/users — Create user
        group.MapPost(
            "/",
            async (
                [FromForm] string email,
                [FromForm] string displayName,
                [FromForm] string password,
                [FromForm] bool emailConfirmed,
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    DisplayName = displayName,
                    EmailConfirmed = emailConfirmed,
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    return Results.Redirect("/admin/users?error=create-failed");
                }

                var form = await context.Request.ReadFormAsync();
                var roles = form["roles"]
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Select(r => r!)
                    .ToList();
                if (roles.Count > 0)
                {
                    await userManager.AddToRolesAsync(user, roles);
                }

                await audit.LogAsync(user.Id, adminId, "UserCreated", $"Created user {email}");

                return Results.Redirect($"/admin/users/{user.Id}/edit");
            }
        );

        // POST /admin/users/{id} — Update details
        group.MapPost(
            "/{id}",
            async (
                string id,
                [FromForm] string displayName,
                [FromForm] string email,
                [FromForm] string? emailConfirmed,
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                user.DisplayName = displayName;
                user.Email = email;
                user.UserName = email;
                user.EmailConfirmed = emailConfirmed is not null;

                await userManager.UpdateAsync(user);
                await audit.LogAsync(
                    id,
                    adminId,
                    "UserUpdated",
                    $"Updated user details for {email}"
                );

                return Results.Redirect($"/admin/users/{id}/edit?tab=details");
            }
        );

        // POST /admin/users/{id}/roles — Set roles
        group.MapPost(
            "/{id}/roles",
            async (
                string id,
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                var form = await context.Request.ReadFormAsync();
                var newRoles = form["roles"]
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Select(r => r!)
                    .ToHashSet();
                var currentRoles = (await userManager.GetRolesAsync(user)).ToHashSet();

                var toRemove = currentRoles.Except(newRoles).ToList();
                var toAdd = newRoles.Except(currentRoles).ToList();

                if (toRemove.Count > 0)
                {
                    await userManager.RemoveFromRolesAsync(user, toRemove);
                    foreach (var role in toRemove)
                    {
                        await audit.LogAsync(id, adminId, "RoleRemoved", $"Removed role {role}");
                    }
                }

                if (toAdd.Count > 0)
                {
                    await userManager.AddToRolesAsync(user, toAdd);
                    foreach (var role in toAdd)
                    {
                        await audit.LogAsync(id, adminId, "RoleAdded", $"Added role {role}");
                    }
                }

                return Results.Redirect($"/admin/users/{id}/edit?tab=roles");
            }
        );

        // POST /admin/users/{id}/permissions — Set direct permissions
        group.MapPost(
            "/{id}/permissions",
            async (
                string id,
                HttpContext context,
                IPermissionContracts permissionContracts,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var userId = UserId.From(id);

                var form = await context.Request.ReadFormAsync();
                var newPermissions = form["permissions"]
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Select(p => p!)
                    .ToHashSet();

                var currentPermissions = await permissionContracts.GetPermissionsForUserAsync(
                    userId
                );

                foreach (var perm in currentPermissions.Where(p => !newPermissions.Contains(p)))
                {
                    await audit.LogAsync(
                        id,
                        adminId,
                        "PermissionRevoked",
                        $"Revoked permission {perm}"
                    );
                }

                foreach (var perm in newPermissions.Where(p => !currentPermissions.Contains(p)))
                {
                    await audit.LogAsync(
                        id,
                        adminId,
                        "PermissionGranted",
                        $"Granted permission {perm}"
                    );
                }

                await permissionContracts.SetPermissionsForUserAsync(userId, newPermissions);

                return Results.Redirect($"/admin/users/{id}/edit?tab=roles");
            }
        );

        // POST /admin/users/{id}/reset-password — Reset password
        group.MapPost(
            "/{id}/reset-password",
            async (
                string id,
                [FromForm] string newPassword,
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                await userManager.ResetPasswordAsync(user, token, newPassword);
                await audit.LogAsync(id, adminId, "PasswordReset");

                return Results.Redirect($"/admin/users/{id}/edit?tab=security");
            }
        );

        // POST /admin/users/{id}/lock — Lock account
        group.MapPost(
            "/{id}/lock",
            async (
                string id,
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                await userManager.SetLockoutEnabledAsync(user, true);
                await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                await audit.LogAsync(id, adminId, "AccountLocked");

                return Results.Redirect($"/admin/users/{id}/edit?tab=security");
            }
        );

        // POST /admin/users/{id}/unlock — Unlock account
        group.MapPost(
            "/{id}/unlock",
            async (
                string id,
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                await userManager.SetLockoutEndDateAsync(user, null);
                await userManager.ResetAccessFailedCountAsync(user);
                await audit.LogAsync(id, adminId, "AccountUnlocked");

                return Results.Redirect($"/admin/users/{id}/edit?tab=security");
            }
        );

        // POST /admin/users/{id}/force-reverify — Force email re-verification
        group.MapPost(
            "/{id}/force-reverify",
            async (
                string id,
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                user.EmailConfirmed = false;
                await userManager.UpdateAsync(user);
                await audit.LogAsync(id, adminId, "EmailReverified");

                return Results.Redirect($"/admin/users/{id}/edit?tab=security");
            }
        );

        // POST /admin/users/{id}/disable-2fa — Disable two-factor authentication
        group.MapPost(
            "/{id}/disable-2fa",
            async (
                string id,
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                await userManager.SetTwoFactorEnabledAsync(user, false);
                await userManager.ResetAuthenticatorKeyAsync(user);
                await audit.LogAsync(id, adminId, "TwoFactorDisabled");

                return Results.Redirect($"/admin/users/{id}/edit?tab=security");
            }
        );

        // POST /admin/users/{id}/deactivate — Soft-delete (deactivate) user
        group.MapPost(
            "/{id}/deactivate",
            async (
                string id,
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                user.DeactivatedAt = DateTimeOffset.UtcNow;
                await userManager.UpdateAsync(user);
                await userManager.SetLockoutEnabledAsync(user, true);
                await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                await audit.LogAsync(id, adminId, "UserDeactivated");

                return Results.Redirect($"/admin/users/{id}/edit?tab=details");
            }
        );

        // POST /admin/users/{id}/reactivate — Reactivate user
        group.MapPost(
            "/{id}/reactivate",
            async (
                string id,
                HttpContext context,
                UserManager<ApplicationUser> userManager,
                AuditService audit
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                user.DeactivatedAt = null;
                await userManager.UpdateAsync(user);
                await userManager.SetLockoutEndDateAsync(user, null);
                await userManager.ResetAccessFailedCountAsync(user);
                await audit.LogAsync(id, adminId, "UserReactivated");

                return Results.Redirect($"/admin/users/{id}/edit?tab=details");
            }
        );
    }
}
