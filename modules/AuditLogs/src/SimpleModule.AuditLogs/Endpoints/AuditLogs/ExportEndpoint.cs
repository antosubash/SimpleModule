using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.AuditLogs.Endpoints.AuditLogs;

public class ExportEndpoint : IEndpoint
{
    public const string Route = AuditLogsConstants.Routes.Export;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async ([AsParameters] AuditExportRequest request, IAuditLogContracts auditLogs) =>
                {
                    var stream = await auditLogs.ExportAsync(request);
                    var format = request.EffectiveFormat;
                    var contentType = format.Equals("json", StringComparison.OrdinalIgnoreCase)
                        ? "application/json"
                        : "text/csv";
                    var extension = format.Equals("json", StringComparison.OrdinalIgnoreCase)
                        ? "json"
                        : "csv";
                    return TypedResults.File(
                        stream,
                        contentType,
                        $"audit-logs-{DateTimeOffset.UtcNow:yyyy-MM-dd}.{extension}"
                    );
                }
            )
            .RequirePermission(AuditLogsPermissions.Export);
}
