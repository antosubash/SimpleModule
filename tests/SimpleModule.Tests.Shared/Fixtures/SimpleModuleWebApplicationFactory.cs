using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.AuditLogs;
using SimpleModule.BackgroundJobs;
using SimpleModule.Database;
using SimpleModule.Email;
using SimpleModule.FeatureFlags;
using SimpleModule.FileStorage;
using SimpleModule.Host;
using SimpleModule.OpenIddict;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Permissions;
using SimpleModule.RateLimiting;
using SimpleModule.Settings;
using SimpleModule.Tenants;
using SimpleModule.Testing;
using SimpleModule.Users;

namespace SimpleModule.Tests.Shared.Fixtures;

public partial class SimpleModuleWebApplicationFactory : WebApplicationFactory<Program>
{
    // Shared in-memory SQLite connection kept open for the lifetime of the factory
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    // Wolverine.Sqlite rejects in-memory databases for the durable outbox/inbox,
    // so the test process gets a single file-based SQLite path under the OS temp
    // directory. Stored statically and set as an env var before Program.cs reads
    // configuration so the override beats appsettings.json in the default config
    // chain. Per-instance isolation would require sub-processes — overkill for
    // tests that already share an xUnit collection fixture.
    public static readonly string WolverineDbPath = Path.Combine(
        Path.GetTempPath(),
        $"wolverine-test-{Guid.NewGuid():N}.db"
    );

    static SimpleModuleWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            "Database__DefaultConnection",
            $"Data Source={WolverineDbPath}"
        );
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");
        builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Testing");

        builder.ConfigureServices(services =>
        {
            // Configure DatabaseOptions so module DbContexts can detect SQLite
            // and apply table prefixes in OnModelCreating
            services.Configure<DatabaseOptions>(opts =>
            {
                opts.DefaultConnection = "Data Source=:memory:";
                opts.Provider = "Sqlite";
            });

            ReplaceDbContext<HostDbContext>(services, useOpenIddict: true);
            ReplaceDbContext<UsersDbContext>(services);
            ReplaceDbContext<PermissionsDbContext>(services);
            ReplaceDbContext<SettingsDbContext>(services);
            ReplaceDbContext<AuditLogsDbContext>(services);
            ReplaceDbContext<FileStorageDbContext>(services);
            ReplaceDbContext<FeatureFlagsDbContext>(services);
            ReplaceDbContext<TenantsDbContext>(services);
            ReplaceDbContext<BackgroundJobsDbContext>(services);
            ReplaceDbContext<RateLimitingDbContext>(services);
            ReplaceDbContext<EmailDbContext>(services);
            ReplaceDbContext<OpenIddictAppDbContext>(services, useOpenIddict: true);

            // Remove hosted seed services — they need real DB tables that
            // EnsureCreated on HostDbContext alone won't produce for module contexts.
            RemoveHostedService<SimpleModule.OpenIddict.Services.OpenIddictSeedService>(services);
            RemoveHostedService<SimpleModule.Permissions.Services.PermissionSeedService>(services);
            RemoveHostedService<SimpleModule.Users.Services.UserSeedService>(services);
            RemoveHostedService<SimpleModule.AuditLogs.Pipeline.AuditWriterService>(services);
            RemoveHostedService<SimpleModule.AuditLogs.Retention.AuditRetentionService>(services);
            RemoveHostedService<SimpleModule.FeatureFlags.FeatureFlagSyncService>(services);
            // Email recurring job registration runs during startup and accesses the
            // BackgroundJobs DB; remove it in tests to avoid table-not-found errors.
            RemoveHostedService<SimpleModule.Email.Jobs.EmailJobRegistrationHostedService>(
                services
            );

            // Add test authentication scheme that bypasses OpenIddict validation
            services.AddTestAuthentication();

            services.PostConfigure<PolicySchemeOptions>(
                AuthConstants.SmartAuthPolicy,
                options =>
                {
                    var fallbackSelector = options.ForwardDefaultSelector;
                    options.ForwardDefaultSelector = context =>
                    {
                        if (context.Request.Headers.ContainsKey(TestAuthDefaults.ClaimsHeader))
                            return TestAuthDefaults.AuthenticationScheme;

                        return fallbackSelector?.Invoke(context);
                    };
                }
            );
        });
    }

    // xUnit treats *any* exception from a fixture's Dispose/DisposeAsync as a
    // test-pipeline failure that fails the process even when every assertion
    // passed. On Linux CI runners Wolverine's host shutdown occasionally throws
    // because the durability tables/connection are torn down out from under its
    // polling agents — that's not a real test failure, just a cleanup race, so
    // swallow it. Anything that genuinely needs to fail tests should assert
    // before Dispose runs.

    protected override void Dispose(bool disposing)
    {
#pragma warning disable CA1031
        try
        {
            base.Dispose(disposing);
        }
        catch
        { /* ignore host-shutdown noise */
        }

        if (disposing)
        {
            try
            {
                _connection.Dispose();
            }
            catch
            { /* ignore */
            }
            TryDeleteWolverineDb();
        }
#pragma warning restore CA1031
    }

    public override async ValueTask DisposeAsync()
    {
#pragma warning disable CA1031
        try
        {
            await base.DisposeAsync();
        }
        catch
        { /* ignore host-shutdown noise */
        }

        try
        {
            await _connection.DisposeAsync();
        }
        catch
        { /* ignore */
        }
        TryDeleteWolverineDb();
        GC.SuppressFinalize(this);
#pragma warning restore CA1031
    }

    private static void TryDeleteWolverineDb()
    {
        try
        {
            File.Delete(WolverineDbPath);
        }
#pragma warning disable CA1031
        catch
        {
            // SQLite may briefly hold the file after host shutdown; leaking
            // an empty temp DB is acceptable for test cleanup.
        }
#pragma warning restore CA1031
    }
}
