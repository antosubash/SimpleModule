using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;

namespace SimpleModule.AuditLogs.Endpoints.AuditLogs;

public class GetByIdEndpoint : IEndpoint
{
    public const string Route = AuditLogsConstants.Routes.GetById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (int id, IAuditLogContracts auditLogs) =>
                    CrudEndpoints.GetById(() => auditLogs.GetByIdAsync(AuditEntryId.From(id)))
            )
            .RequirePermission(AuditLogsPermissions.View);
}
