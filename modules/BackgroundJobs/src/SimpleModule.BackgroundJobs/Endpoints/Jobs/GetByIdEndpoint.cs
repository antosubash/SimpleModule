using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.BackgroundJobs.Endpoints.Jobs;

public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id:guid}",
                async (Guid id, IBackgroundJobsContracts contracts) =>
                {
                    var detail = await contracts.GetJobDetailAsync(JobId.From(id));
                    return detail is null ? Results.NotFound() : Results.Ok(detail);
                }
            )
            .RequirePermission(BackgroundJobsPermissions.ViewJobs);
}
