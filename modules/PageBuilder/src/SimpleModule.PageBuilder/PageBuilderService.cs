using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Exceptions;
using SimpleModule.PageBuilder.Contracts;
using SimpleModule.PageBuilder.Contracts.Events;
using Wolverine;

namespace SimpleModule.PageBuilder;

public sealed partial class PageBuilderService(
    PageBuilderDbContext db,
    IMessageBus bus,
    ILogger<PageBuilderService> logger
) : IPageBuilderContracts, IPageBuilderTemplateContracts, IPageBuilderTagContracts
{
    public async Task<IEnumerable<PageSummary>> GetAllPagesAsync() =>
        await db
            .Pages.AsNoTracking()
            .Include(p => p.Tags)
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Title)
            .Select(p => new PageSummary
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                IsPublished = p.IsPublished,
                HasDraft = !string.IsNullOrEmpty(p.DraftContent),
                Order = p.Order,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                DeletedAt = p.DeletedAt,
                Tags = p.Tags.Select(t => t.Name).ToList(),
            })
            .ToListAsync();

    public async Task<Page?> GetPageByIdAsync(PageId id)
    {
        var page = await db
            .Pages.AsNoTracking()
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (page is null)
        {
            LogPageNotFound(logger, id);
        }

        return page;
    }

    public async Task<Page?> GetPageBySlugAsync(string slug) =>
        await db.Pages.AsNoTracking().Include(p => p.Tags).FirstOrDefaultAsync(p => p.Slug == slug);

    public async Task<IEnumerable<PageSummary>> GetPublishedPagesAsync() =>
        await db
            .Pages.AsNoTracking()
            .Include(p => p.Tags)
            .Where(p => p.IsPublished)
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Title)
            .Select(p => new PageSummary
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                IsPublished = p.IsPublished,
                HasDraft = !string.IsNullOrEmpty(p.DraftContent),
                Order = p.Order,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                DeletedAt = p.DeletedAt,
                Tags = p.Tags.Select(t => t.Name).ToList(),
            })
            .ToListAsync();

    public async Task<Page> CreatePageAsync(CreatePageRequest request)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slugify(request.Title)
            : Slugify(request.Slug);

        var validationError = ValidateSlug(slug);
        if (validationError is not null)
            throw new ArgumentException(validationError, nameof(request));

        slug = await EnsureUniqueSlugAsync(slug);

        var page = new Page
        {
            Title = request.Title,
            Slug = slug,
            Content = "{}",
        };

        db.Pages.Add(page);
        await db.SaveChangesAsync();

        LogPageCreated(logger, page.Id, page.Title);

        await bus.PublishAsync(new PageCreatedEvent(page.Id, page.Title, page.Slug));

        return page;
    }

    public async Task<Page> UpdatePageAsync(PageId id, UpdatePageRequest request)
    {
        var page = await db.Pages.FindAsync(id) ?? throw new NotFoundException("Page", id);

        page.Title = request.Title;

        var slugToSet = Slugify(request.Slug);
        var slugError = ValidateSlug(slugToSet);
        if (slugError is not null)
            throw new ArgumentException(slugError, nameof(request));

        if (
            slugToSet != page.Slug
            && await db.Pages.AnyAsync(p => p.Slug == slugToSet && p.Id != id)
        )
            throw new ArgumentException("Slug is already taken.", nameof(request));

        page.Slug = slugToSet;
        page.Order = request.Order;
        page.IsPublished = request.IsPublished;
        page.MetaDescription = request.MetaDescription;
        page.MetaKeywords = request.MetaKeywords;
        page.OgImage = request.OgImage;

        await db.SaveChangesAsync();

        LogPageUpdated(logger, page.Id, page.Title);
        return page;
    }

    public async Task<Page> UpdatePageContentAsync(PageId id, UpdatePageContentRequest request)
    {
        var page = await db.Pages.FindAsync(id) ?? throw new NotFoundException("Page", id);

        page.DraftContent = request.Content;

        await db.SaveChangesAsync();

        LogPageContentUpdated(logger, page.Id);
        return page;
    }

    public async Task DeletePageAsync(PageId id)
    {
        var page = await db.Pages.FindAsync(id) ?? throw new NotFoundException("Page", id);

        page.IsPublished = false;
        page.IsDeleted = true;
        page.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        LogPageDeleted(logger, id);

        await bus.PublishAsync(new PageDeletedEvent(id));
    }

    public async Task<Page> PublishPageAsync(PageId id)
    {
        var page = await db.Pages.FindAsync(id) ?? throw new NotFoundException("Page", id);

        if (!string.IsNullOrEmpty(page.DraftContent))
        {
            page.Content = page.DraftContent;
            page.DraftContent = null;
        }

        page.IsPublished = true;

        await db.SaveChangesAsync();

        LogPagePublished(logger, page.Id, page.Title);

        await bus.PublishAsync(new PagePublishedEvent(page.Id, page.Title));

        return page;
    }

    public async Task<Page> UnpublishPageAsync(PageId id)
    {
        var page = await db.Pages.FindAsync(id) ?? throw new NotFoundException("Page", id);

        page.IsPublished = false;

        await db.SaveChangesAsync();

        LogPageUnpublished(logger, page.Id, page.Title);

        await bus.PublishAsync(new PageUnpublishedEvent(page.Id, page.Title));

        return page;
    }

    public async Task<IEnumerable<PageSummary>> GetTrashedPagesAsync() =>
        await db
            .Pages.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Tags)
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.DeletedAt)
            .Select(p => new PageSummary
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                IsPublished = p.IsPublished,
                HasDraft = !string.IsNullOrEmpty(p.DraftContent),
                Order = p.Order,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                DeletedAt = p.DeletedAt,
                Tags = p.Tags.Select(t => t.Name).ToList(),
            })
            .ToListAsync();

    public async Task<Page> RestorePageAsync(PageId id)
    {
        var page =
            await db.Pages.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted)
            ?? throw new NotFoundException("Page", id);

        page.IsDeleted = false;
        page.DeletedAt = null;
        page.DeletedBy = null;
        await db.SaveChangesAsync();

        LogPageRestored(logger, page.Id, page.Title);
        return page;
    }

    public async Task PermanentDeletePageAsync(PageId id)
    {
        var page =
            await db
                .Pages.IgnoreQueryFilters()
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Page", id);

        page.Tags.Clear();
        await db.SaveChangesAsync();

        // Bypass the soft-delete interceptor for a true hard delete.
        await db.Pages.IgnoreQueryFilters().Where(p => p.Id == id).ExecuteDeleteAsync();

        LogPagePermanentlyDeleted(logger, id);
    }

    internal static string Slugify(string text)
    {
#pragma warning disable CA1308 // URL slugs are conventionally lowercase
        var slug = text.ToLowerInvariant();
#pragma warning restore CA1308
        slug = SlugInvalidCharsRegex().Replace(slug, "");
        slug = SlugWhitespaceRegex().Replace(slug, "-");
        slug = slug.Trim('-');
        return slug;
    }

    internal static string? ValidateSlug(string slug)
    {
        if (slug.Length < 3)
            return "Slug must be at least 3 characters.";
        if (slug.Length > 200)
            return "Slug must be at most 200 characters.";
        if (!SlugValidRegex().IsMatch(slug))
            return "Slug must contain only lowercase letters, numbers, and hyphens.";
        return null;
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug)
    {
        var baseSlug = slug;
        var counter = 1;

        while (await db.Pages.AnyAsync(p => p.Slug == slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugValidRegex();

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex SlugInvalidCharsRegex();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex SlugWhitespaceRegex();
}
