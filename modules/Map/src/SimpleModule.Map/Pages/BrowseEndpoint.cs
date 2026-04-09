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
                    var map = await maps.GetDefaultMapAsync();
                    var sources = await maps.GetAllLayerSourcesAsync();
                    var basemaps = await maps.GetAllBasemapsAsync();

                    // Fetch all three Map tool toggles in one query instead of three.
                    var toolSettings = await settings.GetSettingsAsync(
                        new SettingsFilter { Scope = SettingScope.Application, Group = "Map" }
                    );
                    var toolsByKey = toolSettings.ToDictionary(s => s.Key, s => s.Value);

                    return Inertia.Render(
                        "Map/Browse",
                        new
                        {
                            map,
                            sources,
                            basemaps,
                            defaultStyleUrl = options.Value.BaseStyleUrl,
                            maxLayers = options.Value.MaxLayersPerMap,
                            enableMeasure = ResolveBool(
                                toolsByKey,
                                MapConstants.SettingKeys.EnableMeasureTools
                            ),
                            enableExportPng = ResolveBool(
                                toolsByKey,
                                MapConstants.SettingKeys.EnableExportPng
                            ),
                            enableGeolocate = ResolveBool(
                                toolsByKey,
                                MapConstants.SettingKeys.EnableGeolocate
                            ),
                        }
                    );
                }
            )
            .AllowAnonymous();
    }

    private static bool ResolveBool(Dictionary<string, string?> values, string key) =>
        values.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed) ? parsed : true;
}
