using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.BackgroundJobs.Endpoints.Jobs;

public class GetRecurringEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/recurring",
                async (IBackgroundJobsContracts contracts) =>
                    Results.Ok(await contracts.GetRecurringJobsAsync())
            )
            .RequirePermission(BackgroundJobsPermissions.ViewJobs);
}
