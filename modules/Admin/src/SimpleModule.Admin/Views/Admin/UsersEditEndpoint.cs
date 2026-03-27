using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Admin.Views.Admin;

[ViewPage("Admin/Admin/UsersEdit")]
public class UsersEditEndpoint : IViewEndpoint
{
    private const int ActivityPageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/users/{id}/edit",
                async (
                    string id,
                    IUserAdminContracts userAdmin,
                    IRoleAdminContracts roleAdmin,
                    IPermissionContracts permissionContracts,
                    AdminDbContext adminDb,
                    PermissionRegistry permissionRegistry,
                    string? tab
                ) =>
                {
                    var user = await userAdmin.GetAdminUserByIdAsync(UserId.From(id));
                    if (user is null)
                        return TypedResults.NotFound();

                    var allRoles = await roleAdmin.GetAllRolesAsync();

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
                    var performers = new Dictionary<string, string>();
                    foreach (var performerId in performerIds)
                    {
                        var performer = await userAdmin.GetAdminUserByIdAsync(
                            UserId.From(performerId)
                        );
                        if (performer is not null)
                        {
                            performers[performerId] = performer.DisplayName;
                        }
                    }

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
                            user,
                            userRoles = user.Roles,
                            userPermissions,
                            allRoles,
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
