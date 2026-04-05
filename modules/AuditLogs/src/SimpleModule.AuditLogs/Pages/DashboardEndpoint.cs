using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.AuditLogs.Pages;

public class DashboardEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/dashboard",
                async (
                    DateTimeOffset? from,
                    DateTimeOffset? to,
                    string? userId,
                    IAuditLogContracts auditLogs
                ) =>
                {
                    var now = DateTimeOffset.UtcNow;
                    var effectiveFrom = from ?? now.AddDays(-30);
                    var effectiveTo = to ?? now;
                    var stats = await auditLogs.GetDashboardStatsAsync(
                        effectiveFrom,
                        effectiveTo,
                        userId
                    );
                    return Inertia.Render(
                        "AuditLogs/Dashboard",
                        new
                        {
                            stats,
                            from = effectiveFrom,
                            to = effectiveTo,
                            userId = userId ?? "",
                            users = stats.TopUsers,
                        }
                    );
                }
            )
            .RequirePermission(AuditLogsPermissions.View);
}
