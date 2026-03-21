using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.AuditLogs.Views;

public class BrowseEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/browse",
                async ([AsParameters] AuditQueryRequest request, IAuditLogContracts auditLogs) =>
                {
                    var result = await auditLogs.QueryAsync(request);
                    return Inertia.Render("AuditLogs/Browse", new { result, filters = request });
                }
            )
            .RequirePermission(AuditLogsPermissions.View);
}
