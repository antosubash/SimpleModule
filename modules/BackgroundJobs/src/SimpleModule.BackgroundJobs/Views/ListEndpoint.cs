using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.BackgroundJobs.Views;

[ViewPage("BackgroundJobs/List")]
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
            {
                var filter = new JobFilter
                {
                    State = state,
                    JobType = jobType,
                    Page = page ?? 1,
                    PageSize = pageSize ?? 20,
                };
                return Inertia.Render(
                    "BackgroundJobs/List",
                    new { jobs = await contracts.GetJobsAsync(filter) }
                );
            }
        );
    }
}
