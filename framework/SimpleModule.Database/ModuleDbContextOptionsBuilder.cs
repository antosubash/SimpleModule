using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

                // Wire up SaveChanges interceptors via a lazy forwarder that resolves
                // the real interceptors per-scope on first use. Eagerly resolving
                // ISaveChangesInterceptor here would cause a circular dependency
                // deadlock when an interceptor's constructor depends on a service
                // that depends on a DbContext (e.g., AuditSaveChangesInterceptor
                // → ISettingsContracts → SettingsService → SettingsDbContext).
                options.AddInterceptors(new DeferredInterceptorForwarder(sp));
            }
        );

        services.AddSingleton(new ModuleDbContextInfo(moduleName, typeof(TContext)));

        return services;
    }
}

/// <summary>
/// Forwards SaveChanges interception to all <see cref="ISaveChangesInterceptor"/>
/// instances registered in DI, resolving them lazily on first use rather than
/// during DbContext options construction. This breaks the circular dependency
/// chain that occurs when an interceptor depends on a service that depends on
/// a DbContext.
/// </summary>
internal sealed class DeferredInterceptorForwarder(IServiceProvider rootProvider)
    : SaveChangesInterceptor
{
    private ISaveChangesInterceptor[]? _interceptors;

    private ISaveChangesInterceptor[] GetInterceptors()
    {
        return _interceptors ??= rootProvider
            .GetServices<ISaveChangesInterceptor>()
            .ToArray();
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var interceptor in GetInterceptors())
        {
            result = await interceptor.SavingChangesAsync(eventData, result, cancellationToken);
        }

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var interceptor in GetInterceptors())
        {
            result = await interceptor.SavedChangesAsync(eventData, result, cancellationToken);
        }

        return result;
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var interceptor in GetInterceptors())
        {
            await interceptor.SaveChangesFailedAsync(eventData, cancellationToken);
        }
    }
}
