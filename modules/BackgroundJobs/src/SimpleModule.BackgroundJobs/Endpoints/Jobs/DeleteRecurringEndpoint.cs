using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.BackgroundJobs.Endpoints.Jobs;

public class DeleteRecurringEndpoint : IEndpoint
{
    public const string Route = BackgroundJobsConstants.Routes.DeleteRecurring;
    public const string Method = "DELETE";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                Route,
                async (Guid id, IBackgroundJobs jobs) =>
                {
                    await jobs.RemoveRecurringAsync(RecurringJobId.From(id));
                    return Results.NoContent();
                }
            )
            .RequirePermission(BackgroundJobsPermissions.ManageJobs);
}
