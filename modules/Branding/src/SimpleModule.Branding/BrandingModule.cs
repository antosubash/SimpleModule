using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;

namespace SimpleModule.Branding;

[Module("Branding", ViewPrefix = "/branding")]
public class BrandingModule : IModule
{
    private const string Icon =
        """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M7 21a4 4 0 01-4-4V5a2 2 0 012-2h4a2 2 0 012 2v12a4 4 0 01-4 4zm0 0h12a2 2 0 002-2v-4a2 2 0 00-2-2h-2.343M11 7.343l1.657-1.657a2 2 0 012.828 0l2.829 2.829a2 2 0 010 2.828l-8.486 8.485M7 17h.01"/></svg>""";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IBrandingContracts, BrandingService>();
        services.AddScoped<IInertiaHeadContributor, BrandingHeadContributor>();
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        app.UseMiddleware<BrandingSharedDataMiddleware>();
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder) =>
        builder.AddPermissions<BrandingPermissions>();

    public void ConfigureMenu(IMenuBuilder menus) =>
        menus.Add(
            new MenuItem
            {
                Label = "Branding",
                Url = "/branding/manage",
                Icon = Icon,
                Order = 86,
                Section = MenuSection.AppSidebar,
                Roles = ["Admin"],
                RequiredPermission = BrandingPermissions.Manage,
            }
        );

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(
                Def(
                    BrandingSettingKeys.AppName,
                    "Application name",
                    SettingType.Text,
                    JsonSerializer.Serialize(BrandingDefaults.AppName),
                    order: 0
                )
            )
            .Add(
                Def(
                    BrandingSettingKeys.ColorPrimary,
                    "Primary color (light)",
                    SettingType.Color,
                    JsonSerializer.Serialize(BrandingDefaults.ColorPrimary),
                    order: 1
                )
            )
            .Add(
                Def(
                    BrandingSettingKeys.ColorPrimaryDark,
                    "Primary color (dark)",
                    SettingType.Color,
                    JsonSerializer.Serialize(BrandingDefaults.ColorPrimaryDark),
                    order: 2
                )
            )
            .Add(
                Def(
                    BrandingSettingKeys.CustomCss,
                    "Custom CSS",
                    SettingType.MultilineText,
                    "\"\"",
                    order: 3
                )
            )
            .Add(
                Def(
                    BrandingSettingKeys.LogoFileId,
                    "Logo file id",
                    SettingType.Text,
                    "\"\"",
                    order: 4
                )
            )
            .Add(
                Def(
                    BrandingSettingKeys.FaviconFileId,
                    "Favicon file id",
                    SettingType.Text,
                    "\"\"",
                    order: 5
                )
            )
            .Add(
                Def(
                    BrandingSettingKeys.TopBar,
                    "Top bar",
                    SettingType.Json,
                    JsonSerializer.Serialize(new TopBarConfig()),
                    order: 6
                )
            )
            .Add(
                Def(
                    BrandingSettingKeys.Footer,
                    "Footer",
                    SettingType.Json,
                    JsonSerializer.Serialize(new FooterConfig()),
                    order: 7
                )
            );
    }

    private static SettingDefinition Def(
        string key,
        string name,
        SettingType type,
        string defaultValue,
        int order
    ) =>
        new()
        {
            Key = key,
            DisplayName = name,
            Group = "Branding",
            Scope = SettingScope.Application,
            Type = type,
            DefaultValue = defaultValue,
            Order = order,
        };
}
