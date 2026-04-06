namespace SimpleModule.AuditLogs.Contracts;

public static class AuditLogsConstants
{
    public const string ModuleName = "AuditLogs";
    public const string RoutePrefix = "/api/audit-logs";
    public const string ViewPrefix = "/audit-logs";

    public static class Routes
    {
        // API endpoints
        public const string GetAll = "/";
        public const string GetById = "/{id}";
        public const string GetStats = "/stats";
        public const string Export = "/export";

        // View endpoints
        public const string Browse = "/browse";
        public const string Dashboard = "/dashboard";
        public const string Detail = "/{id}";
    }
}
