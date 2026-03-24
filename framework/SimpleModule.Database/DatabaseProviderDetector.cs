namespace SimpleModule.Database;

/// <summary>
/// Detects the database provider based on connection string heuristics or explicit configuration.
/// Fails loudly when the connection string cannot be recognized, preventing silent misconfiguration.
/// </summary>
public static class DatabaseProviderDetector
{
    /// <summary>
    /// Detects the database provider. If <paramref name="explicitProvider"/> is set, it takes
    /// precedence over connection-string heuristics. Throws if the provider cannot be determined.
    /// </summary>
    /// <param name="connectionString">The database connection string to analyze.</param>
    /// <param name="explicitProvider">Optional explicit provider name (e.g., "PostgreSql", "SqlServer", "Sqlite"). Takes precedence over heuristics.</param>
    /// <returns>The detected <see cref="DatabaseProvider"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// - An explicit provider is specified but is not a valid <see cref="DatabaseProvider"/> value.
    /// - The connection string cannot be matched to any recognized provider pattern.
    /// </exception>
    public static DatabaseProvider Detect(string connectionString, string? explicitProvider = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitProvider))
        {
            if (
                DatabaseProviderExtensions.TryParse(
                    explicitProvider,
                    out var parsed,
                    ignoreCase: true
                )
            )
            {
                return parsed;
            }

            throw new InvalidOperationException(
                $"Invalid database provider '{explicitProvider}'. "
                    + $"Valid providers are: {string.Join(", ", GetValidProviders())}."
            );
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

        // SQLite uses "Data Source=" pattern
        if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.Sqlite;

        throw new InvalidOperationException(
            $"Unable to detect database provider from connection string. "
                + $"Recognized patterns: "
                + $"PostgreSQL (contains 'Host='), "
                + $"SQL Server (contains 'Initial Catalog=', 'Server=.\\', or 'Server=('), "
                + $"SQLite (contains 'Data Source='). "
                + $"Alternatively, explicitly configure Database:Provider in appsettings.json with one of: {string.Join(", ", GetValidProviders())}."
        );
    }

    /// <summary>
    /// Returns the list of valid database provider names.
    /// </summary>
#pragma warning disable CA1024 // Use properties where appropriate
    public static IReadOnlyList<string> GetValidProviders()
#pragma warning restore CA1024
    {
        return new[] { "Sqlite", "PostgreSql", "SqlServer" };
    }
}
