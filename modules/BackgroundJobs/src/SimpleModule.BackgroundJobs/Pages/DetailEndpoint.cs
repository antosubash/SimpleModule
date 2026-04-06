using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.BackgroundJobs.Pages;

public class DetailEndpoint : IViewEndpoint
{
    public const string Route = BackgroundJobsConstants.Routes.Detail;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            Route,
            async (Guid id, IBackgroundJobsContracts contracts) =>
            {
                var job = await contracts.GetJobDetailAsync(JobId.From(id));
                if (job is null)
                {
                    return Results.NotFound();
                }
                return Inertia.Render("BackgroundJobs/Detail", new { job });
            }
        );
    }
}
