using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Exceptions;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map;

public partial class MapService
{
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
}
