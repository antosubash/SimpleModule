# Page Enhancements Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add metadata/SEO fields, page templates, tags, soft delete, and slug validation to the PageBuilder module.

**Architecture:** Extend the Page entity with metadata and soft-delete fields. Add new PageTemplate and PageTag entities with a many-to-many join. Add slug validation in the service layer. All frontend components import types from the auto-generated `types.ts`.

**Tech Stack:** EF Core (SQLite/PostgreSQL), ASP.NET Minimal APIs, Vogen (strongly-typed IDs), React + @simplemodule/ui, xUnit + FluentAssertions

---

### Task 1: Add Metadata fields to Page entity

**Files:**
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/Page.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/EntityConfigurations/PageConfiguration.cs`
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/UpdatePageRequest.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/PageBuilderService.cs`

**Step 1: Add metadata properties to Page entity**

In `Page.cs`, add after `DraftContent` (line 12):
```csharp
public string? MetaDescription { get; set; }
public string? MetaKeywords { get; set; }
public string? OgImage { get; set; }
```

**Step 2: Configure metadata in entity configuration**

In `PageConfiguration.cs`, add after `DraftContent` config (line 17):
```csharp
builder.Property(p => p.MetaDescription).IsRequired(false).HasMaxLength(300);
builder.Property(p => p.MetaKeywords).IsRequired(false).HasMaxLength(500);
builder.Property(p => p.OgImage).IsRequired(false).HasMaxLength(500);
```

**Step 3: Add metadata to UpdatePageRequest**

In `UpdatePageRequest.cs`, add after `IsPublished` (line 11):
```csharp
public string? MetaDescription { get; set; }
public string? MetaKeywords { get; set; }
public string? OgImage { get; set; }
```

**Step 4: Update UpdatePageAsync in service**

In `PageBuilderService.cs`, in `UpdatePageAsync` (around line 89-104), add after `page.IsPublished = request.IsPublished;` (line 97):
```csharp
page.MetaDescription = request.MetaDescription;
page.MetaKeywords = request.MetaKeywords;
page.OgImage = request.OgImage;
```

**Step 5: Build and run tests**

Run: `dotnet build && dotnet test modules/PageBuilder/tests/PageBuilder.Tests`

**Step 6: Commit**
```
feat(pagebuilder): add MetaDescription, MetaKeywords, OgImage to Page entity
```

---

### Task 2: Add Soft Delete to Page

**Files:**
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/Page.cs`
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/PageSummary.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/EntityConfigurations/PageConfiguration.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/PageBuilderDbContext.cs`
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/IPageBuilderContracts.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/PageBuilderService.cs`
- Create: `modules/PageBuilder/src/PageBuilder/Endpoints/Pages/TrashEndpoint.cs`
- Create: `modules/PageBuilder/src/PageBuilder/Endpoints/Pages/RestoreEndpoint.cs`
- Create: `modules/PageBuilder/src/PageBuilder/Endpoints/Pages/PermanentDeleteEndpoint.cs`
- Modify: `modules/PageBuilder/tests/PageBuilder.Tests/PageBuilderServiceTests.cs`

**Step 1: Add DeletedAt to entities**

In `Page.cs`, add after `UpdatedAt` (line 16):
```csharp
public DateTime? DeletedAt { get; set; }
```

In `PageSummary.cs`, add after `UpdatedAt` (line 15):
```csharp
public DateTime? DeletedAt { get; set; }
```

**Step 2: Configure DeletedAt and add global query filter**

In `PageConfiguration.cs`, add after `Order` config (line 19):
```csharp
builder.Property(p => p.DeletedAt).IsRequired(false);
```

In `PageBuilderDbContext.cs`, add a global query filter inside `OnModelCreating` after line 18:
```csharp
modelBuilder.Entity<Page>().HasQueryFilter(p => p.DeletedAt == null);
```

**Step 3: Update service — soft delete instead of hard delete**

In `PageBuilderService.cs`, replace `DeletePageAsync` (lines 120-129):
```csharp
public async Task DeletePageAsync(PageId id)
{
    var page = await db.Pages.FindAsync(id)
        ?? throw new NotFoundException("Page", id);

    page.DeletedAt = DateTime.UtcNow;
    page.IsPublished = false;
    await db.SaveChangesAsync();

    LogPageDeleted(logger, id);
}
```

**Step 4: Add new interface methods**

In `IPageBuilderContracts.cs`, add after `UnpublishPageAsync` (line 14):
```csharp
Task<IEnumerable<PageSummary>> GetTrashedPagesAsync();
Task<Page> RestorePageAsync(PageId id);
Task PermanentDeletePageAsync(PageId id);
```

**Step 5: Implement new methods in service**

In `PageBuilderService.cs`, add before the `Slugify` method (before line 165):
```csharp
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
```

Add logger messages at the end of the class (before closing brace):
```csharp
[LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} restored: {PageTitle}")]
private static partial void LogPageRestored(ILogger logger, PageId pageId, string pageTitle);

[LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} permanently deleted")]
private static partial void LogPagePermanentlyDeleted(ILogger logger, PageId pageId);
```

**Step 6: Create trash/restore/permanent-delete endpoints**

Create `modules/PageBuilder/src/PageBuilder/Endpoints/Pages/TrashEndpoint.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class TrashEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/trash",
                async (IPageBuilderContracts pageBuilder) =>
                    TypedResults.Ok(await pageBuilder.GetTrashedPagesAsync())
            )
            .RequirePermission(PageBuilderPermissions.Delete);
}
```

Create `modules/PageBuilder/src/PageBuilder/Endpoints/Pages/RestoreEndpoint.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class RestoreEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/{id}/restore",
                async (PageId id, IPageBuilderContracts pageBuilder) =>
                {
                    var page = await pageBuilder.RestorePageAsync(id);
                    return TypedResults.Ok(page);
                }
            )
            .RequirePermission(PageBuilderPermissions.Delete);
}
```

Create `modules/PageBuilder/src/PageBuilder/Endpoints/Pages/PermanentDeleteEndpoint.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Pages;

public class PermanentDeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}/permanent",
                async (PageId id, IPageBuilderContracts pageBuilder) =>
                {
                    await pageBuilder.PermanentDeletePageAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(PageBuilderPermissions.Delete);
}
```

**Step 7: Add unit tests for soft delete**

In `PageBuilderServiceTests.cs`, update the existing `DeletePage_Removes` test and add new tests:
```csharp
[Fact]
public async Task DeletePage_SoftDeletes_SetsDeletedAt()
{
    var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "To Soft Delete" });

    await _sut.DeletePageAsync(page.Id);

    // Soft deleted — not in normal queries
    var found = await _sut.GetPageByIdAsync(page.Id);
    found.Should().BeNull();

    // But in trash
    var trashed = await _sut.GetTrashedPagesAsync();
    trashed.Should().ContainSingle().Which.Title.Should().Be("To Soft Delete");
}

[Fact]
public async Task RestorePage_ClearsDeletedAt()
{
    var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "To Restore" });
    await _sut.DeletePageAsync(page.Id);

    var restored = await _sut.RestorePageAsync(page.Id);

    restored.DeletedAt.Should().BeNull();
    var found = await _sut.GetPageByIdAsync(restored.Id);
    found.Should().NotBeNull();
}

