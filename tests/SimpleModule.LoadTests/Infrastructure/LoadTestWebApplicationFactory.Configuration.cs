using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
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
using SimpleModule.Products;
using SimpleModule.Settings;
using SimpleModule.Tenants;
using SimpleModule.Users;

namespace SimpleModule.LoadTests.Infrastructure;

public partial class LoadTestWebApplicationFactory
{
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
            ReplaceWithFileDb<PageBuilderDbContext>(services, connectionString);
            ReplaceWithFileDb<PermissionsDbContext>(services, connectionString);
            ReplaceWithFileDb<SettingsDbContext>(services, connectionString);
            ReplaceWithFileDb<AuditLogsDbContext>(services, connectionString);
            ReplaceWithFileDb<FileStorageDbContext>(services, connectionString);
            ReplaceWithFileDb<FeatureFlagsDbContext>(services, connectionString);
            ReplaceWithFileDb<TenantsDbContext>(services, connectionString);
            ReplaceWithFileDb<OpenIddictAppDbContext>(
                services,
                connectionString,
                useOpenIddict: true
            );

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
            var oidcScheme = global::OpenIddict
                .Validation
                .AspNetCore
                .OpenIddictValidationAspNetCoreDefaults
                .AuthenticationScheme;
            services.AddAuthentication(options =>
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
                }
            );

            // Register a custom OpenIddict event handler for password grants
            services
                .AddOpenIddict()
                .AddServer(opts =>
                {
                    opts.AddEventHandler<OpenIddictServerEvents.HandleTokenRequestContext>(
                        builder => builder.UseInlineHandler(PasswordGrantTokenHandler.Handle)
                    );
                });
        });
    }

    private static void ReplaceWithFileDb<TContext>(
        IServiceCollection services,
        string connectionString,
        bool useOpenIddict = false
    )
        where TContext : DbContext
    {
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

        services.AddScoped(sp =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<TContext>();
            optionsBuilder.UseSqlite(
                connectionString,
                sqliteOpts =>
                {
                    sqliteOpts.CommandTimeout(30);
                }
            );
            optionsBuilder.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning)
            );
            optionsBuilder.AddInterceptors(SqliteBusyTimeoutInterceptor.Instance);
            if (useOpenIddict)
            {
                optionsBuilder.UseOpenIddict();
            }
            return (DbContextOptions<TContext>)optionsBuilder.Options;
        });
    }
}
