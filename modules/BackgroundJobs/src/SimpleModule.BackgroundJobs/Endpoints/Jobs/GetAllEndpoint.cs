using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.BackgroundJobs.Endpoints.Jobs;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/",
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
                    return Results.Ok(await contracts.GetJobsAsync(filter));
                }
            )
            .RequirePermission(BackgroundJobsPermissions.ViewJobs);
}
