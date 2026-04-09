namespace SimpleModule.Map.Contracts;

public static class MapConstants
{
    public const string ModuleName = "Map";
    public const string RoutePrefix = "/api/map";
    public const string ViewPrefix = "/map";

    /// <summary>
    /// Stable identity of the singleton default map. The Map module manages exactly
    /// one map composition; this fixed id lets the service upsert it without a
    /// dedicated "is default" column.
    /// </summary>
    public static readonly SavedMapId DefaultMapId = SavedMapId.From(
        new Guid("00000000-0000-0000-0000-000000000001")
    );

    public static class Routes
    {
        // Layer source CRUD
        public const string GetAllSources = "/sources";
        public const string GetSourceById = "/sources/{id}";
        public const string CreateSource = "/sources";
        public const string CreateSourceFromDataset = "/sources/from-dataset";
        public const string UpdateSource = "/sources/{id}";
        public const string DeleteSource = "/sources/{id}";

        // Basemap catalog CRUD
        public const string GetAllBasemaps = "/basemaps";
        public const string GetBasemapById = "/basemaps/{id}";
        public const string CreateBasemap = "/basemaps";
        public const string UpdateBasemap = "/basemaps/{id}";
        public const string DeleteBasemap = "/basemaps/{id}";

        // Default map (singleton)
        public const string GetDefaultMap = "/default";
        public const string UpdateDefaultMap = "/default";

        // Views
        public const string Browse = "/";
        public const string Layers = "/layers";
    }

    /// <summary>
    /// Runtime-editable setting keys registered by <c>MapModule.ConfigureSettings</c>
    /// and exposed in the generic admin settings UI. Values override the compile-time
    /// defaults from <c>MapModuleOptions</c>.
    /// </summary>
    public static class SettingKeys
    {
        public const string EnableMeasureTools = "Map.EnableMeasureTools";
        public const string EnableExportPng = "Map.EnableExportPng";
        public const string EnableGeolocate = "Map.EnableGeolocate";
    }
}
