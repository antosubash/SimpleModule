using System.Diagnostics;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs;

public class AuditContext : IAuditContext
{
    public Guid CorrelationId { get; } = GetCorrelationIdFromActivityOrNewGuid();
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }

    private static Guid GetCorrelationIdFromActivityOrNewGuid()
    {
        var activity = Activity.Current;
        if (activity is not null && activity.TraceId != default)
        {
            var traceIdHex = activity.TraceId.ToHexString()[..32];
            return new Guid(traceIdHex);
        }

        return Guid.NewGuid();
    }
}
