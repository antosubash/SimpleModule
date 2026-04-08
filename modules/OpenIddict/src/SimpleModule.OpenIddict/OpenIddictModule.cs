using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Hosting;
using SimpleModule.Core.Inertia;
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

                // Enable password grant in Development for load testing (k6, etc.)
                if (configuration.GetValue<bool>("OpenIddict:AllowPasswordGrant"))
                {
                    options.AllowPasswordFlow();
                }

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
                    // Development/Testing: use ephemeral keys (avoids macOS keychain issues)
                    options.AddEphemeralEncryptionKey().AddEphemeralSigningKey();
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
                    .EnableTokenEndpointPassthrough()
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

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/oauth-callback", () => Inertia.Render("OpenIddict/OAuthCallback"))
            .AllowAnonymous();
    }

    // Menu items removed — accessible via Admin hub page
}
