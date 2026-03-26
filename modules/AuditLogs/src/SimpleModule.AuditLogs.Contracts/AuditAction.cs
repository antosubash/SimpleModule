namespace SimpleModule.AuditLogs.Contracts;

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
    Viewed,
    LoginSuccess,
    LoginFailed,
    PermissionGranted,
    PermissionRevoked,
    SettingChanged,
    Exported,
    Other,
}
