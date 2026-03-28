using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

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
                IUserAdminContracts userAdmin
            ) =>
            {
                var form = await context.Request.ReadFormAsync();
                var filteredRoles = form["roles"]
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Select(r => r!)
                    .ToList();

                var request = new CreateAdminUserRequest
                {
                    Email = email,
                    DisplayName = displayName,
                    Password = password,
                    EmailConfirmed = emailConfirmed,
                    Roles = filteredRoles,
                };

                var user = await userAdmin.CreateUserWithPasswordAsync(request);

                return TypedResults.Redirect($"/admin/users/{user.Id}/edit");
            }
        );

        // POST /admin/users/{id} — Update details
        group.MapPost(
            "/{id}",
            async Task<IResult> (
                string id,
                [FromForm] string displayName,
                [FromForm] string email,
                [FromForm] string? emailConfirmed,
                IUserAdminContracts userAdmin
            ) =>
            {
                var request = new UpdateAdminUserRequest
                {
                    DisplayName = displayName,
                    Email = email,
                    EmailConfirmed = emailConfirmed is not null,
                };

                await userAdmin.UpdateUserDetailsAsync(UserId.From(id), request);

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=details");
            }
        );

        // POST /admin/users/{id}/roles — Set roles
        group.MapPost(
            "/{id}/roles",
            async Task<IResult> (
                string id,
                HttpContext context,
                IUserAdminContracts userAdmin
            ) =>
            {
                var form = await context.Request.ReadFormAsync();
                var newRoles = form["roles"]
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Select(r => r!)
                    .ToList();

                await userAdmin.SetUserRolesAsync(UserId.From(id), newRoles);

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=roles");
            }
        );

        // POST /admin/users/{id}/permissions — Set direct permissions
        group.MapPost(
            "/{id}/permissions",
            async Task<IResult> (
                string id,
                HttpContext context,
                IPermissionContracts permissionContracts
            ) =>
            {
                var userId = UserId.From(id);

                var form = await context.Request.ReadFormAsync();
                var newPermissions = form["permissions"]
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Select(p => p!)
                    .ToHashSet();

                await permissionContracts.SetPermissionsForUserAsync(userId, newPermissions);

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=roles");
            }
        );

        // POST /admin/users/{id}/reset-password — Reset password
        group.MapPost(
            "/{id}/reset-password",
            async Task<IResult> (
                string id,
                [FromForm] string newPassword,
                IUserAdminContracts userAdmin
            ) =>
            {
                await userAdmin.ResetPasswordAsync(UserId.From(id), newPassword);

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=security");
            }
        );

        // POST /admin/users/{id}/lock — Lock account
        group.MapPost(
            "/{id}/lock",
            async Task<IResult> (
                string id,
                HttpContext context,
                IUserAdminContracts userAdmin
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                if (id == adminId)
                    return TypedResults.BadRequest(
                        new { error = "You cannot lock your own account." }
                    );

                await userAdmin.LockAccountAsync(UserId.From(id));

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=security");
            }
        );

        // POST /admin/users/{id}/unlock — Unlock account
        group.MapPost(
            "/{id}/unlock",
            async Task<IResult> (
                string id,
                IUserAdminContracts userAdmin
            ) =>
            {
                await userAdmin.UnlockAccountAsync(UserId.From(id));

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=security");
            }
        );

        // POST /admin/users/{id}/force-reverify — Force email re-verification
        group.MapPost(
            "/{id}/force-reverify",
            async Task<IResult> (
                string id,
                IUserAdminContracts userAdmin
            ) =>
            {
                await userAdmin.ForceEmailReverificationAsync(UserId.From(id));

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=security");
            }
        );

        // POST /admin/users/{id}/disable-2fa — Disable two-factor authentication
        group.MapPost(
            "/{id}/disable-2fa",
            async Task<IResult> (
                string id,
                IUserAdminContracts userAdmin
            ) =>
            {
                await userAdmin.DisableTwoFactorAsync(UserId.From(id));

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=security");
            }
        );

        // POST /admin/users/{id}/deactivate — Soft-delete (deactivate) user
        group.MapPost(
            "/{id}/deactivate",
            async Task<IResult> (
                string id,
                HttpContext context,
                IUserAdminContracts userAdmin
            ) =>
            {
                var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                if (id == adminId)
                    return TypedResults.BadRequest(
                        new { error = "You cannot deactivate your own account." }
                    );

                await userAdmin.DeactivateAsync(UserId.From(id));

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=details");
            }
        );

        // POST /admin/users/{id}/reactivate — Reactivate user
        group.MapPost(
            "/{id}/reactivate",
            async Task<IResult> (
                string id,
                IUserAdminContracts userAdmin
            ) =>
            {
                await userAdmin.ReactivateAsync(UserId.From(id));

                return TypedResults.Redirect($"/admin/users/{id}/edit?tab=details");
            }
        );
    }
}
