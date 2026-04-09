using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Settings;
using SimpleModule.Map.Contracts;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Map.Pages;

/// <summary>
/// Renders the singleton default map. Layer sources, basemaps and tool flags are
/// preloaded so the React page can show the map and let users add/remove overlays
/// in a single screen — there is no longer a list of saved maps.
/// </summary>
public class BrowseEndpoint : IViewEndpoint
{
    public const string Route = MapConstants.Routes.Browse;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                async (
                    IMapContracts maps,
                    ISettingsContracts settings,
                    IOptions<MapModuleOptions> options
                ) =>
                {
                    var mapTask = maps.GetDefaultMapAsync();
                    var sourcesTask = maps.GetAllLayerSourcesAsync();
                    var basemapsTask = maps.GetAllBasemapsAsync();
                    var measureTask = settings.GetSettingAsync<bool?>(
                        MapConstants.SettingKeys.EnableMeasureTools,
                        SettingScope.Application
                    );
                    var exportTask = settings.GetSettingAsync<bool?>(
                        MapConstants.SettingKeys.EnableExportPng,
                        SettingScope.Application
                    );
                    var geolocateTask = settings.GetSettingAsync<bool?>(
                        MapConstants.SettingKeys.EnableGeolocate,
                        SettingScope.Application
                    );

                    return Inertia.Render(
                        "Map/Browse",
                        new
                        {
                            map = await mapTask,
                            sources = await sourcesTask,
                            basemaps = await basemapsTask,
                            defaultStyleUrl = options.Value.BaseStyleUrl,
                            maxLayers = options.Value.MaxLayersPerMap,
                            enableMeasure = await measureTask ?? true,
                            enableExportPng = await exportTask ?? true,
                            enableGeolocate = await geolocateTask ?? true,
                        }
                    );
                }
            )
            .AllowAnonymous();
    }
}
