using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleModule.Core;
using SimpleModule.Core.Constants;
using SimpleModule.Database;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Entities;
using SimpleModule.Users.Features.Admin;
using SimpleModule.Users.Features.Connect;
using SimpleModule.Users.Features.GetAllUsers;
using SimpleModule.Users.Features.GetCurrentUser;
using SimpleModule.Users.Features.GetUserById;
using SimpleModule.Users.Services;

namespace SimpleModule.Users;

[Module(UsersConstants.ModuleName)]
public class UsersModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // DbContext with OpenIddict EF Core extension
        services.AddModuleDbContext<UsersDbContext>(
            configuration,
            UsersConstants.ModuleName,
            opts => opts.UseOpenIddict()
        );

        // ASP.NET Core Identity
        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddDefaultTokenProviders();

        // OpenIddict
        services
            .AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<UsersDbContext>();
            })
            .AddServer(options =>
            {
                options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();

                options.AllowRefreshTokenFlow();

                options
                    .SetAuthorizationEndpointUris(RouteConstants.ConnectAuthorize)
                    .SetTokenEndpointUris(RouteConstants.ConnectToken)
                    .SetEndSessionEndpointUris(RouteConstants.ConnectEndSession)
                    .SetUserInfoEndpointUris(RouteConstants.ConnectUserInfo);

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

        // Email sender (console-based for development)
        services.AddSingleton<IEmailSender<ApplicationUser>, ConsoleEmailSender>();

        // Services
        services.AddScoped<IUserContracts, UserService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // OpenIddict Connect endpoints (token endpoint handled automatically by OpenIddict)
        AuthorizationEndpoint.Map(endpoints);
        LogoutEndpoint.Map(endpoints);
        UserinfoEndpoint.Map(endpoints);

        // User API endpoints
        var usersGroup = endpoints.MapGroup(UsersConstants.RoutePrefix);
        GetAllUsersEndpoint.Map(usersGroup);
        GetUserByIdEndpoint.Map(usersGroup);
        GetCurrentUserEndpoint.Map(usersGroup);

        // Download personal data endpoint (cannot be a Blazor component — returns a file)
        usersGroup
            .MapPost(
                UsersConstants.DownloadPersonalDataRoute,
                async (
                    HttpContext context,
                    UserManager<ApplicationUser> userManager,
                    ILogger<UsersModule> logger
                ) =>
                {
                    var user = await userManager.GetUserAsync(context.User);
                    if (user is null)
                    {
                        return Results.NotFound();
                    }

                    logger.LogInformation("User asked for their personal data.");

                    // Manually enumerate personal data properties (AOT-compatible, no reflection)
                    var personalData = new Dictionary<string, string>
                    {
                        [PersonalDataKeys.Id] = user.Id ?? PersonalDataKeys.NullPlaceholder,
                        [PersonalDataKeys.UserName] =
                            user.UserName ?? PersonalDataKeys.NullPlaceholder,
                        [PersonalDataKeys.Email] = user.Email ?? PersonalDataKeys.NullPlaceholder,
                        [PersonalDataKeys.PhoneNumber] =
                            user.PhoneNumber ?? PersonalDataKeys.NullPlaceholder,
                        [PersonalDataKeys.DisplayName] = user.DisplayName,
                        [PersonalDataKeys.CreatedAt] = user.CreatedAt.ToString("O"),
                        [PersonalDataKeys.LastLoginAt] =
                            user.LastLoginAt?.ToString("O") ?? PersonalDataKeys.NullPlaceholder,
                    };

                    var logins = await userManager.GetLoginsAsync(user);
                    foreach (var l in logins)
                    {
                        personalData.Add(
                            $"{l.LoginProvider} {PersonalDataKeys.ExternalLoginSuffix}",
                            l.ProviderKey
                        );
                    }

                    personalData.Add(
                        PersonalDataKeys.AuthenticatorKey,
                        await userManager.GetAuthenticatorKeyAsync(user) ?? ""
                    );

                    return Results.File(
                        JsonSerializer.SerializeToUtf8Bytes(personalData),
                        PersonalDataKeys.PersonalDataContentType,
                        PersonalDataKeys.PersonalDataFileName
                    );
                }
            )
            .RequireAuthorization();

        // Admin endpoints
        AdminUsersEndpoint.Map(endpoints);
        AdminRolesEndpoint.Map(endpoints);
    }
}
