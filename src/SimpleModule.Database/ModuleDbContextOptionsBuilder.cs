using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SimpleModule.Database;

public static class ModuleDbContextOptionsBuilder
{
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string moduleName,
        Action<DbContextOptionsBuilder>? configureOptions = null
    )
        where TContext : DbContext
    {
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));

        var dbOptions =
            configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();

        var connectionString = dbOptions.ModuleConnections.TryGetValue(moduleName, out var moduleCs)
            ? moduleCs
            : dbOptions.DefaultConnection;

        var provider = DatabaseProviderDetector.Detect(connectionString);

        services.AddDbContext<TContext>(options =>
        {
            switch (provider)
            {
                case DatabaseProvider.PostgreSql:
                    options.UseNpgsql(connectionString);
                    break;
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(connectionString);
                    break;
                default:
                    options.UseSqlite(connectionString);
                    break;
            }

            configureOptions?.Invoke(options);
        });

        services.AddSingleton(new ModuleDbContextInfo(moduleName, typeof(TContext)));

        return services;
    }
}
