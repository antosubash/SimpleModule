namespace SimpleModule.Database;

public static class DatabaseProviderDetector
{
    /// <summary>
    /// Detects the database provider. If <paramref name="explicitProvider"/> is set, it takes
    /// precedence over connection-string heuristics.
    /// </summary>
    public static DatabaseProvider Detect(string connectionString, string? explicitProvider = null)
    {
        if (
            !string.IsNullOrWhiteSpace(explicitProvider)
            && Enum.TryParse<DatabaseProvider>(explicitProvider, ignoreCase: true, out var parsed)
        )
        {
            return parsed;
        }

        if (
            connectionString.Contains(
                DatabaseConstants.PostgresHostPrefix,
                StringComparison.OrdinalIgnoreCase
            )
        )
            return DatabaseProvider.PostgreSql;

        if (
            connectionString.Contains(
                DatabaseConstants.SqlServerCatalogPrefix,
                StringComparison.OrdinalIgnoreCase
            )
            || connectionString.Contains(
                DatabaseConstants.SqlServerLocalPrefix,
                StringComparison.OrdinalIgnoreCase
            )
            || connectionString.Contains(
                DatabaseConstants.SqlServerExpressionPrefix,
                StringComparison.OrdinalIgnoreCase
            )
        )
            return DatabaseProvider.SqlServer;

        return DatabaseProvider.Sqlite;
    }
}
