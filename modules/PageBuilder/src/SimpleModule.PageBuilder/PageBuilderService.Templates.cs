using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Exceptions;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder;

public sealed partial class PageBuilderService
{
    public async Task<IEnumerable<PageTemplate>> GetAllTemplatesAsync() =>
        await db.Templates.AsNoTracking().OrderBy(t => t.Name).ToListAsync();

    public async Task<PageTemplate> CreateTemplateAsync(CreatePageTemplateRequest request)
    {
        var template = new PageTemplate { Name = request.Name, Content = request.Content };

        db.Templates.Add(template);
        await db.SaveChangesAsync();

        return template;
    }

    public async Task DeleteTemplateAsync(PageTemplateId id)
    {
        var template =
            await db.Templates.FindAsync(id) ?? throw new NotFoundException("PageTemplate", id);

        db.Templates.Remove(template);
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<PageTag>> GetAllTagsAsync() =>
        await db.Tags.AsNoTracking().OrderBy(t => t.Name).ToListAsync();

    public async Task<PageTag> GetOrCreateTagAsync(string name)
    {
        var slugName = Slugify(name);
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == slugName);
        if (tag is not null)
            return tag;

        tag = new PageTag { Name = slugName };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();
        return tag;
    }

    public async Task AddTagToPageAsync(PageId pageId, string tagName)
    {
        var page =
            await db.Pages.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == pageId)
            ?? throw new NotFoundException("Page", pageId);

        var tag = await GetOrCreateTagAsync(tagName);

        if (!page.Tags.Any(t => t.Id == tag.Id))
        {
            page.Tags.Add(tag);
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveTagFromPageAsync(PageId pageId, PageTagId tagId)
    {
        var page =
            await db.Pages.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == pageId)
            ?? throw new NotFoundException("Page", pageId);

        var tag = page.Tags.FirstOrDefault(t => t.Id == tagId);
        if (tag is not null)
        {
            page.Tags.Remove(tag);
            await db.SaveChangesAsync();
        }
    }
}
