# PageBuilder Draft Support Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a DraftContent column so editors can save work-in-progress without affecting the live published page, with separate draft preview route.

**Architecture:** Add nullable `DraftContent` to the Page entity. Editor saves to DraftContent. Publish copies DraftContent → Content and clears it. Viewer at `/p/{slug}` shows Content; new route `/p/{slug}/draft` shows DraftContent (admin only).

**Tech Stack:** EF Core (SQLite/PostgreSQL), ASP.NET Minimal APIs, React + Puck editor, xUnit + FluentAssertions

---

### Task 1: Add DraftContent to Entity and Contracts

**Files:**
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/Page.cs`
- Modify: `modules/PageBuilder/src/PageBuilder.Contracts/PageSummary.cs`
- Modify: `modules/PageBuilder/src/PageBuilder/EntityConfigurations/PageConfiguration.cs`

**Step 1: Add DraftContent property to Page entity**

In `Page.cs`, add after `Content` property (line 11):
```csharp
public string? DraftContent { get; set; }
```

**Step 2: Add HasDraft computed property to PageSummary**

In `PageSummary.cs`, add after `IsPublished` (line 11):
```csharp
public bool HasDraft { get; set; }
```

**Step 3: Configure DraftContent in entity configuration**

In `PageConfiguration.cs`, add after the `Content` configuration (line 16):
```csharp
builder.Property(p => p.DraftContent).IsRequired(false);
```

**Step 4: Commit**
```bash
git add modules/PageBuilder/src/PageBuilder.Contracts/Page.cs modules/PageBuilder/src/PageBuilder.Contracts/PageSummary.cs modules/PageBuilder/src/PageBuilder/EntityConfigurations/PageConfiguration.cs
git commit -m "feat(pagebuilder): add DraftContent column to Page entity"
```

---

### Task 2: Update Service — DraftContent in projections

**Files:**
- Modify: `modules/PageBuilder/src/PageBuilder/PageBuilderService.cs`

**Step 1: Write failing test — CreatePage sets DraftContent to null**

In `modules/PageBuilder/tests/PageBuilder.Tests/PageBuilderServiceTests.cs`, add:
```csharp
[Fact]
public async Task CreatePage_DraftContentIsNull()
{
    var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Draft Test" });

    page.DraftContent.Should().BeNull();
}
```

**Step 2: Run test to verify it passes (DraftContent defaults to null, no code change needed)**

Run: `dotnet test modules/PageBuilder/tests/PageBuilder.Tests --filter CreatePage_DraftContentIsNull`

**Step 3: Write failing test — UpdateContent saves to DraftContent instead of Content**

```csharp
[Fact]
public async Task UpdateContent_SavesToDraftContent_NotContent()
{
    var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Draft Save" });

    var updated = await _sut.UpdatePageContentAsync(
        page.Id,
        new UpdatePageContentRequest { Content = """{"content":[{"type":"Text"}],"root":{}}""" }
    );

    updated.DraftContent.Should().Be("""{"content":[{"type":"Text"}],"root":{}}""");
    updated.Content.Should().Be("{}"); // Original content unchanged
}
```

**Step 4: Run test to verify it fails**

Run: `dotnet test modules/PageBuilder/tests/PageBuilder.Tests --filter UpdateContent_SavesToDraftContent_NotContent`

**Step 5: Update UpdatePageContentAsync in PageBuilderService.cs**

Change `UpdatePageContentAsync` (line 104-116) — replace `page.Content = request.Content` with `page.DraftContent = request.Content`:
```csharp
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
```

**Step 6: Run test to verify it passes**

**Step 7: Write failing test — Publish copies DraftContent to Content and clears DraftContent**

```csharp
[Fact]
public async Task PublishPage_CopiesDraftToContent_ClearsDraft()
{
    var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Publish Draft" });
    await _sut.UpdatePageContentAsync(page.Id, new UpdatePageContentRequest { Content = """{"content":[{"type":"Hero"}],"root":{}}""" });

    var published = await _sut.PublishPageAsync(page.Id);

    published.Content.Should().Be("""{"content":[{"type":"Hero"}],"root":{}}""");
    published.DraftContent.Should().BeNull();
    published.IsPublished.Should().BeTrue();
}
```

**Step 8: Run test to verify it fails**

**Step 9: Update PublishPageAsync in PageBuilderService.cs**

Replace `PublishPageAsync` (lines 129-141):
```csharp
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
```

**Step 10: Run all tests to verify they pass**

Run: `dotnet test modules/PageBuilder/tests/PageBuilder.Tests`

Note: The existing `UpdateContent_SavesJsonAndUpdatesTimestamp` test will need updating — it now expects DraftContent instead of Content. Update it:
```csharp
[Fact]
public async Task UpdateContent_SavesJsonAndUpdatesTimestamp()
{
    var page = await _sut.CreatePageAsync(new CreatePageRequest { Title = "Content Test" });
    var before = page.UpdatedAt;

    var updated = await _sut.UpdatePageContentAsync(
        page.Id,
        new UpdatePageContentRequest { Content = """{"content":[],"root":{}}""" }
    );

    updated.DraftContent.Should().Be("""{"content":[],"root":{}}""");
    updated.UpdatedAt.Should().BeOnOrAfter(before);
}
```

**Step 11: Update HasDraft in PageSummary projections**

In `PageBuilderService.cs`, update both `GetAllPagesAsync` (lines 15-29) and `GetPublishedPagesAsync` (lines 45-60) projections to include `HasDraft`:
```csharp
HasDraft = !string.IsNullOrEmpty(p.DraftContent),
```
Add this line after `IsPublished = p.IsPublished,` in both methods.

**Step 12: Run all tests**

Run: `dotnet test modules/PageBuilder/tests/PageBuilder.Tests`

**Step 13: Commit**
```bash
git add modules/PageBuilder/src/PageBuilder/PageBuilderService.cs modules/PageBuilder/tests/PageBuilder.Tests/PageBuilderServiceTests.cs
git commit -m "feat(pagebuilder): editor saves to DraftContent, publish copies draft to live"
```

---

### Task 3: Update EditorEndpoint to send DraftContent

**Files:**
- Modify: `modules/PageBuilder/src/PageBuilder/Views/EditorEndpoint.cs`

**Step 1: Update the edit route to send DraftContent with fallback to Content**

Replace the edit route handler (lines 20-33) in `EditorEndpoint.cs`:
```csharp
app.MapGet(
        "/admin/pages/{id}/edit",
        async (PageId id, IPageBuilderContracts pageBuilder) =>
        {
            var page = await pageBuilder.GetPageByIdAsync(id);
            if (page is null)
            {
                return Results.NotFound();
            }

            // Editor works on draft content, falling back to published content
            var editorPage = new Page
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                Content = page.DraftContent ?? page.Content,
                DraftContent = page.DraftContent,
                IsPublished = page.IsPublished,
                Order = page.Order,
                CreatedAt = page.CreatedAt,
                UpdatedAt = page.UpdatedAt,
            };

            return Inertia.Render("PageBuilder/Editor", new { page = editorPage });
        }
    )
    .RequireAuthorization(policy => policy.RequireRole("Admin"));
