namespace SimpleModule.Database;

public static class DatabaseProviderDetector
{
    public static DatabaseProvider Detect(string connectionString)
    {
        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.PostgreSql;

        if (
            connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains(@"Server=.\", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains(@"Server=(", StringComparison.OrdinalIgnoreCase)
        )
            return DatabaseProvider.SqlServer;

        return DatabaseProvider.Sqlite;
    }
}
