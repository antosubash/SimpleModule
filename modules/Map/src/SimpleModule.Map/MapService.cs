using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using SimpleModule.Core.Exceptions;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Map.Contracts;
using SimpleModule.Map.EntityConfigurations;

namespace SimpleModule.Map;

public partial class MapService(
    MapDbContext db,
    IDatasetsContracts datasets,
    IOptions<MapModuleOptions> options,
    ILogger<MapService> logger
) : IMapContracts
{
    private MapModuleOptions Options => options.Value;

    /// <summary>
    /// Shared geometry factory configured for SRID 4326 (WGS84). NetTopologySuite uses
    /// this for all client-facing spatial values so PostGIS / SQL Server / SpatiaLite
    /// columns are tagged with the right coordinate reference system.
    /// </summary>
    private static readonly GeometryFactory GeometryFactory =
        NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private static Polygon? PolygonFromBounds(double[]? bounds)
    {
        if (bounds is not { Length: 4 })
        {
            return null;
        }

        var west = bounds[0];
        var south = bounds[1];
        var east = bounds[2];
        var north = bounds[3];

        var ring = GeometryFactory.CreateLinearRing([
            new Coordinate(west, south),
            new Coordinate(east, south),
            new Coordinate(east, north),
            new Coordinate(west, north),
            new Coordinate(west, south),
        ]);
        return GeometryFactory.CreatePolygon(ring);
    }

    private static Point PointFromLngLat(double lng, double lat) =>
        GeometryFactory.CreatePoint(new Coordinate(lng, lat));

    // ---------- Layer sources ----------

    public async Task<IEnumerable<LayerSource>> GetAllLayerSourcesAsync() =>
        await db.LayerSources.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

    public async Task<LayerSource?> GetLayerSourceByIdAsync(LayerSourceId id)
    {
        var source = await db.LayerSources.FindAsync(id);
        if (source is null)
        {
            LogLayerSourceNotFound(logger, id);
        }

        return source;
    }

    public async Task<LayerSource> CreateLayerSourceAsync(CreateLayerSourceRequest request)
    {
        var source = new LayerSource
        {
            Id = LayerSourceId.From(Guid.NewGuid()),
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Url = request.Url,
            Attribution = request.Attribution,
            MinZoom = request.MinZoom,
            MaxZoom = request.MaxZoom,
            Bounds = request.Bounds,
            Coverage = PolygonFromBounds(request.Bounds),
            Metadata = request.Metadata ?? [],
        };

        db.LayerSources.Add(source);
        await db.SaveChangesAsync();

        LogLayerSourceCreated(logger, source.Id, source.Name);
        return source;
    }

    public async Task<LayerSource> CreateLayerSourceFromDatasetAsync(
        CreateLayerSourceFromDatasetRequest request,
        CancellationToken ct = default
    )
    {
        var datasetId = DatasetId.From(request.DatasetId);
        var dataset =
            await datasets.GetByIdAsync(datasetId, ct)
            ?? throw new NotFoundException("Dataset", request.DatasetId);

        double[]? bounds = dataset.BoundingBox is { } bb
            ? [bb.MinX, bb.MinY, bb.MaxX, bb.MaxY]
            : null;

        var source = new LayerSource
        {
            Id = LayerSourceId.From(Guid.NewGuid()),
            Name = string.IsNullOrWhiteSpace(request.Name) ? dataset.Name : request.Name,
            Description = request.Description,
            Type = LayerSourceType.Dataset,
            Url = $"/api/datasets/{request.DatasetId}/features",
            Bounds = bounds,
            Coverage = PolygonFromBounds(bounds),
            Metadata = new Dictionary<string, string>
            {
                ["datasetId"] = request.DatasetId.ToString(),
            },
        };

        db.LayerSources.Add(source);
        await db.SaveChangesAsync(ct);

        LogLayerSourceCreated(logger, source.Id, source.Name);
        return source;
    }

    public async Task<LayerSource> UpdateLayerSourceAsync(
        LayerSourceId id,
        UpdateLayerSourceRequest request
    )
    {
        var source =
            await db.LayerSources.FindAsync(id) ?? throw new NotFoundException("LayerSource", id);

        source.Name = request.Name;
        source.Description = request.Description;
        source.Type = request.Type;
        source.Url = request.Url;
        source.Attribution = request.Attribution;
        source.MinZoom = request.MinZoom;
        source.MaxZoom = request.MaxZoom;
        source.Bounds = request.Bounds;
        source.Coverage = PolygonFromBounds(request.Bounds);
        source.Metadata = request.Metadata ?? [];

        await db.SaveChangesAsync();

        LogLayerSourceUpdated(logger, source.Id, source.Name);
        return source;
    }

    public async Task DeleteLayerSourceAsync(LayerSourceId id)
    {
        var source =
            await db.LayerSources.FindAsync(id) ?? throw new NotFoundException("LayerSource", id);

        db.LayerSources.Remove(source);
        await db.SaveChangesAsync();

        LogLayerSourceDeleted(logger, id);
    }

    // ---------- Default map (singleton) ----------

    private const string DefaultMapName = "Default Map";

    public async Task<SavedMap> GetDefaultMapAsync(CancellationToken ct = default)
    {
        var map = await db
            .SavedMaps.AsNoTracking()
            .Include(m => m.Layers)
            .Include(m => m.Basemaps)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.Id == MapConstants.DefaultMapId, ct);

        if (map is not null)
        {
            return map;
        }

        // Lazily seed from MapModuleOptions so the application always has exactly one map.
        var seed = new SavedMap
        {
            Id = MapConstants.DefaultMapId,
            Name = DefaultMapName,
            Description = null,
            CenterLng = Options.DefaultCenterLng,
            CenterLat = Options.DefaultCenterLat,
            Center = PointFromLngLat(Options.DefaultCenterLng, Options.DefaultCenterLat),
            Zoom = Options.DefaultZoom,
            Pitch = Options.DefaultPitch,
            Bearing = Options.DefaultBearing,
            BaseStyleUrl = Options.BaseStyleUrl,
            Basemaps = BasemapConfiguration
                .SeedIds.All.Select((id, i) => new MapBasemap { BasemapId = id, Order = i })
                .ToList(),
            Layers =
            [
                new MapLayer
                {
                    LayerSourceId = LayerSourceConfiguration.SeedIds.OpenStreetMapXyz,
                    Order = 0,
                    Visible = true,
                    Opacity = 1,
                },
                new MapLayer
                {
                    LayerSourceId = LayerSourceConfiguration.SeedIds.MapLibreEarthquakesGeoJson,
                    Order = 1,
                    Visible = true,
                    Opacity = 1,
                },
                new MapLayer
                {
                    LayerSourceId = LayerSourceConfiguration.SeedIds.TerrestrisOsmWms,
                    Order = 2,
                    Visible = false,
                    Opacity = 1,
                },
            ],
        };

        db.SavedMaps.Add(seed);
        await db.SaveChangesAsync(ct);

        LogDefaultMapSeeded(logger);
        return seed;
    }

    public async Task<SavedMap> UpdateDefaultMapAsync(
        UpdateDefaultMapRequest request,
        CancellationToken ct = default
    )
    {
        var map = await db
            .SavedMaps.Include(m => m.Layers)
            .Include(m => m.Basemaps)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.Id == MapConstants.DefaultMapId, ct);

        if (map is null)
        {
            map = new SavedMap { Id = MapConstants.DefaultMapId, Name = DefaultMapName };
            db.SavedMaps.Add(map);
        }

        map.CenterLng = request.CenterLng;
        map.CenterLat = request.CenterLat;
        map.Center = PointFromLngLat(request.CenterLng, request.CenterLat);
        map.Zoom = request.Zoom;
        map.Pitch = request.Pitch;
        map.Bearing = request.Bearing;
        map.BaseStyleUrl = string.IsNullOrWhiteSpace(request.BaseStyleUrl)
            ? Options.BaseStyleUrl
            : request.BaseStyleUrl;

        // Replace owned layers wholesale.
        map.Layers.Clear();
        foreach (var layer in request.Layers ?? [])
        {
            map.Layers.Add(layer);
        }

        // Replace owned basemaps wholesale.
        map.Basemaps.Clear();
        foreach (var basemap in request.Basemaps ?? [])
        {
            map.Basemaps.Add(basemap);
        }

        await db.SaveChangesAsync(ct);

        LogDefaultMapUpdated(logger);
        return map;
    }

    // ---------- Basemap catalog ----------

    public async Task<IEnumerable<Basemap>> GetAllBasemapsAsync() =>
        await db.Basemaps.AsNoTracking().OrderBy(b => b.Name).ToListAsync();

    public async Task<Basemap?> GetBasemapByIdAsync(BasemapId id)
    {
        var basemap = await db.Basemaps.FindAsync(id);
        if (basemap is null)
        {
            LogBasemapNotFound(logger, id);
        }

        return basemap;
    }

    public async Task<Basemap> CreateBasemapAsync(CreateBasemapRequest request)
    {
        var basemap = new Basemap
        {
            Id = BasemapId.From(Guid.NewGuid()),
            Name = request.Name,
            Description = request.Description,
            StyleUrl = request.StyleUrl,
            Attribution = request.Attribution,
            ThumbnailUrl = request.ThumbnailUrl,
        };

        db.Basemaps.Add(basemap);
        await db.SaveChangesAsync();

        LogBasemapCreated(logger, basemap.Id, basemap.Name);
        return basemap;
    }

    public async Task<Basemap> UpdateBasemapAsync(BasemapId id, UpdateBasemapRequest request)
    {
        var basemap = await db.Basemaps.FindAsync(id) ?? throw new NotFoundException("Basemap", id);

        basemap.Name = request.Name;
        basemap.Description = request.Description;
        basemap.StyleUrl = request.StyleUrl;
        basemap.Attribution = request.Attribution;
        basemap.ThumbnailUrl = request.ThumbnailUrl;

        await db.SaveChangesAsync();

        LogBasemapUpdated(logger, basemap.Id, basemap.Name);
        return basemap;
    }

    public async Task DeleteBasemapAsync(BasemapId id)
    {
        var basemap = await db.Basemaps.FindAsync(id) ?? throw new NotFoundException("Basemap", id);

        db.Basemaps.Remove(basemap);
        await db.SaveChangesAsync();

        LogBasemapDeleted(logger, id);
    }

    // ---------- Logging ----------

    [LoggerMessage(Level = LogLevel.Warning, Message = "LayerSource {Id} not found")]
    private static partial void LogLayerSourceNotFound(ILogger logger, LayerSourceId id);

    [LoggerMessage(Level = LogLevel.Information, Message = "LayerSource {Id} created: {Name}")]
    private static partial void LogLayerSourceCreated(
        ILogger logger,
        LayerSourceId id,
        string name
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "LayerSource {Id} updated: {Name}")]
    private static partial void LogLayerSourceUpdated(
        ILogger logger,
        LayerSourceId id,
        string name
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "LayerSource {Id} deleted")]
    private static partial void LogLayerSourceDeleted(ILogger logger, LayerSourceId id);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Default map seeded from MapModuleOptions"
    )]
    private static partial void LogDefaultMapSeeded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Default map updated")]
    private static partial void LogDefaultMapUpdated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Basemap {Id} not found")]
    private static partial void LogBasemapNotFound(ILogger logger, BasemapId id);

    [LoggerMessage(Level = LogLevel.Information, Message = "Basemap {Id} created: {Name}")]
    private static partial void LogBasemapCreated(ILogger logger, BasemapId id, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Basemap {Id} updated: {Name}")]
    private static partial void LogBasemapUpdated(ILogger logger, BasemapId id, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Basemap {Id} deleted")]
    private static partial void LogBasemapDeleted(ILogger logger, BasemapId id);
}
