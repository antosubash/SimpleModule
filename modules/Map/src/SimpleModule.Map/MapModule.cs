using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Map.Contracts;
using SimpleModule.Map.EntityConfigurations;

namespace SimpleModule.Map;

[Module(
    MapConstants.ModuleName,
    RoutePrefix = MapConstants.RoutePrefix,
    ViewPrefix = MapConstants.ViewPrefix
)]
public class MapModule : IModule, IModuleServices, IModuleMenu, IModuleSettings
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Spatial columns (PostGIS / SQL Server geometry / SpatiaLite) are opt-in:
        // they require a provider configured with NetTopologySuite. Default off so
        // vanilla SQLite (no mod_spatialite) and other plain providers continue to
        // work. Hosts opt in via "Modules:Map:EnableSpatial": true in appsettings.
        var enableSpatial = configuration.GetValue<bool>("Modules:Map:EnableSpatial");
        LayerSourceConfiguration.EnableSpatial = enableSpatial;
        SavedMapConfiguration.EnableSpatial = enableSpatial;

        services.AddModuleDbContext<MapDbContext>(
            configuration,
            MapConstants.ModuleName,
            enableSpatial: enableSpatial
        );
        services.AddScoped<IMapContracts, MapService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Map",
                Url = MapConstants.ViewPrefix,
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7"/></svg>""",
                Order = 40,
                Section = MenuSection.AppSidebar,
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Layer Sources",
                Url = MapConstants.ViewPrefix + "/layers",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 11H5m14-4H5m14 8H5m14 4H5"/></svg>""",
                Order = 41,
                Section = MenuSection.AppSidebar,
            }
        );
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(
                new SettingDefinition
                {
                    Key = MapConstants.SettingKeys.EnableMeasureTools,
                    DisplayName = "Enable measure tools",
                    Description = "Show the distance / area measure tools in the map viewer.",
                    Group = "Map",
                    Scope = SettingScope.Application,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = MapConstants.SettingKeys.EnableExportPng,
                    DisplayName = "Enable PNG export",
                    Description = "Show the canvas-to-PNG export button in the map viewer.",
                    Group = "Map",
                    Scope = SettingScope.Application,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = MapConstants.SettingKeys.EnableGeolocate,
                    DisplayName = "Enable geolocate control",
                    Description =
                        "Show the browser geolocation control that centers the map on the user's position.",
                    Group = "Map",
                    Scope = SettingScope.Application,
                    DefaultValue = "true",
                    Type = SettingType.Bool,
                }
            );
    }
}
