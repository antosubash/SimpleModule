using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.BackgroundJobs.Endpoints.Jobs;

public class ToggleRecurringEndpoint : IEndpoint
{
    public const string Route = BackgroundJobsConstants.Routes.ToggleRecurring;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (Guid id, IBackgroundJobs jobs) =>
                {
                    try
                    {
                        var isEnabled = await jobs.ToggleRecurringAsync(RecurringJobId.From(id));
                        return Results.Ok(new { IsEnabled = isEnabled });
                    }
                    catch (InvalidOperationException)
                    {
                        return Results.NotFound();
                    }
                }
            )
            .RequirePermission(BackgroundJobsPermissions.ManageJobs);
}
