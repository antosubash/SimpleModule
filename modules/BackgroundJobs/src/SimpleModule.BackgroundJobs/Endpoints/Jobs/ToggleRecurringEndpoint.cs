using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.BackgroundJobs.Endpoints.Jobs;

public class ToggleRecurringEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/recurring/{id:guid}/toggle",
                async (Guid id, BackgroundJobsDbContext db) =>
                {
                    var ticker = await db.CronTickers
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (ticker is null)
                    {
                        return Results.NotFound();
                    }

                    ticker.IsEnabled = !ticker.IsEnabled;
                    await db.SaveChangesAsync();
                    return Results.Ok(new { ticker.IsEnabled });
                }
            )
            .RequirePermission(BackgroundJobsPermissions.ManageJobs);
}