```

**Step 2: Build to verify compilation**

Run: `dotnet build modules/PageBuilder/src/PageBuilder`

**Step 3: Commit**
```bash
git add modules/PageBuilder/src/PageBuilder/Views/EditorEndpoint.cs
git commit -m "feat(pagebuilder): editor loads DraftContent with fallback to Content"
```

---

### Task 4: Add Draft Viewer Route

**Files:**
- Create: `modules/PageBuilder/src/PageBuilder/Views/ViewerDraftEndpoint.cs`

**Step 1: Write failing integration test for draft viewer**

In `modules/PageBuilder/tests/PageBuilder.Tests/PageEndpointTests.cs`, add:
```csharp
[Fact]
public async Task DraftViewer_WithAdminRole_ReturnsOk()
{
    var client = _factory.CreateAuthenticatedClient(
        [PageBuilderPermissions.Create, PageBuilderPermissions.Update],
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
    );

    var createResponse = await client.PostAsJsonAsync(
        "/api/pagebuilder",
        new CreatePageRequest { Title = "Draft Preview Test", Slug = "draft-preview-test" }
    );
    var created = await createResponse.Content.ReadFromJsonAsync<Page>();

    await client.PutAsJsonAsync(
        $"/api/pagebuilder/{created!.Id}/content",
        new UpdatePageContentRequest { Content = """{"content":[{"type":"Text"}],"root":{}}""" }
    );

    var response = await client.GetAsync("/p/draft-preview-test/draft");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}

