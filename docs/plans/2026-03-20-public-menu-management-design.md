# Public Menu Management — Design

**Date:** 2026-03-20
**Module:** Settings
**Status:** Approved

## Summary

Add a menu management feature to the Settings module that lets admins build multi-level public navigation menus, assign existing module pages or custom URLs to items, and designate a home page.

## Requirements

- Admin-only menu management UI in Settings
- Multi-level menus (max 3 levels deep)
- Link to existing module pages (auto-discovered) or custom URLs
- Per-item properties: label, icon, CSS class, open-in-new-tab, visibility toggle
- Home page designation (one item renders at `/`)
- Replaces hardcoded `Navbar` menu items for public layout
- Graceful fallback to hardcoded menus when DB is empty

## Approach

**Approach A (selected):** Menu entities in Settings module with a new `IPublicMenuProvider` Core abstraction.

Alternatives considered:
- **B: Dedicated Menu module** — overkill for a settings concern
- **C: JSON blob in existing Settings key-value store** — no relational integrity, poor concurrency

## Data Model

New entity `PublicMenuItemEntity` in Settings module:

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` | PK |
| `ParentId` | `int?` | Self-referential FK, null = top-level |
| `Label` | `string` | Required |
| `Url` | `string?` | Custom/external URL |
| `PageRoute` | `string?` | Module page route (e.g., `Products/Browse`) |
| `Icon` | `string` | SVG string, default empty |
| `CssClass` | `string?` | Optional CSS class |
| `OpenInNewTab` | `bool` | Default false |
| `IsVisible` | `bool` | Show/hide toggle, default true |
| `IsHomePage` | `bool` | Exactly one item max |
| `SortOrder` | `int` | Ordering within parent |
| `CreatedAt` | `DateTimeOffset` | |
| `UpdatedAt` | `DateTimeOffset` | |

Constraints:
- Max nesting depth: 3 (enforced at service level)
- `Url` and `PageRoute` are mutually exclusive (one must be set)
- `IsHomePage` — setting on one item clears all others

## Core Abstraction

New interface in `SimpleModule.Core`:

```csharp
public interface IPublicMenuProvider
{
    Task<IReadOnlyList<PublicMenuItem>> GetMenuTreeAsync();
    Task<string?> GetHomePageUrlAsync();
}
```

New DTO `PublicMenuItem` in Core:

```csharp
public sealed class PublicMenuItem
{
    public required string Label { get; init; }
    public required string Url { get; init; }
    public string Icon { get; init; } = "";
    public string? CssClass { get; init; }
    public bool OpenInNewTab { get; init; }
    public bool IsHomePage { get; init; }
    public IReadOnlyList<PublicMenuItem> Children { get; init; } = [];
}
```

Settings module implements `IPublicMenuProvider`, registered as scoped service.

## Available Pages Discovery

New source generator emitter produces `PageRegistry` — a static list of all `IViewEndpoint` pages with their resolved URLs and module names:

```csharp
public static class PageRegistry
{
    public static IReadOnlyList<AvailablePage> Pages { get; } = [ ... ];
}
public sealed record AvailablePage(string PageRoute, string Url, string Module);
```

Exposed via `GET /api/settings/menus/available-pages` for the page picker dropdown.

## API Endpoints

All admin-only, under `/api/settings/menus`:

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/settings/menus` | Get full menu tree |
| `POST` | `/api/settings/menus` | Create menu item |
| `PUT` | `/api/settings/menus/{id}` | Update menu item |
| `DELETE` | `/api/settings/menus/{id}` | Delete item + cascade children |
| `PUT` | `/api/settings/menus/reorder` | Batch reorder `[{id, parentId, sortOrder}]` |
| `PUT` | `/api/settings/menus/{id}/home` | Set as home page |
| `DELETE` | `/api/settings/menus/home` | Clear home page |
| `GET` | `/api/settings/menus/available-pages` | List discoverable pages |

View endpoint: `GET /settings/menus` renders `Settings/MenuManager` page.

Validation:
- Max depth 3 (reject if parent chain exceeds limit)
- `url` or `pageRoute` required (mutually exclusive)
- `pageRoute` must exist in `PageRegistry.Pages`
- `label` required, non-empty

## Frontend — Menu Manager Page

React page `Settings/MenuManager`:

**Left panel — Tree view:**
- Drag-and-drop sortable tree (3 levels max)
- Each node shows label, icon preview, page/URL indicator, visibility eye toggle
- Click to select for editing

**Right panel — Item editor:**
- Label, link type toggle (Page dropdown / Custom URL), icon, CSS class
- Open in new tab, visible, home page checkboxes
- Delete with confirmation for items with children

**Top toolbar:** "Add item" (top-level), "Add child" (when selected, disabled at level 3)

**Persistence:** Optimistic updates on blur/toggle. Drag-drop calls batch reorder endpoint.

## Blazor Layout Integration

**PublicLayout.razor:** Inject `IPublicMenuProvider`. If dynamic menu has items, render multi-level nav (dropdowns for children, flyouts for 3rd level). If empty, fall back to `MenuRegistry.GetItems(MenuSection.Navbar)`.

**Home page middleware:** Registered early in pipeline. On `GET /`, checks `IPublicMenuProvider.GetHomePageUrlAsync()`. If configured (and not already `/`), rewrites `context.Request.Path` internally. URL stays `/` in browser.

**Caching:** In-memory cache with short TTL or invalidation on admin save. Avoids DB hit per public request.

## Testing Strategy

**Unit tests:**
- Depth validation, tree building, home page toggle logic, input validation

**Integration tests:**
- Full CRUD cycle, reorder, cascade delete, home page middleware, available pages endpoint, fallback behavior

**Playwright UI tests:**
- Menu manager CRUD, drag-and-drop, visibility toggle, home page, public navbar reflects changes
