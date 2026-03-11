namespace SimpleModule.Core.Constants;

public static class HealthCheckConstants
{
    public const string DatabaseCheckName = "database";
    public const string ReadyTag = "ready";
    public const string AllDatabasesReachable = "All module databases are reachable.";
    public const string DatabaseHealthCheckFailed = "Database health check failed.";
    public const string CannotConnectFormat = "Cannot connect to database for module '{0}'";
}
