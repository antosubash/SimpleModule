using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Menu;
using SimpleModule.Settings.Contracts;
using SimpleModule.Settings.Entities;

namespace SimpleModule.Settings.Services;

public sealed class PublicMenuService(
    SettingsDbContext db,
    IMemoryCache cache,
    IOptions<SettingsModuleOptions> moduleOptions
) : IPublicMenuProvider
{
    private const string MenuTreeCacheKey = "PublicMenu_Tree";
    private const string HomePageCacheKey = "PublicMenu_Home";

    public async Task<IReadOnlyList<PublicMenuItem>> GetMenuTreeAsync()
    {
        if (
            cache.TryGetValue(MenuTreeCacheKey, out IReadOnlyList<PublicMenuItem>? cached)
            && cached is not null
        )
            return cached;

        var entities = await db
            .PublicMenuItems.Where(e => e.IsVisible)
            .OrderBy(e => e.SortOrder)
            .ToListAsync();

        var tree = BuildPublicTree(entities, parentId: null);
        cache.Set(MenuTreeCacheKey, tree, moduleOptions.Value.CacheDuration);
        return tree;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Return value, not a property"
    )]
    public async Task<string?> GetHomePageUrlAsync()
    {
        if (cache.TryGetValue(HomePageCacheKey, out string? cached))
            return cached;

        var entity = await db
            .PublicMenuItems.Where(e => e.IsVisible && e.IsHomePage)
            .FirstOrDefaultAsync();

        var url = entity is not null ? (entity.Url ?? entity.PageRoute) : null;
        cache.Set(HomePageCacheKey, url, moduleOptions.Value.CacheDuration);
        return url;
    }

    public async Task<List<PublicMenuItemDto>> GetAllAsync()
    {
        var entities = await db.PublicMenuItems.OrderBy(e => e.SortOrder).ToListAsync();

        return BuildDtoTree(entities, parentId: null);
    }

    public async Task<PublicMenuItemEntity> CreateAsync(CreateMenuItemRequest request)
    {
        if (request.ParentId is not null)
        {
            var depth = await GetDepthAsync(request.ParentId.Value);
            if (depth >= 3)
                throw new InvalidOperationException(
                    "Cannot create menu item: maximum nesting depth of 3 has been reached."
                );
        }

        var maxSortOrder =
            await db
                .PublicMenuItems.Where(e => e.ParentId == request.ParentId)
                .MaxAsync(e => (int?)e.SortOrder)
            ?? -1;

        if (request.IsHomePage)
            await ClearAllHomePageFlags();

        var entity = new PublicMenuItemEntity
        {
            ParentId = request.ParentId,
            Label = request.Label,
            Url = request.Url,
            PageRoute = request.PageRoute,
            Icon = request.Icon,
            CssClass = request.CssClass,
            OpenInNewTab = request.OpenInNewTab,
            IsVisible = request.IsVisible,
            IsHomePage = request.IsHomePage,
            SortOrder = maxSortOrder + 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.PublicMenuItems.Add(entity);
        await db.SaveChangesAsync();
        InvalidateCache();
        return entity;
    }

    public async Task<PublicMenuItemEntity?> UpdateAsync(int id, UpdateMenuItemRequest request)
    {
        var entity = await db.PublicMenuItems.FindAsync(id);
        if (entity is null)
            return null;

        if (request.IsHomePage)
            await ClearAllHomePageFlags();

        entity.Label = request.Label;
        entity.Url = request.Url;
        entity.PageRoute = request.PageRoute;
        entity.Icon = request.Icon;
        entity.CssClass = request.CssClass;
        entity.OpenInNewTab = request.OpenInNewTab;
        entity.IsVisible = request.IsVisible;
        entity.IsHomePage = request.IsHomePage;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        InvalidateCache();
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await db.PublicMenuItems.FindAsync(id);
        if (entity is null)
            return false;

        db.PublicMenuItems.Remove(entity);
        await db.SaveChangesAsync();
        InvalidateCache();
        return true;
    }

    public async Task ReorderAsync(ReorderMenuItemsRequest request)
    {
        foreach (var item in request.Items)
        {
            var entity = await db.PublicMenuItems.FindAsync(item.Id);
            if (entity is not null)
            {
                entity.ParentId = item.ParentId;
                entity.SortOrder = item.SortOrder;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        InvalidateCache();
    }

    public async Task SetHomePageAsync(int id)
    {
        await ClearAllHomePageFlags();

        var entity = await db.PublicMenuItems.FindAsync(id);
        if (entity is not null)
        {
            entity.IsHomePage = true;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        InvalidateCache();
    }

    public async Task ClearHomePageAsync()
    {
        await ClearAllHomePageFlags();
        await db.SaveChangesAsync();
        InvalidateCache();
    }

    private async Task ClearAllHomePageFlags()
    {
        var homePages = await db.PublicMenuItems.Where(e => e.IsHomePage).ToListAsync();

        foreach (var hp in homePages)
        {
            hp.IsHomePage = false;
            hp.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task<int> GetDepthAsync(int parentId)
    {
        var depth = 1;
        var currentId = (int?)parentId;

        while (currentId is not null)
        {
            var parent = await db
                .PublicMenuItems.Where(e => e.Id == currentId)
                .Select(e => e.ParentId)
                .FirstOrDefaultAsync();

            currentId = parent;
            if (currentId is not null)
                depth++;
        }

        return depth;
    }

    private static List<PublicMenuItem> BuildPublicTree(
        List<PublicMenuItemEntity> entities,
        int? parentId
    )
    {
        return entities
            .Where(e => e.ParentId == parentId)
            .Select(e => new PublicMenuItem
            {
                Label = e.Label,
                Url = e.Url ?? e.PageRoute ?? "#",
                Icon = e.Icon,
                CssClass = e.CssClass,
                OpenInNewTab = e.OpenInNewTab,
                IsHomePage = e.IsHomePage,
                Children = BuildPublicTree(entities, e.Id),
            })
            .ToList();
    }

    private static List<PublicMenuItemDto> BuildDtoTree(
        List<PublicMenuItemEntity> entities,
        int? parentId
    )
    {
        return entities
            .Where(e => e.ParentId == parentId)
            .Select(e => new PublicMenuItemDto
            {
                Id = e.Id,
                ParentId = e.ParentId,
                Label = e.Label,
                Url = e.Url,
                PageRoute = e.PageRoute,
                Icon = e.Icon,
                CssClass = e.CssClass,
                OpenInNewTab = e.OpenInNewTab,
                IsVisible = e.IsVisible,
                IsHomePage = e.IsHomePage,
                SortOrder = e.SortOrder,
                Children = BuildDtoTree(entities, e.Id),
            })
            .ToList();
    }

    private void InvalidateCache()
    {
        cache.Remove(MenuTreeCacheKey);
        cache.Remove(HomePageCacheKey);
    }
}
