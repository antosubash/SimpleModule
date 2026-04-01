using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.BackgroundJobs.Endpoints.Jobs;

public class ToggleRecurringEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/recurring/{id:guid}/toggle",
                async (Guid id, IBackgroundJobs jobs) =>
                {
                    var isEnabled = await jobs.ToggleRecurringAsync(RecurringJobId.From(id));
                    return Results.Ok(new { IsEnabled = isEnabled });
                }
            )
            .RequirePermission(BackgroundJobsPermissions.ManageJobs);
}
