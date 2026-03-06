using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SimpleModule.Database;

public static class DatabaseInitializerExtensions
{
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
                if (!ModuleTablesExist(dbContext, info.ModuleName))
                {
                    creator.CreateTables();
                }
            }
        }
    }

    private static bool ModuleTablesExist(DbContext dbContext, string moduleName)
    {
        var prefix = $"{moduleName}_";
        var entityTypes = dbContext.Model.GetEntityTypes();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
            {
                continue;
            }

            // Check for prefixed table (SQLite shared DB) or unprefixed (own DB)
            var nameToCheck = tableName.StartsWith(prefix, StringComparison.Ordinal)
                ? tableName
                : $"{prefix}{tableName}";

            var count = dbContext.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type='table' AND name={0}",
                nameToCheck
            );

            if (count.Any(c => c > 0))
            {
                return true;
            }
        }

        return false;
    }
}
