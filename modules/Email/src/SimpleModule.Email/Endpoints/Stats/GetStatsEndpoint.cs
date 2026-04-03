using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Stats;

public class GetStatsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/stats",
                async (IEmailContracts emailContracts) =>
                    TypedResults.Ok(await emailContracts.GetEmailStatsAsync())
            )
            .RequirePermission(EmailPermissions.ViewHistory);
}