[Fact]
public async Task PermanentDelete_RemovesCompletely()
{
    var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "To Perm Delete" });
    await _sut.DeletePageAsync(page.Id);

    await _sut.PermanentDeletePageAsync(page.Id);

    var trashed = await _sut.GetTrashedPagesAsync();
    trashed.Should().BeEmpty();
}
```

Remove the old `DeletePage_Removes` and `DeletePage_NonExistent_ThrowsNotFoundException` tests since delete behavior changed.

**Step 8: Build and run tests**

Run: `dotnet build && dotnet test modules/PageBuilder/tests/PageBuilder.Tests`

**Step 9: Commit**
```
feat(pagebuilder): add soft delete with trash, restore, permanent delete
```

---

### Task 3: Add Slug Validation

**Files:**
- Modify: `modules/PageBuilder/src/PageBuilder/PageBuilderService.cs`
- Modify: `modules/PageBuilder/tests/PageBuilder.Tests/PageBuilderServiceTests.cs`

**Step 1: Add a SlugValidationException or use ArgumentException**

In `PageBuilderService.cs`, add a static validation method after `Slugify` (around line 174):
```csharp
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
```

**Step 2: Apply validation in CreatePageAsync**

In `CreatePageAsync` (around line 64-87), after computing the slug and before `EnsureUniqueSlugAsync`, add validation:
```csharp
var validationError = ValidateSlug(slug);
if (validationError is not null)
    throw new ArgumentException(validationError, nameof(request));
```

**Step 3: Apply validation in UpdatePageAsync**

In `UpdatePageAsync` (around line 89-104), before `page.Slug = request.Slug;` (line 95), add:
```csharp
var slugToSet = Slugify(request.Slug);
var slugError = ValidateSlug(slugToSet);
if (slugError is not null)
    throw new ArgumentException(slugError, nameof(request));

if (slugToSet != page.Slug && await db.Pages.AnyAsync(p => p.Slug == slugToSet && p.Id != id))
    throw new ArgumentException("Slug is already taken.", nameof(request));

page.Slug = slugToSet;
```

Remove the old `page.Slug = request.Slug;` line since it's now handled above.

**Step 4: Add unit tests**

```csharp
[Fact]
public void ValidateSlug_ValidSlug_ReturnsNull()
{
    PageBuilderService.ValidateSlug("hello-world").Should().BeNull();
    PageBuilderService.ValidateSlug("abc").Should().BeNull();
    PageBuilderService.ValidateSlug("my-page-123").Should().BeNull();
}

[Fact]
public void ValidateSlug_TooShort_ReturnsError()
{
    PageBuilderService.ValidateSlug("ab").Should().NotBeNull();
}

[Fact]
public void ValidateSlug_InvalidChars_ReturnsError()
{
    PageBuilderService.ValidateSlug("Hello World").Should().NotBeNull();
    PageBuilderService.ValidateSlug("has_underscore").Should().NotBeNull();
}

[Fact]
public async Task UpdatePage_DuplicateSlug_ThrowsArgumentException()
{
    await _sut.CreatePageAsync(new CreatePageRequest { Title = "Page One", Slug = "page-one" });
    var page2 = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Page Two", Slug = "page-two" });

    var act = () => _sut.UpdatePageAsync(page2.Id, new UpdatePageRequest
    {
        Title = "Page Two",
        Slug = "page-one",
        Order = 0,
        IsPublished = false,
    });

    await act.Should().ThrowAsync<ArgumentException>().WithMessage("*already taken*");
}
```

**Step 5: Build and run tests**

Run: `dotnet build && dotnet test modules/PageBuilder/tests/PageBuilder.Tests`

**Step 6: Commit**
```
feat(pagebuilder): add slug validation with format and uniqueness checks
```

---

### Task 4: Add PageTemplate entity and API

**Files:**
- Create: `modules/PageBuilder/src/PageBuilder.Contracts/PageTemplateId.cs`
- Create: `modules/PageBuilder/src/PageBuilder.Contracts/PageTemplate.cs`
- Create: `modules/PageBuilder/src/PageBuilder.Contracts/CreatePageTemplateRequest.cs`
- Create: `modules/PageBuilder/src/PageBuilder/EntityConfigurations/PageTemplateConfiguration.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/PageBuilderDbContext.cs`
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/IPageBuilderContracts.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/PageBuilderService.cs`
- Create: `modules/PageBuilder/src/PageBuilder/Endpoints/Templates/GetAllTemplatesEndpoint.cs`
- Create: `modules/PageBuilder/src/PageBuilder/Endpoints/Templates/CreateTemplateEndpoint.cs`
- Create: `modules/PageBuilder/src/PageBuilder/Endpoints/Templates/DeleteTemplateEndpoint.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/PageBuilderModule.cs`
- Modify: `modules/PageBuilder/tests/PageBuilder.Tests/PageBuilderServiceTests.cs`

