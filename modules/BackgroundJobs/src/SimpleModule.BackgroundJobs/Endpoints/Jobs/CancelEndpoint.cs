using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.BackgroundJobs.Endpoints.Jobs;

public class CancelEndpoint : IEndpoint
{
    public const string Route = BackgroundJobsConstants.Routes.Cancel;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (Guid id, IBackgroundJobs jobs) =>
                {
                    await jobs.CancelAsync(JobId.From(id));
                    return Results.Ok();
                }
            )
            .RequirePermission(BackgroundJobsPermissions.ManageJobs);
}
