using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Agents.Module;
using SimpleModule.AuditLogs;
using SimpleModule.BackgroundJobs;
using SimpleModule.Chat;
using SimpleModule.Database;
using SimpleModule.Email;
using SimpleModule.FeatureFlags;
using SimpleModule.FileStorage;
using SimpleModule.Host;
using SimpleModule.Map;
using SimpleModule.OpenIddict;
using SimpleModule.Orders;
using SimpleModule.PageBuilder;
using SimpleModule.Permissions;
using SimpleModule.Products;
using SimpleModule.Rag.Module;
using SimpleModule.RateLimiting;
using SimpleModule.Settings;
using SimpleModule.Tenants;
using SimpleModule.Users;

namespace SimpleModule.Tests.Shared.Fixtures;

public partial class SimpleModuleWebApplicationFactory
{
    private bool _dbInitialized;

    private void EnsureDatabasesInitialized()
    {
        if (_dbInitialized)
            return;
        _dbInitialized = true;
        EnsureModuleDatabasesCreated();
    }

    private void EnsureModuleDatabasesCreated()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        // HostDbContext includes all entities with module prefixes — it creates all tables.
        sp.GetRequiredService<HostDbContext>().Database.EnsureCreated();
        // Some module contexts may need explicit table creation if EnsureCreated
        // returns false (database already has tables from HostDbContext startup).
        EnsureTablesCreated<UsersDbContext>(sp);
        EnsureTablesCreated<OrdersDbContext>(sp);
        EnsureTablesCreated<ProductsDbContext>(sp);
        EnsureTablesCreated<MapDbContext>(sp);
        EnsureTablesCreated<PageBuilderDbContext>(sp);
        EnsureTablesCreated<PermissionsDbContext>(sp);
        EnsureTablesCreated<SettingsDbContext>(sp);
        EnsureTablesCreated<AuditLogsDbContext>(sp);
        EnsureTablesCreated<FileStorageDbContext>(sp);
        EnsureTablesCreated<FeatureFlagsDbContext>(sp);
        EnsureTablesCreated<TenantsDbContext>(sp);
        EnsureTablesCreated<RagDbContext>(sp);
        EnsureTablesCreated<AgentsDbContext>(sp);
        EnsureTablesCreated<ChatDbContext>(sp);
        EnsureTablesCreated<BackgroundJobsDbContext>(sp);
        EnsureTablesCreated<RateLimitingDbContext>(sp);
        EnsureTablesCreated<EmailDbContext>(sp);
        EnsureTablesCreated<OpenIddictAppDbContext>(sp);
    }

    private static void EnsureTablesCreated<TContext>(IServiceProvider sp)
        where TContext : DbContext
    {
        var db = sp.GetRequiredService<TContext>();
        if (!db.Database.EnsureCreated())
        {
            // DB already exists but tables for this context may not.
            // Force table creation via the relational creator.
            try
            {
                db.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>()
                    ?.CreateTables();
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                // Tables already exist — ignore
            }
        }
    }

    private static void RemoveHostedService<TService>(IServiceCollection services)
        where TService : class
    {
        var descriptor = services.SingleOrDefault(d =>
            d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
            && d.ImplementationType == typeof(TService)
        );
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
    }

    private void ReplaceDbContext<TContext>(IServiceCollection services, bool useOpenIddict = false)
        where TContext : DbContext
    {
        // Remove ALL descriptors related to this DbContext's options
        var toRemove = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<TContext>)
                || (
                    d.ServiceType == typeof(DbContextOptions) && d.ImplementationFactory is not null
                )
            )
            .ToList();
        foreach (var descriptor in toRemove)
        {
            services.Remove(descriptor);
        }

        // Register fresh options that use the shared in-memory SQLite connection.
        // UseApplicationServiceProvider is required so that IdentityDbContext can resolve
        // IdentityOptions (e.g. SchemaVersion = Version3) during OnModelCreating.
        services.AddScoped(sp =>
        {
            var builder = new DbContextOptionsBuilder<TContext>();
            builder.UseSqlite(_connection);
            builder.UseApplicationServiceProvider(sp);
            builder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            if (useOpenIddict)
            {
                builder.UseOpenIddict();
            }
            return (DbContextOptions<TContext>)builder.Options;
        });
    }
}