**Step 1: Create PageTemplateId**

Create `modules/PageBuilder/src/PageBuilder.Contracts/PageTemplateId.cs`:
```csharp
using Vogen;

namespace SimpleModule.PageBuilder.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct PageTemplateId;
```

**Step 2: Create PageTemplate entity**

Create `modules/PageBuilder/src/PageBuilder.Contracts/PageTemplate.cs`:
```csharp
using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class PageTemplate
{
    public PageTemplateId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

**Step 3: Create request DTO**

Create `modules/PageBuilder/src/PageBuilder.Contracts/CreatePageTemplateRequest.cs`:
```csharp
using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class CreatePageTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
```

**Step 4: Create entity configuration**

Create `modules/PageBuilder/src/PageBuilder/EntityConfigurations/PageTemplateConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.EntityConfigurations;

public class PageTemplateConfiguration : IEntityTypeConfiguration<PageTemplate>
{
    public void Configure(EntityTypeBuilder<PageTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(t => t.Name).IsUnique();
        builder.Property(t => t.Content).IsRequired();
    }
}
```

**Step 5: Register in DbContext**

In `PageBuilderDbContext.cs`, add after `Pages` DbSet (line 14):
```csharp
public DbSet<PageTemplate> Templates => Set<PageTemplate>();
```

In `OnModelCreating`, add after `PageConfiguration` (line 18):
```csharp
modelBuilder.ApplyConfiguration(new PageTemplateConfiguration());
```

In `ConfigureConventions`, add after `PageId` converter (line 26):
```csharp
configurationBuilder
    .Properties<PageTemplateId>()
    .HaveConversion<PageTemplateId.EfCoreValueConverter, PageTemplateId.EfCoreValueComparer>();
```

**Step 6: Add interface methods**

In `IPageBuilderContracts.cs`, add after `PermanentDeletePageAsync`:
```csharp
Task<IEnumerable<PageTemplate>> GetAllTemplatesAsync();
Task<PageTemplate> CreateTemplateAsync(CreatePageTemplateRequest request);
Task DeleteTemplateAsync(PageTemplateId id);
```

**Step 7: Implement in service**

In `PageBuilderService.cs`, add before the `Slugify` method:
```csharp
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
```

**Step 8: Create API endpoints**

Create `modules/PageBuilder/src/PageBuilder/Endpoints/Templates/GetAllTemplatesEndpoint.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Templates;

public class GetAllTemplatesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/templates",
                async (IPageBuilderContracts pageBuilder) =>
                    TypedResults.Ok(await pageBuilder.GetAllTemplatesAsync())
            )
            .RequirePermission(PageBuilderPermissions.View);
}
```

Create `modules/PageBuilder/src/PageBuilder/Endpoints/Templates/CreateTemplateEndpoint.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Templates;

public class CreateTemplateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/templates",
                async (CreatePageTemplateRequest request, IPageBuilderContracts pageBuilder) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Name))
                        return Results.BadRequest("Template name is required.");

                    var template = await pageBuilder.CreateTemplateAsync(request);
                    return TypedResults.Created($"/api/pagebuilder/templates/{template.Id}", template);
                }
            )
            .RequirePermission(PageBuilderPermissions.Create);
}
```

Create `modules/PageBuilder/src/PageBuilder/Endpoints/Templates/DeleteTemplateEndpoint.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Templates;

