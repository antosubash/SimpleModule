using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Exceptions;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder;

public partial class PageBuilderService(
    PageBuilderDbContext db,
    ILogger<PageBuilderService> logger
) : IPageBuilderContracts
{
    public async Task<IEnumerable<PageSummary>> GetAllPagesAsync() =>
        await db
            .Pages.OrderBy(p => p.Order)
            .ThenBy(p => p.Title)
            .Select(p => new PageSummary
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                IsPublished = p.IsPublished,
                HasDraft = p.DraftContent != null,
                Order = p.Order,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
            })
            .ToListAsync();

    public async Task<Page?> GetPageByIdAsync(PageId id)
    {
        var page = await db.Pages.FirstOrDefaultAsync(p => p.Id == id);
        if (page is null)
        {
            LogPageNotFound(logger, id);
        }

        return page;
    }

    public async Task<Page?> GetPageBySlugAsync(string slug) =>
        await db.Pages.FirstOrDefaultAsync(p => p.Slug == slug);

    public async Task<IEnumerable<PageSummary>> GetPublishedPagesAsync() =>
        await db
            .Pages.Where(p => p.IsPublished)
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Title)
            .Select(p => new PageSummary
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                IsPublished = p.IsPublished,
                HasDraft = p.DraftContent != null,
                Order = p.Order,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
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

        var now = DateTime.UtcNow;
        var page = new Page
        {
            Title = request.Title,
            Slug = slug,
            Content = "{}",
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Pages.Add(page);
        await db.SaveChangesAsync();

        LogPageCreated(logger, page.Id, page.Title);
        return page;
    }

    public async Task<Page> UpdatePageAsync(PageId id, UpdatePageRequest request)
    {
        var page = await db.Pages.FindAsync(id)
            ?? throw new NotFoundException("Page", id);

        page.Title = request.Title;

        var slugToSet = Slugify(request.Slug);
        var slugError = ValidateSlug(slugToSet);
        if (slugError is not null)
            throw new ArgumentException(slugError, nameof(request));

        if (slugToSet != page.Slug && await db.Pages.AnyAsync(p => p.Slug == slugToSet && p.Id != id))
            throw new ArgumentException("Slug is already taken.", nameof(request));

        page.Slug = slugToSet;
        page.Order = request.Order;
        page.IsPublished = request.IsPublished;
        page.MetaDescription = request.MetaDescription;
        page.MetaKeywords = request.MetaKeywords;
        page.OgImage = request.OgImage;
        page.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        LogPageUpdated(logger, page.Id, page.Title);
        return page;
    }

    public async Task<Page> UpdatePageContentAsync(PageId id, UpdatePageContentRequest request)
    {
        var page = await db.Pages.FindAsync(id)
            ?? throw new NotFoundException("Page", id);

        page.DraftContent = request.Content;
        page.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        LogPageContentUpdated(logger, page.Id);
        return page;
    }

    public async Task DeletePageAsync(PageId id)
    {
        var page = await db.Pages.FindAsync(id)
            ?? throw new NotFoundException("Page", id);

        page.DeletedAt = DateTime.UtcNow;
        page.IsPublished = false;
        await db.SaveChangesAsync();

        LogPageDeleted(logger, id);
    }

    public async Task<Page> PublishPageAsync(PageId id)
    {
        var page = await db.Pages.FindAsync(id)
            ?? throw new NotFoundException("Page", id);

        if (!string.IsNullOrEmpty(page.DraftContent))
        {
            page.Content = page.DraftContent;
            page.DraftContent = null;
        }

        page.IsPublished = true;
        page.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        LogPagePublished(logger, page.Id, page.Title);
        return page;
    }

    public async Task<Page> UnpublishPageAsync(PageId id)
    {
        var page = await db.Pages.FindAsync(id)
            ?? throw new NotFoundException("Page", id);

        page.IsPublished = false;
        page.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        LogPageUnpublished(logger, page.Id, page.Title);
        return page;
    }

    public async Task<IEnumerable<PageSummary>> GetTrashedPagesAsync() =>
        await db
            .Pages.IgnoreQueryFilters()
            .Where(p => p.DeletedAt != null)
            .OrderByDescending(p => p.DeletedAt)
            .Select(p => new PageSummary
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                IsPublished = p.IsPublished,
                HasDraft = p.DraftContent != null,
                Order = p.Order,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                DeletedAt = p.DeletedAt,
            })
            .ToListAsync();

    public async Task<Page> RestorePageAsync(PageId id)
    {
        var page = await db.Pages.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt != null)
            ?? throw new NotFoundException("Page", id);

        page.DeletedAt = null;
        page.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        LogPageRestored(logger, page.Id, page.Title);
        return page;
    }

    public async Task PermanentDeletePageAsync(PageId id)
    {
        var page = await db.Pages.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Page", id);

        db.Pages.Remove(page);
        await db.SaveChangesAsync();

        LogPagePermanentlyDeleted(logger, id);
    }

    public async Task<IEnumerable<PageTemplate>> GetAllTemplatesAsync() =>
        await db.Templates.OrderBy(t => t.Name).ToListAsync();

    public async Task<PageTemplate> CreateTemplateAsync(CreatePageTemplateRequest request)
    {
        var template = new PageTemplate
        {
            Name = request.Name,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
        };

        db.Templates.Add(template);
        await db.SaveChangesAsync();

        return template;
    }

    public async Task DeleteTemplateAsync(PageTemplateId id)
    {
        var template = await db.Templates.FindAsync(id)
            ?? throw new NotFoundException("PageTemplate", id);

        db.Templates.Remove(template);
        await db.SaveChangesAsync();
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

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugValidRegex();

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

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex SlugInvalidCharsRegex();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex SlugWhitespaceRegex();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Page with ID {PageId} not found")]
    private static partial void LogPageNotFound(ILogger logger, PageId pageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} created: {PageTitle}")]
    private static partial void LogPageCreated(ILogger logger, PageId pageId, string pageTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} updated: {PageTitle}")]
    private static partial void LogPageUpdated(ILogger logger, PageId pageId, string pageTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} content updated")]
    private static partial void LogPageContentUpdated(ILogger logger, PageId pageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} deleted")]
    private static partial void LogPageDeleted(ILogger logger, PageId pageId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Page {PageId} published: {PageTitle}"
    )]
    private static partial void LogPagePublished(ILogger logger, PageId pageId, string pageTitle);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Page {PageId} unpublished: {PageTitle}"
    )]
    private static partial void LogPageUnpublished(ILogger logger, PageId pageId, string pageTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} restored: {PageTitle}")]
    private static partial void LogPageRestored(ILogger logger, PageId pageId, string pageTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} permanently deleted")]
    private static partial void LogPagePermanentlyDeleted(ILogger logger, PageId pageId);
}
