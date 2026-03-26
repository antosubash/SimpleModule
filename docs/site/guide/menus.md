---
outline: deep
---

# Menus

SimpleModule provides a menu system that allows each module to contribute navigation items. Menus are configured in module classes and organized into sections for different parts of the UI.

## Overview

The menu system has two layers:

1. **Module menus** -- static navigation items defined by modules at startup via `IMenuBuilder`
2. **Public menus** -- dynamic, database-backed menu trees managed through the Settings module via `IPublicMenuProvider`

## Module Menus

### IMenuBuilder

Each module contributes menu items by implementing `ConfigureMenu` on its module class:

```csharp
public interface IMenuBuilder
{
    IMenuBuilder Add(MenuItem item);
}
```

The builder collects items from all modules, then sorts them by `Order` to produce the final list.

### Configuring Menu Items

Override `ConfigureMenu` in your module class:

```csharp
[Module("Products", RoutePrefix = "/products", ViewPrefix = "/products")]
public class ProductsModule : IModule
{
    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(new MenuItem
        {
            Label = "Products",
            Url = "/products/browse",
            Icon = """<svg class="w-4 h-4" ...>...</svg>""",
            Order = 30,
            Section = MenuSection.Navbar,
            RequiresAuth = false,
        });

        menus.Add(new MenuItem
        {
            Label = "Manage Products",
            Url = "/products/manage",
            Icon = """<svg class="w-4 h-4" ...>...</svg>""",
            Order = 31,
            Section = MenuSection.Navbar,
        });
    }
}
```

### MenuItem Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Label` | `string` | *required* | Display text for the menu item |
| `Url` | `string` | *required* | Navigation URL |
| `Icon` | `string` | `""` | SVG icon markup |
| `Order` | `int` | `0` | Sort order (lower values appear first) |
| `Section` | `MenuSection` | `Navbar` | Which UI section this item belongs to |
| `RequiresAuth` | `bool` | `true` | Whether the user must be authenticated |
| `Group` | `string?` | `null` | Optional group label for visual grouping |

### Menu Sections

The `MenuSection` enum determines where a menu item is rendered:

```csharp
public enum MenuSection
{
    Navbar,        // Top navigation bar
    UserDropdown,  // User profile dropdown
    AdminSidebar,  // Admin panel sidebar
    AppSidebar,    // Main application sidebar
}
```

Items can appear in multiple sections by adding separate `MenuItem` entries:

```csharp
public void ConfigureMenu(IMenuBuilder menus)
{
    // Show in the top navbar
    menus.Add(new MenuItem
    {
        Label = "Products",
        Url = "/products/browse",
        Order = 30,
        Section = MenuSection.Navbar,
        RequiresAuth = false,
    });

    // Also show in the app sidebar
    menus.Add(new MenuItem
    {
        Label = "Products",
        Url = "/products/browse",
        Order = 20,
        Section = MenuSection.AppSidebar,
    });
}
```

### Grouping

Use the `Group` property to visually group related items in sidebar sections:

```csharp
menus.Add(new MenuItem
{
    Label = "Dashboard",
    Url = "/audit-logs/dashboard",
    Order = 94,
    Section = MenuSection.AdminSidebar,
    Group = "Audit Logs",
});

menus.Add(new MenuItem
{
    Label = "Browse Logs",
    Url = "/audit-logs/browse",
    Order = 95,
    Section = MenuSection.AdminSidebar,
    Group = "Audit Logs",
});
```

Items with the same `Group` value are rendered together under a group header.

### Ordering

Menu items are sorted globally by `Order` within each section. Use consistent ranges per module to keep items together:

| Range | Module |
|-------|--------|
| 10-19 | Dashboard |
| 20-29 | Core features |
| 30-39 | Products |
| 40-49 | Orders |
| 90-99 | Settings / Admin |

::: tip
Leave gaps between order values so new items can be inserted without renumbering everything.
:::

## IMenuRegistry

At runtime, the `IMenuRegistry` provides read-only access to the collected menu items:

```csharp
public interface IMenuRegistry
{
    IReadOnlyList<MenuItem> GetItems(MenuSection section);
}
```

The `MenuRegistry` groups items by section and returns them sorted by `Order`:

```csharp
app.MapGet("/api/menu", (IMenuRegistry registry) =>
{
    return new
    {
        navbar = registry.GetItems(MenuSection.Navbar),
        sidebar = registry.GetItems(MenuSection.AppSidebar),
    };
});
```

## Public Menus

The Settings module provides a database-backed public menu system through `IPublicMenuProvider`:

```csharp
public interface IPublicMenuProvider
{
    Task<IReadOnlyList<PublicMenuItem>> GetMenuTreeAsync();
    Task<string?> GetHomePageUrlAsync();
}
```

### PublicMenuItem

Public menu items support hierarchical nesting:

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

Public menus are managed through the Settings module's admin UI, which supports:

- Creating, updating, and deleting menu items
- Hierarchical nesting (up to 3 levels deep)
- Drag-and-drop reordering
- Designating a home page
- Linking to internal page routes or external URLs

### Home Page Resolution

The framework uses `IPublicMenuProvider.GetHomePageUrlAsync()` to redirect the root URL (`/`) to the configured home page. If a menu item is marked as `IsHomePage`, requests to `/` are internally rewritten to that URL.

## Frontend Rendering

Menu data is typically shared with the frontend via Inertia shared data or API endpoints. The Blazor layout components (`ModuleNav`, `UserDropdown`, sidebar components) read from `IMenuRegistry` to render navigation:

- **`ModuleNav`** renders `MenuSection.Navbar` items
- **Sidebar components** render `MenuSection.AppSidebar` and `MenuSection.AdminSidebar` items
- **`UserDropdown`** renders `MenuSection.UserDropdown` items

The `RequiresAuth` property controls visibility -- items with `RequiresAuth = true` are only shown to authenticated users.
