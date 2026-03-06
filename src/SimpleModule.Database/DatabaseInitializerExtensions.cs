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
                // Shared database already created — create this module's tables
                var creator = dbContext.GetService<IRelationalDatabaseCreator>();
                creator.CreateTables();
            }
        }
    }
}
