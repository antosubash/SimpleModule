using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Views.Admin;

public class UsersActivityEndpoint : IEndpoint
{
    private const int PageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/users/{id}/activity",
                async (
                    string id,
                    AdminDbContext adminDb,
                    UserManager<ApplicationUser> userManager,
                    int page = 1
                ) =>
                {
                    var entries = await adminDb
                        .AuditLogEntries.Where(e => e.UserId == id)
                        .OrderByDescending(e => e.Timestamp)
                        .Skip((page - 1) * PageSize)
                        .Take(PageSize)
                        .Select(e => new
                        {
                            e.Id,
                            e.Action,
                            e.Details,
                            e.PerformedByUserId,
                            e.Timestamp,
                        })
                        .ToListAsync();

                    var performerIds = entries.Select(e => e.PerformedByUserId).Distinct().ToList();
                    var performers = await userManager
                        .Users.Where(u => performerIds.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

                    var total = await adminDb.AuditLogEntries.CountAsync(e => e.UserId == id);

                    return TypedResults.Ok(
                        new
                        {
                            entries = entries.Select(e => new
                            {
                                e.Id,
                                e.Action,
                                e.Details,
                                performedBy = performers.GetValueOrDefault(
                                    e.PerformedByUserId,
                                    "Unknown"
                                ),
                                timestamp = e.Timestamp.ToString("O"),
                            }),
                            total,
                            page,
                            totalPages = (int)Math.Ceiling((double)total / PageSize),
                        }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
