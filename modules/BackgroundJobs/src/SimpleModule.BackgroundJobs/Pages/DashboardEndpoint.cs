using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.BackgroundJobs.Pages;

public class DashboardEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/",
            async (IBackgroundJobsContracts contracts) =>
            {
                var activeJobs = await contracts.GetJobsAsync(
                    new JobFilter { State = JobState.Running, PageSize = 5 }
                );
                var failedJobs = await contracts.GetJobsAsync(
                    new JobFilter { State = JobState.Failed, PageSize = 5 }
                );
                var recurringCount = await contracts.GetRecurringCountAsync();

                return Inertia.Render(
                    "BackgroundJobs/Dashboard",
                    new
                    {
                        activeJobs = activeJobs.Items,
                        activeCount = activeJobs.TotalCount,
                        failedJobs = failedJobs.Items,
                        failedCount = failedJobs.TotalCount,
                        recurringCount,
                    }
                );
            }
        );
    }
}
