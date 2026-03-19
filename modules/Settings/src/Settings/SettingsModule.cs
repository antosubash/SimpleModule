using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Settings.Contracts;

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
        services.AddModuleDbContext<SettingsDbContext>(configuration, SettingsConstants.ModuleName);
        services.AddScoped<ISettingsContracts, SettingsService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Settings",
                Url = "/settings",
                Icon = "Settings",
                Order = 90,
                Section = MenuSection.AppSidebar,
            }
        );
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(new SettingDefinition
            {
                Key = "app.title",
                DisplayName = "Application Title",
                Group = "General",
                Scope = SettingScope.Application,
                DefaultValue = "\"SimpleModule\"",
                Type = SettingType.Text,
            })
            .Add(new SettingDefinition
            {
                Key = "app.theme",
                DisplayName = "Theme",
                Description = "Default color theme for the application",
                Group = "Appearance",
                Scope = SettingScope.User,
                DefaultValue = "\"light\"",
                Type = SettingType.Text,
            })
            .Add(new SettingDefinition
            {
                Key = "app.language",
                DisplayName = "Language",
                Description = "Default language for the application",
                Group = "General",
                Scope = SettingScope.User,
                DefaultValue = "\"en\"",
                Type = SettingType.Text,
            })
            .Add(new SettingDefinition
            {
                Key = "app.timezone",
                DisplayName = "Timezone",
                Group = "General",
                Scope = SettingScope.Application,
                DefaultValue = "\"UTC\"",
                Type = SettingType.Text,
            })
            .Add(new SettingDefinition
            {
                Key = "system.maintenance_mode",
                DisplayName = "Maintenance Mode",
                Description = "When enabled, the application shows a maintenance page to non-admin users",
                Group = "System",
                Scope = SettingScope.System,
                DefaultValue = "false",
                Type = SettingType.Bool,
            })
            .Add(new SettingDefinition
            {
                Key = "system.registration_enabled",
                DisplayName = "User Registration",
                Description = "Allow new users to register",
                Group = "System",
                Scope = SettingScope.System,
                DefaultValue = "true",
                Type = SettingType.Bool,
            });
    }
}
