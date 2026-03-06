using Microsoft.EntityFrameworkCore;

namespace SimpleModule.Database;

public static class ModuleModelBuilderExtensions
{
#pragma warning disable CA1308 // Schema names are conventionally lowercase in PostgreSQL/SQL Server
    public static void ApplyModuleSchema(
        this ModelBuilder modelBuilder,
        string moduleName,
        DatabaseOptions dbOptions
    )
    {
        var hasOwnConnection = dbOptions.ModuleConnections.ContainsKey(moduleName);
        if (hasOwnConnection)
            return;

        var connectionString = dbOptions.DefaultConnection;
        var provider = DatabaseProviderDetector.Detect(connectionString);

        if (provider == DatabaseProvider.Sqlite)
        {
            var prefix = $"{moduleName}_";
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entity.GetTableName();
                if (
                    tableName is not null
                    && !tableName.StartsWith(prefix, StringComparison.Ordinal)
                )
                {
                    entity.SetTableName($"{prefix}{tableName}");
                }
            }
        }
        else
        {
            var schema = moduleName.ToLowerInvariant();
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetSchema(schema);
            }
        }
    }
#pragma warning restore CA1308
}
