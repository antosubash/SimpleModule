using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.AuditLogs.Pages;

public class DetailEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/{id}",
                async (int id, IAuditLogContracts auditLogs) =>
                {
                    var entry = await auditLogs.GetByIdAsync(AuditEntryId.From(id));
                    if (entry is null)
                        return TypedResults.NotFound();

                    var correlated = await auditLogs.GetByCorrelationIdAsync(entry.CorrelationId);
                    return Inertia.Render("AuditLogs/Detail", new { entry, correlated });
                }
            )
            .RequirePermission(AuditLogsPermissions.View);
}
