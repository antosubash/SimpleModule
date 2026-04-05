using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.BackgroundJobs.Pages;

public class RecurringEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/recurring",
            async (IBackgroundJobsContracts contracts) =>
                Inertia.Render(
                    "BackgroundJobs/Recurring",
                    new { jobs = await contracts.GetRecurringJobsAsync() }
                )
        );
    }
}
