using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Services;

namespace SimpleModule.Users;

[Module(UsersConstants.ModuleName, ViewPrefix = "/Identity/Account")]
public class UsersModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<UsersDbContext>(configuration, UsersConstants.ModuleName);

        if (IsExternalIdentityProvider(configuration))
        {
            ConfigureExternalMode(services);
        }
        else
        {
            ConfigureLocalMode(services, configuration);
        }
    }

    private static bool IsExternalIdentityProvider(IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Identity:Provider");
        return string.Equals(provider, "Keycloak", StringComparison.OrdinalIgnoreCase);
    }

    private static void ConfigureLocalMode(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<IdentityPasskeyOptions>(configuration.GetSection("Passkeys"));

        services.Configure<IdentityOptions>(options =>
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3
        );

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";

            options.Events.OnRedirectToLogin = context =>
            {
                if (
                    context.Request.Path.StartsWithSegments(
                        "/api",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (
                    context.Request.Path.StartsWithSegments(
                        "/api",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        services.AddSingleton<IPostConfigureOptions<IdentityOptions>, ApplyUsersModuleOptions>();
        services.AddSingleton<
            IPostConfigureOptions<SecurityStampValidatorOptions>,
            ApplySecurityStampValidatorOptions
        >();

        services.AddScoped<IUserContracts, UserService>();
        services.AddScoped<IUserAdminContracts, UserAdminService>();
        services.AddScoped<IRoleAdminContracts, RoleAdminService>();
        services.AddHostedService<UserSeedService>();
        services.AddSingleton<IEmailSender<ApplicationUser>, ConsoleEmailSender>();
        services.AddSingleton<IAccountUnlockEmailSender, ConsoleAccountUnlockEmailSender>();
        services.AddSingleton<ISmsSender, ConsoleSmsSender>();
    }

    private static void ConfigureExternalMode(IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(options =>
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3
        );

        services.AddScoped<IUserContracts, ExternalUserService>();
        services.AddScoped<IUserAdminContracts, ExternalUserAdminService>();
        services.AddScoped<IRoleAdminContracts, ExternalRoleAdminService>();
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<UsersPermissions>();
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings.Add(
            new SettingDefinition
            {
                Key = ConfigKeys.ShowTestAccounts,
                DisplayName = "Show Test Accounts",
                Description = "Show quick-select buttons for test accounts on the login page",
                Group = "Authentication",
                Scope = SettingScope.System,
                DefaultValue = "true",
                Type = SettingType.Bool,
            }
        );
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
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
    }
}
