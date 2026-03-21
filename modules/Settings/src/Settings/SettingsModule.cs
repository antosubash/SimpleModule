using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Settings.Services;

namespace SimpleModule.Settings;

[Module(
    SettingsConstants.ModuleName,
    RoutePrefix = SettingsConstants.RoutePrefix,
    ViewPrefix = "/settings"
)]
public class SettingsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddModuleDbContext<SettingsDbContext>(configuration, SettingsConstants.ModuleName);
        services.AddScoped<PublicMenuService>();
        services.AddScoped<IPublicMenuProvider>(sp => sp.GetRequiredService<PublicMenuService>());
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Settings",
                Url = "/settings",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/><circle cx="12" cy="12" r="3"/></svg>""",
                Order = 90,
                Section = MenuSection.AppSidebar,
            }
        );

        menus.Add(
            new MenuItem
            {
                Label = "Menus",
                Url = "/settings/menus",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5"/></svg>""",
                Order = 91,
                Section = MenuSection.AdminSidebar,
            }
        );
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(
                new SettingDefinition
                {
                    Key = "app.title",
                    DisplayName = "Application Title",
                    Group = "General",
                    Scope = SettingScope.Application,
                    DefaultValue = "\"SimpleModule\"",
                    Type = SettingType.Text,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "app.theme",
                    DisplayName = "Theme",
                    Description = "Default color theme for the application",
                    Group = "Appearance",
                    Scope = SettingScope.User,
                    DefaultValue = "\"light\"",
                    Type = SettingType.Text,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "app.language",
                    DisplayName = "Language",
                    Description = "Default language for the application",
                    Group = "General",
                    Scope = SettingScope.User,
                    DefaultValue = "\"en\"",
                    Type = SettingType.Text,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "app.timezone",
                    DisplayName = "Timezone",
                    Group = "General",
                    Scope = SettingScope.Application,
                    DefaultValue = "\"UTC\"",
                    Type = SettingType.Text,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "system.maintenance_mode",
                    DisplayName = "Maintenance Mode",
                    Description =
                        "When enabled, the application shows a maintenance page to non-admin users",
                    Group = "System",
                    Scope = SettingScope.System,
                    DefaultValue = "false",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "system.registration_enabled",
                    DisplayName = "User Registration",
                    Description = "Allow new users to register",
                    Group = "System",
                    Scope = SettingScope.System,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            );
    }
}
