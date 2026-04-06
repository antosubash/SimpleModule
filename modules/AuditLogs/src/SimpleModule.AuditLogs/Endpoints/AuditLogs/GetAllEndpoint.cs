using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.AuditLogs.Endpoints.AuditLogs;

public class GetAllEndpoint : IEndpoint
{
    public const string Route = AuditLogsConstants.Routes.GetAll;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async ([AsParameters] AuditQueryRequest request, IAuditLogContracts auditLogs) =>
                    TypedResults.Ok(await auditLogs.QueryAsync(request))
            )
            .RequirePermission(AuditLogsPermissions.View);
}
