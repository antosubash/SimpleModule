using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Hosting;

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
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseConstants.SectionName)
        );

        var dbOptions =
            configuration.GetSection(DatabaseConstants.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();

        var connectionString = dbOptions.ModuleConnections.TryGetValue(moduleName, out var moduleCs)
            ? moduleCs
            : dbOptions.DefaultConnection;

        var provider = DatabaseProviderDetector.Detect(connectionString, dbOptions.Provider);

        services.AddDbContext<TContext>(
            (sp, options) =>
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

                // Suppress PendingModelChangesWarning caused by Vogen ConfigureConventions
                // value converters that don't change the actual schema
                options.ConfigureWarnings(w =>
                    w.Ignore(RelationalEventId.PendingModelChangesWarning)
                );

                configureOptions?.Invoke(options);

                // Auto-resolve host DbContext contributors (e.g., OpenIddict's UseOpenIddict())
                if (moduleName == DatabaseConstants.HostModuleName)
                {
                    var contributors = sp.GetServices<IHostDbContextContributor>();
                    foreach (var contributor in contributors)
                    {
                        contributor.Configure(options);
                    }
                }

                // Resolve SaveChanges interceptors lazily at first use rather than during
                // DbContext options construction. This avoids circular dependency issues
                // when an interceptor's constructor depends on a service that depends on
                // a DbContext (e.g., SaveChangesInterceptor -> IServiceA -> ServiceA
                // -> SomeDbContext). Interceptors should resolve runtime dependencies at
                // interception time via eventData.Context.GetInfrastructure().GetService<T>().
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            }
        );

        services.AddSingleton(new ModuleDbContextInfo(moduleName, typeof(TContext)));

        return services;
    }
}