[Fact]
public async Task DraftViewer_Anonymous_Returns401Or302()
{
    var client = _factory.CreateClient();

    var response = await client.GetAsync("/p/some-page/draft");
    response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test modules/PageBuilder/tests/PageBuilder.Tests --filter DraftViewer`

**Step 3: Create ViewerDraftEndpoint.cs**

Create `modules/PageBuilder/src/PageBuilder/Views/ViewerDraftEndpoint.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Views;

public class ViewerDraftEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/p/{slug}/draft",
                async (string slug, IPageBuilderContracts pageBuilder) =>
                {
                    var page = await pageBuilder.GetPageBySlugAsync(slug);
                    if (page is null)
                    {
                        return Results.NotFound();
                    }

                    // Show draft content if available, otherwise fall back to published content
                    var viewerPage = new Page
                    {
                        Id = page.Id,
                        Title = page.Title,
                        Slug = page.Slug,
                        Content = page.DraftContent ?? page.Content,
                        IsPublished = page.IsPublished,
                        Order = page.Order,
                        CreatedAt = page.CreatedAt,
                        UpdatedAt = page.UpdatedAt,
                    };

                    return Inertia.Render(
                        "PageBuilder/Viewer",
                        new { page = viewerPage, isDraft = true }
                    );
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test modules/PageBuilder/tests/PageBuilder.Tests --filter DraftViewer`

**Step 5: Commit**
```bash
git add modules/PageBuilder/src/PageBuilder/Views/ViewerDraftEndpoint.cs modules/PageBuilder/tests/PageBuilder.Tests/PageEndpointTests.cs
git commit -m "feat(pagebuilder): add /p/{slug}/draft route for admin draft preview"
```

---

### Task 5: Update Frontend — Viewer draft banner

**Files:**
- Modify: `modules/PageBuilder/src/PageBuilder/Views/Viewer.tsx`

**Step 1: Add isDraft prop and banner to Viewer component**

Replace the full `Viewer.tsx`:
```tsx
import { Render } from '@measured/puck/rsc';
import { useEffect, useMemo } from 'react';
import { puckConfig } from '../puck/config';
import { loadPuckCss } from '../puck/load-css';

interface Page {
  id: number;
  title: string;
  slug: string;
  content: string;
}

interface Props {
  page: Page;
  isDraft?: boolean;
}

export default function Viewer({ page, isDraft }: Props) {
  useEffect(() => loadPuckCss(), []);
  const data = useMemo(() => {
    try {
      return JSON.parse(page.content);
    } catch {
      return { content: [], root: {} };
    }
  }, [page.content]);

  return (
    <div className="max-w-4xl mx-auto py-8">
      {isDraft && (
        <div className="mb-6 rounded-lg border border-warning/30 bg-warning-bg px-4 py-3 text-warning-text text-sm font-medium flex items-center gap-2">
          <svg
            width="16"
            height="16"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path d="M12 9v4m0 4h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
          </svg>
          Draft Preview — this version is not published
        </div>
      )}
      <Render config={puckConfig} data={data} />
    </div>
  );
}
```

**Step 2: Build the frontend**

Run: `cd modules/PageBuilder/src/PageBuilder && npx vite build`

**Step 3: Commit**
```bash
git add modules/PageBuilder/src/PageBuilder/Views/Viewer.tsx
git commit -m "feat(pagebuilder): show draft preview banner in Viewer"
```

---

### Task 6: Update Frontend — Manage page draft indicators

**Files:**
- Modify: `modules/PageBuilder/src/PageBuilder/Views/Manage.tsx`

**Step 1: Add hasDraft to interface and show badge + preview link**

Update the `PageSummary` interface to add `hasDraft`:
```tsx
interface PageSummary {
  id: number;
  title: string;
  slug: string;
  isPublished: boolean;
  hasDraft: boolean;
  order: number;
  createdAt: string;
  updatedAt: string;
}
```

Update the Status cell (around line 68-72) to show both badges:
```tsx
<TableCell>
  <div className="flex gap-1.5">
    <Badge variant={page.isPublished ? 'success' : 'secondary'}>
      {page.isPublished ? 'Published' : 'Unpublished'}
    </Badge>
    {page.hasDraft && (
      <Badge variant="warning">Draft</Badge>
    )}
  </div>
</TableCell>
```

Add a "Preview Draft" link in the actions column (after the Edit button, around line 82):
```tsx
{page.hasDraft && (
  <Button
    variant="ghost"
    size="sm"
    onClick={() => window.open(`/p/${page.slug}/draft`, '_blank')}
  >
    Preview Draft
  </Button>
)}
```

**Step 2: Build the frontend**

Run: `cd modules/PageBuilder/src/PageBuilder && npx vite build`

**Step 3: Commit**
```bash
git add modules/PageBuilder/src/PageBuilder/Views/Manage.tsx
git commit -m "feat(pagebuilder): show draft badge and preview link in Manage page"
```

---

### Task 7: Update integration test for UpdateContent and add EF migration

**Files:**
- Modify: `modules/PageBuilder/tests/PageBuilder.Tests/PageEndpointTests.cs`

**Step 1: Update existing UpdateContent integration test**

The `UpdateContent_WithPermission_SavesPuckJson` test (line 55-79) now saves to DraftContent. Update assertion:
```csharp
updated!.DraftContent.Should().Be("""{"content":[],"root":{}}""");
```

**Step 2: Run all tests**

Run: `dotnet test`

**Step 3: Generate EF migration**

Run from repo root:
```bash
dotnet ef migrations add AddPageDraftContent --project template/SimpleModule.Host --context HostDbContext
```

**Step 4: Verify migration was generated**

Check `template/SimpleModule.Host/Migrations/` for the new migration file. It should add a nullable `DraftContent` TEXT column to `PageBuilder_Pages`.

**Step 5: Commit**
```bash
git add modules/PageBuilder/tests/PageBuilder.Tests/PageEndpointTests.cs template/SimpleModule.Host/Migrations/
git commit -m "feat(pagebuilder): update integration tests and add DraftContent migration"
```

---

### Task 8: Final verification

**Step 1: Build entire solution**

Run: `dotnet build`

**Step 2: Run all tests**

Run: `dotnet test`

**Step 3: Run the app and manually verify**

Run: `dotnet run --project template/SimpleModule.Host`

Test flow:
1. Login as admin
2. Go to `/admin/pages` — existing pages should show no "Draft" badge
3. Edit a page → save in editor → go back to Manage → should see "Draft" badge
4. Click "Preview Draft" → opens `/p/{slug}/draft` with draft banner
5. Visit `/p/{slug}` → still shows old published content
6. Click "Publish" on the page → Draft badge disappears, published content updated
7. Visit `/p/{slug}` → shows new content

**Step 4: Final commit**
```bash
git add -A
git commit -m "feat(pagebuilder): complete draft support with preview"
```
