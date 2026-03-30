using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Hosting;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.OpenIddict.Hosting;
using SimpleModule.OpenIddict.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SimpleModule.OpenIddict;

[Module(OpenIddictModuleConstants.ModuleName, ViewPrefix = "/openiddict")]
public class OpenIddictModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // DbContext with OpenIddict EF Core extension
        // Note: OpenIddict manages its own tables internally (no public DbSet<T> properties).
        // The unified HostDbContext also calls UseOpenIddict() for EF Core migrations.
        services.AddModuleDbContext<OpenIddictAppDbContext>(
            configuration,
            OpenIddictModuleConstants.ModuleName,
            opts => opts.UseOpenIddict()
        );

        // OpenIddict
        services
            .AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<OpenIddictAppDbContext>();
            })
            .AddServer(options =>
            {
                options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();

                options.AllowRefreshTokenFlow();

                options
                    .SetAuthorizationEndpointUris(ConnectRouteConstants.ConnectAuthorize)
                    .SetTokenEndpointUris(ConnectRouteConstants.ConnectToken)
                    .SetEndSessionEndpointUris(ConnectRouteConstants.ConnectEndSession)
                    .SetUserInfoEndpointUris(ConnectRouteConstants.ConnectUserInfo);

                var encryptionCertPath = configuration[ConfigKeys.OpenIddictEncryptionCertPath];
                var signingCertPath = configuration[ConfigKeys.OpenIddictSigningCertPath];

                if (
                    !string.IsNullOrEmpty(encryptionCertPath)
                    && !string.IsNullOrEmpty(signingCertPath)
                )
                {
                    // Production: use real certificates
                    var certPassword = configuration[ConfigKeys.OpenIddictCertPassword];
                    options.AddEncryptionCertificate(
                        System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
                            encryptionCertPath,
                            certPassword
                        )
                    );
                    options.AddSigningCertificate(
                        System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
                            signingCertPath,
                            certPassword
                        )
                    );
                }
                else
                {
                    // Development/Testing: use auto-generated development certificates
                    options
                        .AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }

                options.RegisterScopes(
                    AuthConstants.OpenIdScope,
                    AuthConstants.ProfileScope,
                    AuthConstants.EmailScope,
                    AuthConstants.RolesScope
                );

                options
                    .UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Seed service
        services.AddHostedService<OpenIddictSeedService>();

        // Session management contracts
        services.AddScoped<IOpenIddictSessionContracts, OpenIddictSessionService>();

        // Host-level contributions
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, OpenIddictSwaggerGenSetup>();
        services.AddTransient<IConfigureOptions<SwaggerUIOptions>, OpenIddictSwaggerUISetup>();
        services.AddSingleton<IHostDbContextContributor, OpenIddictDbContextContributor>();
        OpenIddictAuthSetup.AddSmartAuthentication(services);
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "OAuth Clients",
                Url = "/openiddict/clients",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"/></svg>""",
                Order = 20,
                Section = MenuSection.AdminSidebar,
            }
        );
    }
}
