using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Admin;
using SimpleModule.Admin.Contracts;
using SimpleModule.AuditLogs;
using SimpleModule.Database;
using SimpleModule.FileStorage;
using SimpleModule.Host;
using SimpleModule.OpenIddict;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Orders;
using SimpleModule.PageBuilder;
using SimpleModule.Permissions;
using SimpleModule.Products;
using SimpleModule.Settings;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users;

namespace SimpleModule.LoadTests.Infrastructure;

/// <summary>
/// Load test factory that uses file-based SQLite with WAL mode for concurrent write support.
/// WAL (Write-Ahead Logging) allows multiple readers and a single writer simultaneously,
/// which is far better than the default journal mode for load testing.
/// </summary>
public class LoadTestWebApplicationFactory : SimpleModuleWebApplicationFactory
{
    private readonly string _dbPath;

    public LoadTestWebApplicationFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"loadtest_{Guid.NewGuid():N}.db");

        // Create the database file and enable WAL BEFORE any EF Core usage.
        // WAL mode persists on the file — all subsequent connections inherit it.
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Sets DOTNET_CONTENTROOT so HostApplicationBuilder.Initialize() inside Program.Main
    /// finds the correct Host project directory. Must be called before creating the factory.
    /// </summary>
    public static void EnsureContentRoot()
    {
        if (Environment.GetEnvironmentVariable("DOTNET_CONTENTROOT") is not null)
            return;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SimpleModule.slnx")))
        {
            dir = dir.Parent;
        }

        var contentRoot = dir is not null
            ? Path.Combine(dir.FullName, "template", "SimpleModule.Host")
            : throw new InvalidOperationException(
                "Could not find SimpleModule.slnx to resolve Host content root.");

        Environment.SetEnvironmentVariable("DOTNET_CONTENTROOT", contentRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Call base to set up test auth, environment, remove seed services, etc.
        base.ConfigureWebHost(builder);

        // Override the DbContext registrations to use file-based SQLite with WAL
        var connectionString = $"Data Source={_dbPath};Cache=Shared;Default Timeout=30";
        builder.ConfigureServices(services =>
        {
            services.Configure<DatabaseOptions>(opts =>
            {
                opts.DefaultConnection = connectionString;
                opts.Provider = "Sqlite";
            });

            ReplaceWithFileDb<HostDbContext>(services, connectionString, useOpenIddict: true);
            ReplaceWithFileDb<UsersDbContext>(services, connectionString);
            ReplaceWithFileDb<OrdersDbContext>(services, connectionString);
            ReplaceWithFileDb<ProductsDbContext>(services, connectionString);
            ReplaceWithFileDb<AdminDbContext>(services, connectionString);
            ReplaceWithFileDb<PageBuilderDbContext>(services, connectionString);
            ReplaceWithFileDb<PermissionsDbContext>(services, connectionString);
            ReplaceWithFileDb<SettingsDbContext>(services, connectionString);
            ReplaceWithFileDb<AuditLogsDbContext>(services, connectionString);
            ReplaceWithFileDb<FileStorageDbContext>(services, connectionString);
            ReplaceWithFileDb<OpenIddictAppDbContext>(services, connectionString, useOpenIddict: true);
        });
    }

    private static void ReplaceWithFileDb<TContext>(
        IServiceCollection services,
        string connectionString,
        bool useOpenIddict = false)
        where TContext : DbContext
    {
        // Remove existing registrations (from base factory's in-memory setup)
        var toRemove = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<TContext>)
                || (d.ServiceType == typeof(DbContextOptions) && d.ImplementationFactory is not null))
            .ToList();

        foreach (var descriptor in toRemove)
        {
            services.Remove(descriptor);
        }

        // Register with file-based SQLite. Each connection sets busy_timeout and WAL pragmas
        // to handle concurrent access under load.
        services.AddScoped(sp =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<TContext>();
            optionsBuilder.UseSqlite(connectionString, sqliteOpts =>
            {
                sqliteOpts.CommandTimeout(30);
            });
            optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            optionsBuilder.AddInterceptors(SqliteBusyTimeoutInterceptor.Instance);
            if (useOpenIddict)
            {
                optionsBuilder.UseOpenIddict();
            }
            return (DbContextOptions<TContext>)optionsBuilder.Options;
        });
    }

    public HttpClient CreateServiceAccountClient()
    {
        return CreateServiceAccountClientWithUserId("service-account");
    }

    /// <summary>
    /// Creates an authenticated client whose NameIdentifier matches a real Identity user.
    /// Use this for endpoints that look up the current user via UserManager (e.g., /api/users/me).
    /// </summary>
    public HttpClient CreateServiceAccountClientWithUserId(string userId)
    {
        return CreateAuthenticatedClient(
            AllPermissions,
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "Load Test Service Account"),
            new Claim(ClaimTypes.Email, "loadtest@simplemodule.dev")
        );
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // Clean up the temp database file
            try
            {
                if (File.Exists(_dbPath))
                    File.Delete(_dbPath);

                var walPath = _dbPath + "-wal";
                if (File.Exists(walPath))
                    File.Delete(walPath);

                var shmPath = _dbPath + "-shm";
                if (File.Exists(shmPath))
                    File.Delete(shmPath);
            }
#pragma warning disable CA1031 // Cleanup should not throw
            catch
            {
                // Best-effort cleanup
            }
#pragma warning restore CA1031
        }
    }

    private static readonly string[] AllPermissions =
    [
        ProductsPermissions.View, ProductsPermissions.Create,
        ProductsPermissions.Update, ProductsPermissions.Delete,
        OrdersPermissions.View, OrdersPermissions.Create,
        OrdersPermissions.Update, OrdersPermissions.Delete,
        AuditLogsPermissions.View, AuditLogsPermissions.Export,
        FileStoragePermissions.View, FileStoragePermissions.Upload, FileStoragePermissions.Delete,
        PageBuilderPermissions.View, PageBuilderPermissions.Create,
        PageBuilderPermissions.Update, PageBuilderPermissions.Delete, PageBuilderPermissions.Publish,
        AdminPermissions.ManageUsers, AdminPermissions.ManageRoles, AdminPermissions.ViewAuditLog,
        OpenIddictPermissions.ManageClients,
    ];
}
