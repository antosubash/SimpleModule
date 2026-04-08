using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using SimpleModule.Core.Exceptions;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map;

public partial class MapService(
    MapDbContext db,
    IDatasetsContracts datasets,
    ILogger<MapService> logger
) : IMapContracts
{
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
            Attribution = null,
            MinZoom = null,
            MaxZoom = null,
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

    // ---------- Saved maps ----------

    public async Task<IEnumerable<SavedMap>> GetAllMapsAsync() =>
        await db
            .SavedMaps.AsNoTracking()
            .Include(m => m.Layers)
            .Include(m => m.Basemaps)
            .AsSplitQuery()
            .OrderBy(m => m.Name)
            .ToListAsync();

    public async Task<SavedMap?> GetMapByIdAsync(SavedMapId id)
    {
        var map = await db
            .SavedMaps.AsNoTracking()
            .Include(m => m.Layers)
            .Include(m => m.Basemaps)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.Id == id);
        if (map is null)
        {
            LogMapNotFound(logger, id);
        }

        return map;
    }

    public async Task<SavedMap> CreateMapAsync(CreateMapRequest request)
    {
        var map = new SavedMap
        {
            Id = SavedMapId.From(Guid.NewGuid()),
            Name = request.Name,
            Description = request.Description,
            CenterLng = request.CenterLng,
            CenterLat = request.CenterLat,
            Center = PointFromLngLat(request.CenterLng, request.CenterLat),
            Zoom = request.Zoom,
            Pitch = request.Pitch,
            Bearing = request.Bearing,
            BaseStyleUrl = request.BaseStyleUrl,
            Layers = request.Layers ?? [],
            Basemaps = request.Basemaps ?? [],
        };

        db.SavedMaps.Add(map);
        await db.SaveChangesAsync();

        LogMapCreated(logger, map.Id, map.Name);
        return map;
    }

    public async Task<SavedMap> UpdateMapAsync(SavedMapId id, UpdateMapRequest request)
    {
        var map =
            await db
                .SavedMaps.Include(m => m.Layers)
                .Include(m => m.Basemaps)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new NotFoundException("SavedMap", id);

        map.Name = request.Name;
        map.Description = request.Description;
        map.CenterLng = request.CenterLng;
        map.CenterLat = request.CenterLat;
        map.Center = PointFromLngLat(request.CenterLng, request.CenterLat);
        map.Zoom = request.Zoom;
        map.Pitch = request.Pitch;
        map.Bearing = request.Bearing;
        map.BaseStyleUrl = request.BaseStyleUrl;

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

        await db.SaveChangesAsync();

        LogMapUpdated(logger, map.Id, map.Name);
        return map;
    }

    public async Task DeleteMapAsync(SavedMapId id)
    {
        var map = await db.SavedMaps.FindAsync(id) ?? throw new NotFoundException("SavedMap", id);

        db.SavedMaps.Remove(map);
        await db.SaveChangesAsync();

        LogMapDeleted(logger, id);
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "SavedMap {Id} not found")]
    private static partial void LogMapNotFound(ILogger logger, SavedMapId id);

    [LoggerMessage(Level = LogLevel.Information, Message = "SavedMap {Id} created: {Name}")]
    private static partial void LogMapCreated(ILogger logger, SavedMapId id, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "SavedMap {Id} updated: {Name}")]
    private static partial void LogMapUpdated(ILogger logger, SavedMapId id, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "SavedMap {Id} deleted")]
    private static partial void LogMapDeleted(ILogger logger, SavedMapId id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Basemap {Id} not found")]
    private static partial void LogBasemapNotFound(ILogger logger, BasemapId id);

    [LoggerMessage(Level = LogLevel.Information, Message = "Basemap {Id} created: {Name}")]
    private static partial void LogBasemapCreated(ILogger logger, BasemapId id, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Basemap {Id} updated: {Name}")]
    private static partial void LogBasemapUpdated(ILogger logger, BasemapId id, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Basemap {Id} deleted")]
    private static partial void LogBasemapDeleted(ILogger logger, BasemapId id);
}
