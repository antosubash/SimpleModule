using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Agents;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Datasets.Agents;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Datasets.Converters;
using SimpleModule.Datasets.Jobs;
using SimpleModule.Datasets.Processing;

namespace SimpleModule.Datasets;

[Module(
    DatasetsConstants.ModuleName,
    RoutePrefix = DatasetsConstants.RoutePrefix,
    ViewPrefix = DatasetsConstants.ViewPrefix
)]
public class DatasetsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<DatasetsDbContext>(configuration, DatasetsConstants.ModuleName);
        services.AddScoped<IDatasetsContracts, DatasetsContractsService>();
        services.AddScoped<IAgentToolProvider, DatasetsToolProvider>();

        // Processors (one per format)
        services.AddScoped<IDatasetProcessor, GeoJsonProcessor>();
        services.AddScoped<IDatasetProcessor, ShapefileProcessor>();
        services.AddScoped<IDatasetProcessor, KmlProcessor>();
        services.AddScoped<IDatasetProcessor, GeoPackageProcessor>();
        services.AddScoped<IDatasetProcessor, PmTilesProcessor>();
        services.AddScoped<IDatasetProcessor, CogProcessor>();
        services.AddScoped<DatasetProcessorRegistry>();

        // Converters
        services.AddScoped<IDatasetConverter, VectorToGeoJsonConverter>();
        services.AddScoped<IDatasetConverter, VectorToPmTilesConverter>();
        services.AddScoped<IDatasetConverter, RasterToCogConverter>();
        services.AddScoped<DatasetConverterRegistry>();

        // Background jobs are resolved by type via IBackgroundJobs.EnqueueAsync<T>;
        // register so DI can construct them.
        services.AddScoped<ProcessDatasetJob>();
        services.AddScoped<ConvertDatasetJob>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Datasets",
                Url = DatasetsConstants.ViewPrefix,
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l5.447 2.724A1 1 0 0021 18.382V7.618a1 1 0 00-1.447-.894L15 4m0 13V4m0 0L9 7"/></svg>""",
                Order = 55,
                Section = MenuSection.AppSidebar,
            }
        );
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<DatasetsPermissions>();
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings.Add(
            new SettingDefinition
            {
                Key = DatasetsConstants.SettingKeys.MaxUploadSizeMb,
                DisplayName = "Max Upload Size (MB)",
                Description = "Maximum allowed upload size for GIS datasets in megabytes.",
                Group = "Datasets",
                Scope = SettingScope.Application,
                DefaultValue = "1024",
                Type = SettingType.Number,
            }
        );
        settings.Add(
            new SettingDefinition
            {
                Key = DatasetsConstants.SettingKeys.AllowedFormats,
                DisplayName = "Allowed Formats",
                Description = "JSON array of DatasetFormat names accepted on upload.",
                Group = "Datasets",
                Scope = SettingScope.Application,
                DefaultValue =
                    "[\"GeoJson\",\"Shapefile\",\"Kml\",\"Kmz\",\"GeoPackage\",\"PmTiles\",\"Cog\"]",
                Type = SettingType.Json,
            }
        );
        settings.Add(
            new SettingDefinition
            {
                Key = DatasetsConstants.SettingKeys.DefaultTargetSrid,
                DisplayName = "Default Target SRID",
                Description = "Target SRID used to reproject vector datasets during processing.",
                Group = "Datasets",
                Scope = SettingScope.Application,
                DefaultValue = "4326",
                Type = SettingType.Number,
            }
        );
        settings.Add(
            new SettingDefinition
            {
                Key = DatasetsConstants.SettingKeys.FeatureQueryLimit,
                DisplayName = "Feature Query Limit",
                Description =
                    "Maximum number of features returned by the feature query endpoint / agent tool.",
                Group = "Datasets",
                Scope = SettingScope.Application,
                DefaultValue = "1000",
                Type = SettingType.Number,
            }
        );
        settings.Add(
            new SettingDefinition
            {
                Key = DatasetsConstants.SettingKeys.DefaultVectorConversionFormat,
                DisplayName = "Default Vector Conversion Format",
                Description =
                    "Target format used when a vector dataset is converted without an explicit target.",
                Group = "Datasets",
                Scope = SettingScope.Application,
                DefaultValue = "\"PmTiles\"",
                Type = SettingType.Text,
            }
        );
        settings.Add(
            new SettingDefinition
            {
                Key = DatasetsConstants.SettingKeys.DefaultRasterConversionFormat,
                DisplayName = "Default Raster Conversion Format",
                Description =
                    "Target format used when a raster dataset is converted without an explicit target.",
                Group = "Datasets",
                Scope = SettingScope.Application,
                DefaultValue = "\"Cog\"",
                Type = SettingType.Text,
            }
        );
        settings.Add(
            new SettingDefinition
            {
                Key = DatasetsConstants.SettingKeys.AutoGenerateDefaultDerivative,
                DisplayName = "Auto Generate Default Derivative",
                Description =
                    "When true, successful processing auto-enqueues a conversion to the default derivative format (vector→PMTiles, raster→COG).",
                Group = "Datasets",
                Scope = SettingScope.Application,
                DefaultValue = "true",
                Type = SettingType.Bool,
            }
        );
        settings.Add(
            new SettingDefinition
            {
                Key = DatasetsConstants.SettingKeys.StoragePrefix,
                DisplayName = "Storage Prefix",
                Description = "Root path under which dataset blobs are stored.",
                Group = "Datasets",
                Scope = SettingScope.Application,
                DefaultValue = "\"datasets\"",
                Type = SettingType.Text,
            }
        );
    }
}
