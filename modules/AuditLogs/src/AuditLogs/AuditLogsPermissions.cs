using SimpleModule.Core.Authorization;

namespace SimpleModule.AuditLogs;

public sealed class AuditLogsPermissions : IModulePermissions
{
    public const string View = "AuditLogs.View";
    public const string Export = "AuditLogs.Export";
}
