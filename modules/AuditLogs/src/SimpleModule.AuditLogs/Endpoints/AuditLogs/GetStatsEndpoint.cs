using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.AuditLogs.Endpoints.AuditLogs;

public class GetStatsEndpoint : IEndpoint
{
    public const string Route = AuditLogsConstants.Routes.GetStats;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async (DateTimeOffset from, DateTimeOffset to, IAuditLogContracts auditLogs) =>
                    TypedResults.Ok(await auditLogs.GetStatsAsync(from, to))
            )
            .RequirePermission(AuditLogsPermissions.View);
}
