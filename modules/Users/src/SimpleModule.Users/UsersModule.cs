using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
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

        services
            .AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<IdentityPasskeyOptions>(configuration.GetSection("Passkeys"));

        // Opt into Identity Schema Version 3 to enable the AspNetUserPasskeys table
        services.Configure<IdentityOptions>(options =>
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3
        );

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        });

        // Bridge UsersModuleOptions into ASP.NET Identity options
        services.AddSingleton<IPostConfigureOptions<IdentityOptions>, ApplyUsersModuleOptions>();

        services.AddHostedService<UserSeedService>();
        services.AddSingleton<IEmailSender<ApplicationUser>, ConsoleEmailSender>();
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

        // App sidebar item
        menus.Add(
            new MenuItem
            {
                Label = "Account",
                Url = "/Identity/Account/Manage",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/></svg>""",
                Order = 80,
                Section = MenuSection.AppSidebar,
            }
        );
    }
}
