using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.BackgroundJobs.Endpoints.Jobs;

public class RetryEndpoint : IEndpoint
{
    public const string Route = BackgroundJobsConstants.Routes.Retry;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (Guid id, IBackgroundJobsContracts contracts) =>
                {
                    await contracts.RetryAsync(JobId.From(id));
                    return Results.Ok();
                }
            )
            .RequirePermission(BackgroundJobsPermissions.ManageJobs);
}
