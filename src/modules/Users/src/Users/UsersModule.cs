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
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Endpoints.Admin;
using SimpleModule.Users.Endpoints.Connect;
using SimpleModule.Users.Endpoints.Users;
using SimpleModule.Users.Entities;
using SimpleModule.Users.Services;

namespace SimpleModule.Users;

[Module(UsersConstants.ModuleName, RoutePrefix = UsersConstants.RoutePrefix)]
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

    public void ConfigureMenu(IMenuBuilder menus)
    {
        // Navbar items
        menus.Add(
            new MenuItem
            {
                Label = "Users",
                Url = "/admin/users",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/></svg>""",
                Order = 20,
                Section = MenuSection.Navbar,
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Roles",
                Url = "/admin/roles",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>""",
                Order = 21,
                Section = MenuSection.Navbar,
            }
        );

        // User dropdown items
        menus.Add(
            new MenuItem
            {
                Label = "Account Settings",
                Url = "/Identity/Account/Manage",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/></svg>""",
                Order = 10,
                Section = MenuSection.UserDropdown,
                Group = "account",
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Email",
                Url = "/Identity/Account/Manage/Email",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/></svg>""",
                Order = 11,
                Section = MenuSection.UserDropdown,
                Group = "account",
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Security",
                Url = "/Identity/Account/Manage/TwoFactorAuthentication",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>""",
                Order = 12,
                Section = MenuSection.UserDropdown,
                Group = "account",
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Manage Users",
                Url = "/admin/users",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/></svg>""",
                Order = 30,
                Section = MenuSection.UserDropdown,
                Group = "admin",
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Manage Roles",
                Url = "/admin/roles",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>""",
                Order = 31,
                Section = MenuSection.UserDropdown,
                Group = "admin",
            }
        );
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // OpenIddict Connect endpoints (token endpoint handled automatically by OpenIddict)
        AuthorizationEndpoint.Map(endpoints);
        LogoutEndpoint.Map(endpoints);
        UserinfoEndpoint.Map(endpoints);

        // User API endpoints
        var usersGroup = endpoints.MapGroup(UsersConstants.RoutePrefix);
        GetAllEndpoint.Map(usersGroup);
        GetByIdEndpoint.Map(usersGroup);
        GetCurrentEndpoint.Map(usersGroup);

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
