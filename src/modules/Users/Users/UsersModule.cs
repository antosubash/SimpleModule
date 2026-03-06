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
using SimpleModule.Database;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Entities;
using SimpleModule.Users.Features.Connect;
using SimpleModule.Users.Features.GetAllUsers;
using SimpleModule.Users.Features.GetCurrentUser;
using SimpleModule.Users.Features.GetUserById;
using SimpleModule.Users.Services;

namespace SimpleModule.Users;

[Module("Users")]
public class UsersModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // DbContext with OpenIddict EF Core extension
        services.AddModuleDbContext<UsersDbContext>(
            configuration,
            "Users",
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
                    .SetAuthorizationEndpointUris("/connect/authorize")
                    .SetTokenEndpointUris("/connect/token")
                    .SetEndSessionEndpointUris("/connect/endsession")
                    .SetUserInfoEndpointUris("/connect/userinfo");

                options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();

                options.RegisterScopes("openid", "profile", "email", "roles");

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
        var usersGroup = endpoints.MapGroup("/api/users");
        GetAllUsersEndpoint.Map(usersGroup);
        GetUserByIdEndpoint.Map(usersGroup);
        GetCurrentUserEndpoint.Map(usersGroup);

        // Download personal data endpoint (cannot be a Blazor component — returns a file)
        usersGroup.MapPost(
            "/download-personal-data",
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

                var personalData = new Dictionary<string, string>();
                var personalDataProps = typeof(ApplicationUser)
                    .GetProperties()
                    .Where(prop =>
                        Attribute.IsDefined(prop, typeof(PersonalDataAttribute))
                    );
                foreach (var p in personalDataProps)
                {
                    personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
                }

                var logins = await userManager.GetLoginsAsync(user);
                foreach (var l in logins)
                {
                    personalData.Add(
                        $"{l.LoginProvider} external login provider key",
                        l.ProviderKey
                    );
                }

                personalData.Add(
                    "Authenticator Key",
                    await userManager.GetAuthenticatorKeyAsync(user) ?? ""
                );

                return Results.File(
                    JsonSerializer.SerializeToUtf8Bytes(personalData),
                    "application/json",
                    "PersonalData.json"
                );
            }
        ).RequireAuthorization();
    }
}
