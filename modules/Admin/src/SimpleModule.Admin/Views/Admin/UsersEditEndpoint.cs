using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Views.Admin;

public class UsersEditEndpoint : IViewEndpoint
{
    private const int ActivityPageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/users/{id}/edit",
                async (
                    string id,
                    UserManager<ApplicationUser> userManager,
                    RoleManager<ApplicationRole> roleManager,
                    IPermissionContracts permissionContracts,
                    AdminDbContext adminDb,
                    PermissionRegistry permissionRegistry,
                    string? tab
                ) =>
                {
                    var user = await userManager.FindByIdAsync(id);
                    if (user is null)
                        return TypedResults.NotFound();

                    var userRoles = await userManager.GetRolesAsync(user);
                    var allRoles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

                    // User direct permissions
                    var userPermissions = (
                        await permissionContracts.GetPermissionsForUserAsync(UserId.From(id))
                    ).ToList();

                    // Permission registry grouped by module
                    var permissionsByModule = permissionRegistry.ByModule.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToList()
                    );

                    // Activity log (first page)
                    var activityLog = await adminDb
                        .AuditLogEntries.Where(e => e.UserId == id)
                        .OrderByDescending(e => e.Timestamp)
                        .Take(ActivityPageSize)
                        .Select(e => new
                        {
                            e.Id,
                            e.Action,
                            e.Details,
                            e.PerformedByUserId,
                            e.Timestamp,
                        })
                        .ToListAsync();

                    // Resolve performer names for activity entries
                    var performerIds = activityLog
                        .Select(e => e.PerformedByUserId)
                        .Distinct()
                        .ToList();
                    var performers = await userManager
                        .Users.Where(u => performerIds.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

                    var activityWithNames = activityLog.Select(e => new
                    {
                        e.Id,
                        e.Action,
                        e.Details,
                        performedBy = performers.GetValueOrDefault(e.PerformedByUserId, "Unknown"),
                        timestamp = e.Timestamp.ToString("O"),
                    });

                    var activityTotal = await adminDb.AuditLogEntries.CountAsync(e =>
                        e.UserId == id
                    );

                    return Inertia.Render(
                        "Admin/Admin/UsersEdit",
                        new
                        {
                            user = new
                            {
                                id = user.Id,
                                displayName = user.DisplayName,
                                email = user.Email,
                                emailConfirmed = user.EmailConfirmed,
                                twoFactorEnabled = user.TwoFactorEnabled,
                                isLockedOut = user.LockoutEnd.HasValue
                                    && user.LockoutEnd > DateTimeOffset.UtcNow,
                                isDeactivated = user.DeactivatedAt.HasValue,
                                accessFailedCount = user.AccessFailedCount,
                                createdAt = user.CreatedAt.ToString("O"),
                                lastLoginAt = user.LastLoginAt?.ToString("O"),
                            },
                            userRoles = userRoles.ToList(),
                            userPermissions,
                            allRoles = allRoles
                                .Select(r => new
                                {
                                    id = r.Id,
                                    name = r.Name,
                                    description = r.Description,
                                })
                                .ToList(),
                            permissionsByModule,
                            activityLog = activityWithNames,
                            activityTotal,
                            tab = tab ?? "details",
                        }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