public class DeleteTemplateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/templates/{id}",
                async (PageTemplateId id, IPageBuilderContracts pageBuilder) =>
                {
                    await pageBuilder.DeleteTemplateAsync(id);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(PageBuilderPermissions.Delete);
}
```

**Step 9: Register PageTemplateId converter in Host**

In `template/SimpleModule.Host/HostDbContext.Conventions.cs`, add the PageTemplateId converter alongside the existing PageId one:
```csharp
configurationBuilder
    .Properties<PageTemplateId>()
    .HaveConversion<PageTemplateId.EfCoreValueConverter, PageTemplateId.EfCoreValueComparer>();
```

Add `using SimpleModule.PageBuilder.Contracts;` if not already present (it should be).

**Step 10: Add unit tests**

```csharp
[Fact]
public async Task CreateTemplate_SavesNameAndContent()
{
    var template = await _sut.CreateTemplateAsync(new CreatePageTemplateRequest
    {
        Name = "Landing Page",
        Content = """{"content":[{"type":"Hero"}],"root":{}}""",
    });

    template.Name.Should().Be("Landing Page");
    template.Content.Should().Contain("Hero");
}

[Fact]
public async Task GetAllTemplates_ReturnsOrderedByName()
{
    await _sut.CreateTemplateAsync(new CreatePageTemplateRequest { Name = "Zzz", Content = "{}" });
    await _sut.CreateTemplateAsync(new CreatePageTemplateRequest { Name = "Aaa", Content = "{}" });

    var templates = await _sut.GetAllTemplatesAsync();

    templates.First().Name.Should().Be("Aaa");
}

[Fact]
public async Task DeleteTemplate_Removes()
{
    var template = await _sut.CreateTemplateAsync(new CreatePageTemplateRequest { Name = "To Delete", Content = "{}" });

    await _sut.DeleteTemplateAsync(template.Id);

    var all = await _sut.GetAllTemplatesAsync();
    all.Should().BeEmpty();
}
```

**Step 11: Build and run tests**

Run: `dotnet build && dotnet test modules/PageBuilder/tests/PageBuilder.Tests`

**Step 12: Commit**
```
feat(pagebuilder): add PageTemplate entity with CRUD API
```

---

### Task 5: Add Tags (PageTag entity + many-to-many)

**Files:**
- Create: `modules/PageBuilder/src/PageBuilder.Contracts/PageTagId.cs`
- Create: `modules/PageBuilder/src/PageBuilder.Contracts/PageTag.cs`
- Create: `modules/PageBuilder/src/PageBuilder/EntityConfigurations/PageTagConfiguration.cs`
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/Page.cs`
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/PageSummary.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/PageBuilderDbContext.cs`
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/IPageBuilderContracts.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/PageBuilderService.cs`
- Create: `modules/PageBuilder/src/PageBuilder/Endpoints/Tags/GetAllTagsEndpoint.cs`
- Create: `modules/PageBuilder/src/PageBuilder/Endpoints/Tags/AddTagToPageEndpoint.cs`
- Create: `modules/PageBuilder/src/PageBuilder/Endpoints/Tags/RemoveTagFromPageEndpoint.cs`
- Modify: `modules/PageBuilder/tests/PageBuilder.Tests/PageBuilderServiceTests.cs`

**Step 1: Create PageTagId**

Create `modules/PageBuilder/src/PageBuilder.Contracts/PageTagId.cs`:
```csharp
using Vogen;

namespace SimpleModule.PageBuilder.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct PageTagId;
```

**Step 2: Create PageTag entity**

Create `modules/PageBuilder/src/PageBuilder.Contracts/PageTag.cs`:
```csharp
using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class PageTag
{
    public PageTagId Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

**Step 3: Add Tags navigation to Page**

In `Page.cs`, add after `UpdatedAt` (before `DeletedAt`):
```csharp
public List<PageTag> Tags { get; set; } = [];
```

In `PageSummary.cs`, add after `DeletedAt`:
```csharp
public List<string> Tags { get; set; } = [];
```

**Step 4: Create entity configuration**

Create `modules/PageBuilder/src/PageBuilder/EntityConfigurations/PageTagConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.EntityConfigurations;

