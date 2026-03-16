using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SimpleModule.Database;

public static class DatabaseInitializerExtensions
{
    /// <summary>
    /// Creates module databases and tables using EnsureCreated.
    /// <para>
    /// <b>Limitation:</b> EnsureCreated cannot evolve existing schemas. If you add or rename
    /// columns after initial creation, the existing database will NOT be updated. For production
    /// workloads that require schema evolution, use EF Core migrations per module instead.
    /// </para>
    /// </summary>
    public static void EnsureModuleDatabases(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var infos = scope.ServiceProvider.GetServices<ModuleDbContextInfo>();
        var dbOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var createdDatabases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var info in infos)
        {
            var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(info.DbContextType);
            var connectionString = dbOptions.ModuleConnections.TryGetValue(
                info.ModuleName,
                out var moduleCs
            )
                ? moduleCs
                : dbOptions.DefaultConnection;

            if (createdDatabases.Add(connectionString))
            {
                // First context for this database — create DB and its tables
                dbContext.Database.EnsureCreated();
            }
            else
            {
                // Shared database already created — only create this module's tables if they don't exist
                var creator = dbContext.GetService<IRelationalDatabaseCreator>();
                var provider = DatabaseProviderDetector.Detect(
                    connectionString,
                    dbOptions.Provider
                );
                if (!ModuleTablesExist(dbContext, info.ModuleName, provider))
                {
                    creator.CreateTables();
                }
            }
        }
    }

    private static bool ModuleTablesExist(
        DbContext dbContext,
        string moduleName,
        DatabaseProvider provider
    )
    {
        var prefix = $"{moduleName}_";
        var entityTypes = dbContext.Model.GetEntityTypes();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
                continue;

            // Check for prefixed table (SQLite shared DB) or unprefixed (own DB)
            var nameToCheck = tableName.StartsWith(prefix, StringComparison.Ordinal)
                ? tableName
                : $"{prefix}{tableName}";

            // Schema names are lowercase by convention in PostgreSQL/SQL Server module isolation
#pragma warning disable CA1308 // Normalize strings to uppercase - schema names must be lowercase
            var schemaName = moduleName.ToLowerInvariant();
#pragma warning restore CA1308
            var exists = provider switch
            {
                DatabaseProvider.PostgreSql => TableExistsPostgreSql(
                    dbContext,
                    schemaName,
                    nameToCheck
                ),
                DatabaseProvider.SqlServer => TableExistsSqlServer(
                    dbContext,
                    schemaName,
                    nameToCheck
                ),
                _ => TableExistsSqlite(dbContext, nameToCheck),
            };

            if (exists)
                return true;
        }

        return false;
    }

    private static bool TableExistsSqlite(DbContext dbContext, string tableName)
    {
        var count = dbContext.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type='table' AND name={0}",
            tableName
        );
        return count.Any(c => c > 0);
    }

    private static bool TableExistsPostgreSql(
        DbContext dbContext,
        string schemaName,
        string tableName
    )
    {
        var count = dbContext.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables WHERE table_schema={0} AND table_name={1}",
            schemaName,
            tableName
        );
        return count.Any(c => c > 0);
    }

    private static bool TableExistsSqlServer(
        DbContext dbContext,
        string schemaName,
        string tableName
    )
    {
        var count = dbContext.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA={0} AND TABLE_NAME={1}",
            schemaName,
            tableName
        );
        return count.Any(c => c > 0);
    }
}
