using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.BackgroundJobs.Pages;

public class ListEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/list",
            async (
                IBackgroundJobsContracts contracts,
                JobState? state,
                string? jobType,
                int? page,
                int? pageSize
            ) =>
                Inertia.Render(
                    "BackgroundJobs/List",
                    new
                    {
                        jobs = await contracts.GetJobsAsync(
                            JobFilter.FromQuery(state, jobType, page, pageSize)
                        ),
                    }
                )
        );
    }
}
