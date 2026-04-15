using Microsoft.Data.Sqlite;
using SimpleModule.Admin.Contracts;
using SimpleModule.AuditLogs;
using SimpleModule.FeatureFlags;
using SimpleModule.FileStorage;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Orders;
using SimpleModule.PageBuilder;
using SimpleModule.Products;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.LoadTests.Infrastructure;

/// <summary>
/// Load test factory using file-based SQLite with WAL mode and real OAuth Bearer tokens.
/// Adds ROPC (password) grant to OpenIddict, seeds a confidential client and admin user,
/// then acquires tokens via POST /connect/token.
/// </summary>
public partial class LoadTestWebApplicationFactory : SimpleModuleWebApplicationFactory
{
    private const string ServiceClientId = "loadtest-service";
    private const string ServiceClientSecret = "loadtest-secret-key-2024";
    private const string TestUserEmail = "loadtest@simplemodule.dev";
    private const string TestUserPassword = "LoadTest123!";
    private const string TestUserDisplayName = "Load Test User";

    private readonly string _dbPath;
    private bool _seeded;

    public LoadTestWebApplicationFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"loadtest_{Guid.NewGuid():N}.db");

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
        cmd.ExecuteNonQuery();
    }

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
                "Could not find SimpleModule.slnx to resolve Host content root."
            );

        Environment.SetEnvironmentVariable("DOTNET_CONTENTROOT", contentRoot);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            try
            {
                foreach (var suffix in new[] { "", "-wal", "-shm" })
                {
                    var path = _dbPath + suffix;
                    if (File.Exists(path))
                        File.Delete(path);
                }
            }
#pragma warning disable CA1031
            catch
            { /* Best-effort cleanup */
            }
#pragma warning restore CA1031
        }
    }

    private static readonly string[] AllPermissions =
    [
        ProductsPermissions.View,
        ProductsPermissions.Create,
        ProductsPermissions.Update,
        ProductsPermissions.Delete,
        OrdersPermissions.View,
        OrdersPermissions.Create,
        OrdersPermissions.Update,
        OrdersPermissions.Delete,
        AuditLogsPermissions.View,
        AuditLogsPermissions.Export,
        FileStoragePermissions.View,
        FileStoragePermissions.Upload,
        FileStoragePermissions.Delete,
        PageBuilderPermissions.View,
        PageBuilderPermissions.Create,
        PageBuilderPermissions.Update,
        PageBuilderPermissions.Delete,
        PageBuilderPermissions.Publish,
        AdminPermissions.ManageUsers,
        AdminPermissions.ManageRoles,
        OpenIddictPermissions.ManageClients,
        FeatureFlagsPermissions.View,
        FeatureFlagsPermissions.Manage,
    ];
}