public class PageTagConfiguration : IEntityTypeConfiguration<PageTag>
{
    public void Configure(EntityTypeBuilder<PageTag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
```

**Step 5: Register in DbContext**

In `PageBuilderDbContext.cs`:
- Add DbSet: `public DbSet<PageTag> Tags => Set<PageTag>();`
- In `OnModelCreating`, add: `modelBuilder.ApplyConfiguration(new PageTagConfiguration());`
- Configure many-to-many in `OnModelCreating`:
```csharp
modelBuilder.Entity<Page>()
    .HasMany(p => p.Tags)
    .WithMany()
    .UsingEntity("PagePageTag");
```
- In `ConfigureConventions`, add:
```csharp
configurationBuilder
    .Properties<PageTagId>()
    .HaveConversion<PageTagId.EfCoreValueConverter, PageTagId.EfCoreValueComparer>();
```

**Step 6: Add interface methods**

In `IPageBuilderContracts.cs`, add:
```csharp
Task<IEnumerable<PageTag>> GetAllTagsAsync();
Task<PageTag> GetOrCreateTagAsync(string name);
Task AddTagToPageAsync(PageId pageId, string tagName);
Task RemoveTagFromPageAsync(PageId pageId, PageTagId tagId);
```

**Step 7: Implement in service**

In `PageBuilderService.cs`, add before `Slugify`:
```csharp
public async Task<IEnumerable<PageTag>> GetAllTagsAsync() =>
    await db.Tags.OrderBy(t => t.Name).ToListAsync();

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
    var page = await db.Pages.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == pageId)
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
    var page = await db.Pages.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == pageId)
        ?? throw new NotFoundException("Page", pageId);

    var tag = page.Tags.FirstOrDefault(t => t.Id == tagId);
    if (tag is not null)
    {
        page.Tags.Remove(tag);
        await db.SaveChangesAsync();
    }
}
```

**Step 8: Update PageSummary projections**

In both `GetAllPagesAsync` and `GetPublishedPagesAsync` projections, add:
```csharp
Tags = p.Tags.Select(t => t.Name).ToList(),
```

**Step 9: Create tag endpoints**

Create `modules/PageBuilder/src/PageBuilder/Endpoints/Tags/GetAllTagsEndpoint.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Tags;

public class GetAllTagsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/tags",
                async (IPageBuilderContracts pageBuilder) =>
                    TypedResults.Ok(await pageBuilder.GetAllTagsAsync())
            )
            .RequirePermission(PageBuilderPermissions.View);
}
```

Create `modules/PageBuilder/src/PageBuilder/Endpoints/Tags/AddTagToPageEndpoint.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Tags;

public class AddTagToPageEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/{id}/tags",
                async (PageId id, AddTagRequest request, IPageBuilderContracts pageBuilder) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Name))
                        return Results.BadRequest("Tag name is required.");

                    await pageBuilder.AddTagToPageAsync(id, request.Name);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(PageBuilderPermissions.Update);
}
```

Create the AddTagRequest DTO — `modules/PageBuilder/src/PageBuilder.Contracts/AddTagRequest.cs`:
```csharp
using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class AddTagRequest
{
    public string Name { get; set; } = string.Empty;
}
```

Create `modules/PageBuilder/src/PageBuilder/Endpoints/Tags/RemoveTagFromPageEndpoint.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Tags;

