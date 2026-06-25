using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;

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

    // Branding values are stored via ISettingsContracts by key but intentionally NOT
    // registered as SettingDefinitions: they are an implementation detail edited through
    // the dedicated /branding/manage page, not the generic Settings admin UI. Defaults
    // live in BrandingDefaults / BrandingService. (SettingsService stores/reads keys
    // without a definition; it only validates when one exists.)
}
