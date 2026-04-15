using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
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
using SimpleModule.OpenIddict.Contracts;
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

public partial class SimpleModuleWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestAuthScheme = "TestScheme";

    // Shared in-memory SQLite connection kept open for the lifetime of the factory
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

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
            ReplaceDbContext<OrdersDbContext>(services);
            ReplaceDbContext<ProductsDbContext>(services);
            // Map module: in-memory SQLite has no SpatiaLite native lib, so disable
            // the geometry columns globally for the test fixture. Production providers
            // (PostGIS, SQL Server, SpatiaLite when installed) still get the spatial
            // columns because the static defaults to true.
            SimpleModule.Map.EntityConfigurations.LayerSourceConfiguration.EnableSpatial = false;
            SimpleModule.Map.EntityConfigurations.SavedMapConfiguration.EnableSpatial = false;
            ReplaceDbContext<MapDbContext>(services);
            ReplaceDbContext<PageBuilderDbContext>(services);
            ReplaceDbContext<PermissionsDbContext>(services);
            ReplaceDbContext<SettingsDbContext>(services);
            ReplaceDbContext<AuditLogsDbContext>(services);
            ReplaceDbContext<FileStorageDbContext>(services);
            ReplaceDbContext<FeatureFlagsDbContext>(services);
            ReplaceDbContext<TenantsDbContext>(services);
            ReplaceDbContext<RagDbContext>(services);
            ReplaceDbContext<AgentsDbContext>(services);
            ReplaceDbContext<ChatDbContext>(services);
            ReplaceDbContext<SimpleModule.Datasets.DatasetsDbContext>(services);
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
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthScheme;
                    options.DefaultChallengeScheme = TestAuthScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthScheme, _ => { });

            services.PostConfigure<PolicySchemeOptions>(
                AuthConstants.SmartAuthPolicy,
                options =>
                {
                    var fallbackSelector = options.ForwardDefaultSelector;
                    options.ForwardDefaultSelector = context =>
                    {
                        if (context.Request.Headers.ContainsKey("X-Test-Claims"))
                            return TestAuthScheme;

                        return fallbackSelector?.Invoke(context);
                    };
                }
            );
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