public class RemoveTagFromPageEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(
                "/{id}/tags/{tagId}",
                async (PageId id, PageTagId tagId, IPageBuilderContracts pageBuilder) =>
                {
                    await pageBuilder.RemoveTagFromPageAsync(id, tagId);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(PageBuilderPermissions.Update);
}
```

**Step 10: Register PageTagId converter in Host**

In `template/SimpleModule.Host/HostDbContext.Conventions.cs`, add:
```csharp
configurationBuilder
    .Properties<PageTagId>()
    .HaveConversion<PageTagId.EfCoreValueConverter, PageTagId.EfCoreValueComparer>();
```

**Step 11: Add unit tests**

```csharp
[Fact]
public async Task AddTagToPage_CreatesTagAndAssociates()
{
    var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Tagged Page" });

    await _sut.AddTagToPageAsync(page.Id, "blog");

    var tags = await _sut.GetAllTagsAsync();
    tags.Should().ContainSingle().Which.Name.Should().Be("blog");
}

[Fact]
public async Task AddTagToPage_DuplicateTag_DoesNotDuplicate()
{
    var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Tagged Page" });

    await _sut.AddTagToPageAsync(page.Id, "blog");
    await _sut.AddTagToPageAsync(page.Id, "blog");

    var tags = await _sut.GetAllTagsAsync();
    tags.Should().ContainSingle();
}

[Fact]
public async Task RemoveTagFromPage_RemovesAssociation()
{
    var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Tagged Page" });
    await _sut.AddTagToPageAsync(page.Id, "blog");

    var tags = await _sut.GetAllTagsAsync();
    await _sut.RemoveTagFromPageAsync(page.Id, tags.First().Id);

    var updatedPage = await _sut.GetPageByIdAsync(page.Id);
    updatedPage!.Tags.Should().BeEmpty();
}
```

**Step 12: Build and run tests**

Run: `dotnet build && dotnet test modules/PageBuilder/tests/PageBuilder.Tests`

**Step 13: Commit**
```
feat(pagebuilder): add PageTag entity with many-to-many and tag endpoints
```

---

### Task 6: Frontend updates — use generated types, show tags/metadata/trash

**Files:**
- Modify: `modules/PageBuilder/src/PageBuilder/Views/Manage.tsx`
- Modify: `modules/PageBuilder/src/PageBuilder/Views/Editor.tsx`
- Modify: `modules/PageBuilder/src/PageBuilder/Views/Viewer.tsx`
- Modify: `modules/PageBuilder/src/PageBuilder/Views/PagesList.tsx`

**Step 1: Update all views to import from generated types**

In ALL view files (Manage.tsx, Editor.tsx, Viewer.tsx, PagesList.tsx), remove inline interface definitions and import from `../types` instead:

For `Manage.tsx` — remove the `PageSummary` interface (lines 13-22) and add at top:
```tsx
import type { PageSummary } from '../types';
```

For `Editor.tsx` — remove the `Page` interface (lines 8-13) and add:
```tsx
import type { Page } from '../types';
```

For `Viewer.tsx` — remove the `Page` interface (lines 6-11) and add:
```tsx
import type { Page } from '../types';
```

For `PagesList.tsx` — remove the `PageSummary` interface and add:
```tsx
import type { PageSummary } from '../types';
```

Note: The `Props` interfaces stay inline since they're component-specific.

**Step 2: Update Manage.tsx to show tags and trash link**

In the table header, add a "Tags" column after "Status":
```tsx
<TableHead>Tags</TableHead>
```

In the table body, add a Tags cell after the status cell:
```tsx
<TableCell>
  <div className="flex gap-1 flex-wrap">
    {page.tags.map((tag) => (
      <Badge key={tag} variant="outline">{tag}</Badge>
    ))}
  </div>
</TableCell>
```

Add a "Trash" link at the bottom of the page or next to the header.

**Step 3: Build the frontend**

Run: `cd modules/PageBuilder/src/PageBuilder && npx vite build`

**Step 4: Commit**
```
feat(pagebuilder): use generated types in all frontend views, show tags
```

---

### Task 7: Generate EF migration and final verification

**Files:**
- New migration files in `template/SimpleModule.Host/Migrations/`

**Step 1: Generate migration**

Run:
```bash
dotnet ef migrations add AddPageEnhancements --project template/SimpleModule.Host --context HostDbContext
```

**Step 2: Build entire solution**

Run: `dotnet build`

**Step 3: Run all tests**

Run: `dotnet test`

**Step 4: Commit**
```
feat(pagebuilder): add migration for metadata, soft delete, templates, tags
```
