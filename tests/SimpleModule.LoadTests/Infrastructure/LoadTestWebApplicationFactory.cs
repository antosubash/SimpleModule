using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using SimpleModule.Admin;
using SimpleModule.Admin.Contracts;
using SimpleModule.AuditLogs;
using SimpleModule.Core.Authorization;
using SimpleModule.Database;
using SimpleModule.FeatureFlags;
using SimpleModule.FileStorage;
using SimpleModule.Host;
using SimpleModule.OpenIddict;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Orders;
using SimpleModule.PageBuilder;
using SimpleModule.Permissions;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Products;
using SimpleModule.Settings;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users;
using SimpleModule.Users.Contracts;

namespace SimpleModule.LoadTests.Infrastructure;

/// <summary>
/// Load test factory using file-based SQLite with WAL mode and real OAuth Bearer tokens.
/// Adds ROPC (password) grant to OpenIddict, seeds a confidential client and admin user,
/// then acquires tokens via POST /connect/token.
/// </summary>
public class LoadTestWebApplicationFactory : SimpleModuleWebApplicationFactory
{
    private const string ServiceClientId = "loadtest-service";
    private const string ServiceClientSecret = "loadtest-secret-key-2024";
    private const string TestUserEmail = "loadtest@simplemodule.dev";
    private const string TestUserPassword = "LoadTest123!";
    private const string TestUserDisplayName = "Load Test User";

    private readonly string _dbPath;

    public LoadTestWebApplicationFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"loadtest_{Guid.NewGuid():N}.db");

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
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
                "Could not find SimpleModule.slnx to resolve Host content root.");

        Environment.SetEnvironmentVariable("DOTNET_CONTENTROOT", contentRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

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
            ReplaceWithFileDb<FeatureFlagsDbContext>(services, connectionString);
            ReplaceWithFileDb<OpenIddictAppDbContext>(services, connectionString, useOpenIddict: true);

            // Add ROPC (password) grant to OpenIddict for load test token acquisition.
            services.Configure<OpenIddictServerOptions>(opts =>
            {
                opts.GrantTypes.Add(OpenIddictConstants.GrantTypes.Password);
                opts.CodeChallengeMethods.Clear();
            });

            // Disable HTTPS requirement (TestServer uses HTTP)
            services.Configure<OpenIddictServerAspNetCoreOptions>(opts =>
            {
                opts.DisableTransportSecurityRequirement = true;
            });

            // Override the base factory's TestScheme — restore OpenIddict validation as default.
            var oidcScheme = global::OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = oidcScheme;
                    options.DefaultChallengeScheme = oidcScheme;
                });

            // Override the SmartAuth policy to always forward to OpenIddict validation
            services.PostConfigure<Microsoft.AspNetCore.Authentication.PolicySchemeOptions>(
                AuthConstants.SmartAuthPolicy,
                options =>
                {
                    options.ForwardDefaultSelector = _ => oidcScheme;
                });


            // Register a custom OpenIddict event handler for password grants
            services.AddOpenIddict().AddServer(opts =>
            {
                opts.AddEventHandler<OpenIddictServerEvents.HandleTokenRequestContext>(builder =>
                    builder.UseInlineHandler(PasswordGrantTokenHandler.Handle));
            });
        });
    }

    private static void ReplaceWithFileDb<TContext>(
        IServiceCollection services,
        string connectionString,
        bool useOpenIddict = false)
        where TContext : DbContext
    {
        var toRemove = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<TContext>)
                || (d.ServiceType == typeof(DbContextOptions) && d.ImplementationFactory is not null))
            .ToList();

        foreach (var descriptor in toRemove)
        {
            services.Remove(descriptor);
        }

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

    private bool _seeded;

    private async Task SeedTestInfrastructureAsync()
    {
        if (_seeded)
            return;
        _seeded = true;

        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        // 1. Seed the ROPC-capable OAuth client
        var appManager = sp.GetRequiredService<IOpenIddictApplicationManager>();
        if (await appManager.FindByClientIdAsync(ServiceClientId) is null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = ServiceClientId,
                ClientSecret = ServiceClientSecret,
                DisplayName = "Load Test Service Account",
                ClientType = OpenIddictConstants.ClientTypes.Confidential,
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Prefixes.Scope + AuthConstants.RolesScope,
                },
            });
        }

        // 2. Seed the Admin role
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "Admin",
                Description = "Load test admin role",
                CreatedAt = DateTime.UtcNow,
            });
        }

        // 3. Seed the test user
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(TestUserEmail);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = TestUserEmail,
                Email = TestUserEmail,
                DisplayName = TestUserDisplayName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
            };
            var result = await userManager.CreateAsync(user, TestUserPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }

        // 4. Assign all permissions to the user
        var permContracts = sp.GetRequiredService<IPermissionContracts>();
        var userId = UserId.From(user!.Id);
        await permContracts.SetPermissionsForUserAsync(userId, AllPermissions);
    }

    /// <summary>
    /// Acquires a real Bearer token via ROPC (password grant) from /connect/token.
    /// The token contains real claims: sub, email, name, roles, permissions.
    /// </summary>
    public async Task<HttpClient> CreateBearerClientAsync()
    {
        await SeedTestInfrastructureAsync();

        using var tokenClient = CreateClient();
        using var tokenRequest = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", ServiceClientId),
            new KeyValuePair<string, string>("client_secret", ServiceClientSecret),
            new KeyValuePair<string, string>("username", TestUserEmail),
            new KeyValuePair<string, string>("password", TestUserPassword),
            new KeyValuePair<string, string>("scope", "openid profile email roles"),
        ]);

        var tokenResponse = await tokenClient.PostAsync("/connect/token", tokenRequest);
        var body = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to acquire Bearer token: {tokenResponse.StatusCode} - {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var accessToken = doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Token response missing access_token");

        var client = CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>
    /// Gets the seeded test user's Identity ID.
    /// </summary>
    public async Task<string> GetSeededUserIdAsync()
    {
        await SeedTestInfrastructureAsync();

        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(TestUserEmail)
            ?? throw new InvalidOperationException("Seeded user not found");
        return user.Id;
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
            catch { /* Best-effort cleanup */ }
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
        FeatureFlagsPermissions.View, FeatureFlagsPermissions.Manage,
    ];
}
