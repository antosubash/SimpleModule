namespace SimpleModule.Map.Contracts;

public static class MapConstants
{
    public const string ModuleName = "Map";
    public const string RoutePrefix = "/api/map";
    public const string ViewPrefix = "/map";

    public static class Routes
    {
        // Layer source CRUD
        public const string GetAllSources = "/sources";
        public const string GetSourceById = "/sources/{id}";
        public const string CreateSource = "/sources";
        public const string UpdateSource = "/sources/{id}";
        public const string DeleteSource = "/sources/{id}";

        // Basemap catalog CRUD
        public const string GetAllBasemaps = "/basemaps";
        public const string GetBasemapById = "/basemaps/{id}";
        public const string CreateBasemap = "/basemaps";
        public const string UpdateBasemap = "/basemaps/{id}";
        public const string DeleteBasemap = "/basemaps/{id}";

        // Saved map CRUD
        public const string GetAllMaps = "/maps";
        public const string GetMapById = "/maps/{id}";
        public const string CreateMap = "/maps";
        public const string UpdateMap = "/maps/{id}";
        public const string DeleteMap = "/maps/{id}";

        // Views
        public const string Browse = "/";
        public const string Layers = "/layers";
        public const string Edit = "/{id}/edit";
        public const string View = "/{id}";
    }
}
