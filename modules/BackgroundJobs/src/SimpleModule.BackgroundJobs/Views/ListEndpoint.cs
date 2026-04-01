using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
            async (IBackgroundJobsContracts contracts, [AsParameters] JobFilter filter) =>
                Inertia.Render("BackgroundJobs/List", new { jobs = await contracts.GetJobsAsync(filter) })
        );
    }
}
