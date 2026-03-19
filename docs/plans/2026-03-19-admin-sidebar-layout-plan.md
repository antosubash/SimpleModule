# Admin Sidebar Layout Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the top navbar layout for admin pages with a dedicated sidebar layout, using React and the existing `@simplemodule/ui` Sidebar component. Menu items come from the `IMenuRegistry` system via a new `AdminSidebar` menu section, shared to React as Inertia props.

**Architecture:** Add `AdminSidebar` to `MenuSection` enum. Modules register admin sidebar items via `ConfigureMenu`. A new Inertia middleware injects admin sidebar menu items as shared props. An `AdminLayout.tsx` React component wraps all admin pages with a collapsible sidebar and simplified top bar.

**Tech Stack:** C# (MenuSection enum, Inertia middleware), React 19, Inertia.js, `@simplemodule/ui` Sidebar components, Tailwind CSS.

---

### Task 1: Add `AdminSidebar` to `MenuSection` enum

**Files:**
- Modify: `framework/SimpleModule.Core/Menu/MenuSection.cs`
- Modify: `tests/SimpleModule.Core.Tests/Menu/MenuRegistryTests.cs`
- Modify: `tests/SimpleModule.Core.Tests/Menu/MenuItemTests.cs`

**Step 1: Write the failing test**

Add a test in `MenuRegistryTests.cs` that uses `MenuSection.AdminSidebar`:

```csharp
[Fact]
public void GetItems_AdminSidebar_ReturnsCorrectItems()
{
    var items = new List<MenuItem>
    {
        new()
        {
            Label = "Users",
            Url = "/admin/users",
            Section = MenuSection.AdminSidebar,
        },
        new()
        {
            Label = "Nav",
            Url = "/nav",
            Section = MenuSection.Navbar,
        },
    };
    var registry = new MenuRegistry(items);

    var sidebarItems = registry.GetItems(MenuSection.AdminSidebar);

    sidebarItems.Should().ContainSingle().Which.Label.Should().Be("Users");
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "FullyQualifiedName~GetItems_AdminSidebar"`
Expected: FAIL — `MenuSection` does not contain `AdminSidebar`.

**Step 3: Add `AdminSidebar` to the enum**

In `framework/SimpleModule.Core/Menu/MenuSection.cs`:

```csharp
[EnumExtensions]
public enum MenuSection
{
    Navbar,
    UserDropdown,
    AdminSidebar,
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "FullyQualifiedName~GetItems_AdminSidebar"`
Expected: PASS

**Step 5: Commit**

```bash
git add framework/SimpleModule.Core/Menu/MenuSection.cs tests/SimpleModule.Core.Tests/Menu/MenuRegistryTests.cs
git commit -m "feat(core): add AdminSidebar to MenuSection enum"
```

---

### Task 2: Register admin sidebar menu items in modules

**Files:**
- Modify: `modules/Admin/src/Admin/AdminModule.cs`
- Modify: `modules/OpenIddict/src/OpenIddict/OpenIddictModule.cs`

**Step 1: Add AdminSidebar items to AdminModule.ConfigureMenu**

In `AdminModule.cs`, add two new `MenuItem` entries with `Section = MenuSection.AdminSidebar` for Users and Roles. Keep the existing Navbar and UserDropdown items — they serve the top nav and dropdown. Use the same icons and URLs as the existing Navbar items.

```csharp
menus.Add(
    new MenuItem
    {
        Label = "Users",
        Url = "/admin/users",
        Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/></svg>""",
        Order = 10,
        Section = MenuSection.AdminSidebar,
    }
);
menus.Add(
    new MenuItem
    {
        Label = "Roles",
        Url = "/admin/roles",
        Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>""",
        Order = 11,
        Section = MenuSection.AdminSidebar,
    }
);
```

**Step 2: Add AdminSidebar item to OpenIddictModule.ConfigureMenu**

In `OpenIddictModule.cs`, add one new `MenuItem`:

```csharp
menus.Add(
    new MenuItem
    {
        Label = "OAuth Clients",
        Url = "/openiddict/clients",
        Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"/></svg>""",
        Order = 20,
        Section = MenuSection.AdminSidebar,
    }
);
```

**Step 3: Build to verify**

Run: `dotnet build`
Expected: Success.

**Step 4: Commit**

```bash
git add modules/Admin/src/Admin/AdminModule.cs modules/OpenIddict/src/OpenIddict/OpenIddictModule.cs
git commit -m "feat(modules): register AdminSidebar menu items in Admin and OpenIddict"
```

---

### Task 3: Share admin sidebar menu items as Inertia props

The current Inertia integration has no shared props mechanism. We need to inject admin sidebar menu items into every Inertia response for admin pages. The cleanest approach: modify `InertiaResult` to support shared data, or create a middleware that enriches Inertia responses.

**Approach:** Add a scoped `InertiaSharedData` service. A middleware populates it per-request. `InertiaResult.ExecuteAsync` merges shared data into props.

**Files:**
- Modify: `framework/SimpleModule.Core/Inertia/InertiaResult.cs`
- Create: `framework/SimpleModule.Core/Inertia/InertiaSharedData.cs`
- Create: `framework/SimpleModule.Core/Inertia/AdminSidebarMiddleware.cs`
- Modify: `template/SimpleModule.Host/Program.cs`

**Step 1: Create `InertiaSharedData` — a per-request shared data store**

Create `framework/SimpleModule.Core/Inertia/InertiaSharedData.cs`:

```csharp
namespace SimpleModule.Core.Inertia;

public sealed class InertiaSharedData
{
    private readonly Dictionary<string, object?> _data = [];

    public void Set(string key, object? value) => _data[key] = value;

    public IReadOnlyDictionary<string, object?> GetAll() => _data;
}
```

**Step 2: Register `InertiaSharedData` as scoped**

In `Program.cs` (or a suitable service registration location), add:

```csharp
builder.Services.AddScoped<InertiaSharedData>();
```

**Step 3: Modify `InertiaResult.ExecuteAsync` to merge shared data**

In `InertiaResult.cs`, resolve `InertiaSharedData` from DI and merge its entries into the props dictionary before serializing:

```csharp
public async Task ExecuteAsync(HttpContext httpContext)
{
    var sharedData = httpContext.RequestServices.GetService<InertiaSharedData>();
    var mergedProps = MergeProps(_props, sharedData);

    var pageData = new
    {
        component = _component,
        props = mergedProps,
        url = httpContext.Request.Path + httpContext.Request.QueryString,
        version = InertiaMiddleware.Version,
    };

    // ... rest unchanged
}

private static object MergeProps(object? props, InertiaSharedData? sharedData)
{
    if (sharedData is null || sharedData.GetAll().Count == 0)
    {
        return props ?? new { };
    }

    var result = new Dictionary<string, object?>();

    // Add shared data first (lower priority)
    foreach (var kvp in sharedData.GetAll())
    {
        result[kvp.Key] = kvp.Value;
    }

    // Add endpoint props (higher priority — overwrites shared data)
    if (props is not null)
    {
        foreach (var property in props.GetType().GetProperties())
        {
            result[property.Name] = property.GetValue(props);
        }
    }

    return result;
}
```

Note: `GetProperties()` is fine here — this runs at request time on anonymous types, not in AOT-constrained module discovery. If AOT issues arise, the plan can use a source-generated approach instead.

**Step 4: Create `AdminSidebarMiddleware`**

Create `framework/SimpleModule.Core/Inertia/AdminSidebarMiddleware.cs`:

```csharp
using SimpleModule.Core.Menu;

namespace SimpleModule.Core.Inertia;

public static class AdminSidebarMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminSidebarSharedData(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var sharedData = context.RequestServices.GetService<InertiaSharedData>();
            if (sharedData is not null && context.User.Identity?.IsAuthenticated == true)
            {
                var menuRegistry = context.RequestServices.GetRequiredService<IMenuRegistry>();
                var items = menuRegistry.GetItems(MenuSection.AdminSidebar);
                sharedData.Set("adminSidebarMenu", items.Select(i => new
                {
                    label = i.Label,
                    url = i.Url,
                    icon = i.Icon,
                    order = i.Order,
                }).ToList());
            }
            await next();
        });
    }
}
```

**Step 5: Register middleware in `Program.cs`**

Add `app.UseAdminSidebarSharedData();` after authentication/authorization middleware, before `app.UseInertia()`.

**Step 6: Build to verify**

Run: `dotnet build`
Expected: Success.

**Step 7: Commit**

```bash
git add framework/SimpleModule.Core/Inertia/InertiaSharedData.cs framework/SimpleModule.Core/Inertia/AdminSidebarMiddleware.cs framework/SimpleModule.Core/Inertia/InertiaResult.cs template/SimpleModule.Host/Program.cs
git commit -m "feat(core): add Inertia shared data and admin sidebar middleware"
```

---

### Task 4: Create `AdminLayout.tsx` React component

**Files:**
- Create: `modules/Admin/src/Admin/Pages/AdminLayout.tsx`

**Step 1: Create the AdminLayout component**

This component renders:
1. A simplified top bar (logo, "Back to app" link, user info placeholder)
2. A collapsible sidebar using `@simplemodule/ui` Sidebar components
3. A content area where child pages render

The component reads `adminSidebarMenu` from Inertia shared props via `usePage()`.

Menu item icons come from the server-registered `MenuItem.Icon` field, which contains trusted SVG strings defined in module source code (e.g., `AdminModule.cs`). These are NOT user-supplied — they are developer-authored constants compiled into the application, making them safe to render directly.

```tsx
import { Link, usePage } from '@inertiajs/react';
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
  useSidebar,
} from '@simplemodule/ui';
import type { ReactNode } from 'react';

interface AdminMenuItem {
  label: string;
  url: string;
  icon: string;
  order: number;
}

interface AdminLayoutProps {
  children: ReactNode;
}

function TopBar() {
  return (
    <header className="sticky top-0 z-50 flex items-center justify-between border-b border-border bg-surface-overlay px-4 py-3" style={{ backdropFilter: 'blur(20px)' }}>
      <div className="flex items-center gap-4">
        <SidebarTrigger />
        <a href="/" className="flex items-center gap-2.5 no-underline font-bold text-text group" style={{ fontFamily: "'Sora', sans-serif" }}>
          <span className="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-bold shadow-md" style={{ background: 'linear-gradient(135deg, var(--color-primary), var(--color-accent))' }}>S</span>
          <span className="text-base">SimpleModule</span>
        </a>
      </div>
      <div className="flex items-center gap-3">
        <a href="/" className="text-sm text-text-muted no-underline hover:text-primary transition-colors">
          Back to app
        </a>
      </div>
    </header>
  );
}

function SidebarNav() {
  const { props } = usePage<{ adminSidebarMenu?: AdminMenuItem[] }>();
  const menuItems = props.adminSidebarMenu ?? [];
  const currentPath = window.location.pathname;
  const { open } = useSidebar();

  return (
    <>
      <SidebarHeader>
        {open && (
          <span className="text-xs font-semibold text-text-muted uppercase tracking-wider">
            Administration
          </span>
        )}
      </SidebarHeader>
      <SidebarContent>
        <SidebarMenu>
          {menuItems.map((item) => {
            const isActive = currentPath.startsWith(item.url);
            return (
              <SidebarMenuItem key={item.url}>
                <Link href={item.url} className="no-underline">
                  <SidebarMenuButton active={isActive}>
                    <span
                      className="flex-shrink-0 [&>svg]:w-5 [&>svg]:h-5"
                      dangerouslySetInnerHTML={{ __html: item.icon }}
                    />
                    {open && <span>{item.label}</span>}
                  </SidebarMenuButton>
                </Link>
              </SidebarMenuItem>
            );
          })}
        </SidebarMenu>
      </SidebarContent>
      <SidebarFooter>
        <SidebarTrigger />
      </SidebarFooter>
    </>
  );
}

export default function AdminLayout({ children }: AdminLayoutProps) {
  return (
    <SidebarProvider>
      <Sidebar>
        <SidebarNav />
      </Sidebar>
      <div className="flex-1 flex flex-col min-h-screen">
        <TopBar />
        <main className="flex-1 p-6">
          {children}
        </main>
      </div>
    </SidebarProvider>
  );
}
```

**Step 2: Build frontend to verify**

Run: `npm run check` from repo root.
Expected: No lint/format errors.

**Step 3: Commit**

```bash
git add modules/Admin/src/Admin/Pages/AdminLayout.tsx
git commit -m "feat(admin): add AdminLayout component with sidebar and top bar"
```

---

### Task 5: Wire admin pages to use AdminLayout via Inertia persistent layouts

**Files:**
- Modify: `modules/Admin/src/Admin/Pages/Admin/Users.tsx`
- Modify: `modules/Admin/src/Admin/Pages/Admin/UsersCreate.tsx`
- Modify: `modules/Admin/src/Admin/Pages/Admin/UsersEdit.tsx`
- Modify: `modules/Admin/src/Admin/Pages/Admin/Roles.tsx`
- Modify: `modules/Admin/src/Admin/Pages/Admin/RolesCreate.tsx`
- Modify: `modules/Admin/src/Admin/Pages/Admin/RolesEdit.tsx`

**Step 1: Add persistent layout to each admin page**

Inertia persistent layouts work by attaching a `layout` property to the page component. For each admin page component, add after the component definition:

```tsx
import AdminLayout from '../AdminLayout';

// ... existing component code ...

Users.layout = (page: React.ReactNode) => <AdminLayout>{page}</AdminLayout>;
```

Do this for all 6 admin page components. The pattern is identical — import `AdminLayout` and attach `.layout`.

**Step 2: Update `resolvePage` to support persistent layouts**

Check `packages/SimpleModule.Client/src/resolve-page.ts`. The current `resolvePage` returns `{ default: page }`. Inertia's `createInertiaApp` needs to see the `layout` property on the resolved component. Verify this works by checking if the returned component preserves its `.layout` property. If `page.default` exists, it should already have `.layout`. If wrapping in `{ default: page }`, the layout property is preserved on the inner `page` function — Inertia reads it from there.

No change should be needed to `resolve-page.ts`, but verify during testing.

**Step 3: Build and verify**

Run: `npm run check` from repo root.
Expected: No errors.

**Step 4: Commit**

```bash
git add modules/Admin/src/Admin/Pages/Admin/Users.tsx modules/Admin/src/Admin/Pages/Admin/UsersCreate.tsx modules/Admin/src/Admin/Pages/Admin/UsersEdit.tsx modules/Admin/src/Admin/Pages/Admin/Roles.tsx modules/Admin/src/Admin/Pages/Admin/RolesCreate.tsx modules/Admin/src/Admin/Pages/Admin/RolesEdit.tsx
git commit -m "feat(admin): wire all admin pages to use AdminLayout persistent layout"
```

---

### Task 6: Wire OpenIddict client pages to use AdminLayout

**Files:**
- Check: `modules/OpenIddict/src/OpenIddict/Pages/index.ts` for page list
- Modify: All OpenIddict admin page components (Clients list, create, edit)

**Step 1: Find OpenIddict page components**

Look at `modules/OpenIddict/src/OpenIddict/Pages/index.ts` to identify all page components.

**Step 2: Add persistent layout to each OpenIddict page**

Same pattern as Task 5:

```tsx
import AdminLayout from './AdminLayout'; // or create a shared AdminLayout, see note below
```

Note: OpenIddict pages need access to `AdminLayout`. Options:
- **Option A:** Duplicate `AdminLayout.tsx` into OpenIddict module (bad — DRY violation).
- **Option B:** Move `AdminLayout.tsx` to `@simplemodule/client` package since it depends on Inertia's `usePage()` (which is already a client concern). Export from `@simplemodule/client/admin-layout`.
- **Option C:** Create a small `AdminLayout.tsx` in each module that imports the sidebar components from `@simplemodule/ui`.

**Recommended: Option B** — Move AdminLayout to `@simplemodule/client`.

Steps:
1. Move `AdminLayout.tsx` from `modules/Admin/src/Admin/Pages/AdminLayout.tsx` to `packages/SimpleModule.Client/src/admin-layout.tsx`
2. Add `@simplemodule/ui` as a dependency in `packages/SimpleModule.Client/package.json`
3. Export from package: add `"./admin-layout"` to package.json exports
4. Update Admin module imports to `import { AdminLayout } from '@simplemodule/client/admin-layout'`
5. Import in OpenIddict pages: `import { AdminLayout } from '@simplemodule/client/admin-layout'`

**Step 3: Build and verify**

Run: `npm run check` from repo root.
Expected: No errors.

**Step 4: Commit**

```bash
git add packages/SimpleModule.Client/ modules/Admin/src/Admin/Pages/ modules/OpenIddict/src/OpenIddict/Pages/
git commit -m "refactor: move AdminLayout to @simplemodule/client, wire OpenIddict pages"
```

---

### Task 7: Remove admin items from top navbar

Now that admin pages have their own sidebar, remove the Admin and OpenIddict items from `MenuSection.Navbar` to avoid duplication.

**Files:**
- Modify: `modules/Admin/src/Admin/AdminModule.cs`
- Modify: `modules/OpenIddict/src/OpenIddict/OpenIddictModule.cs`

**Step 1: Remove Navbar menu items for admin pages**

In `AdminModule.cs`, remove the two `MenuItem` entries with `Section = MenuSection.Navbar` (Users and Roles, order 20-21). Keep the `UserDropdown` items (Manage Users, Manage Roles) — those serve as quick-access links from the user menu.

In `OpenIddictModule.cs`, remove the `MenuItem` with `Section = MenuSection.Navbar` (OAuth Clients, order 22).

**Step 2: Add a single "Admin" navbar link**

In `AdminModule.cs`, add one Navbar item that links to the admin area:

```csharp
menus.Add(
    new MenuItem
    {
        Label = "Admin",
        Url = "/admin/users",
        Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/><path d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/></svg>""",
        Order = 20,
        Section = MenuSection.Navbar,
    }
);
```

**Step 3: Build and verify**

Run: `dotnet build`
Expected: Success.

**Step 4: Commit**

```bash
git add modules/Admin/src/Admin/AdminModule.cs modules/OpenIddict/src/OpenIddict/OpenIddictModule.cs
git commit -m "refactor: consolidate navbar to single Admin link, move items to sidebar"
```

---

### Task 8: Remove hardcoded admin links from ManageLayout

The `ManageLayout.tsx` in the Users module has hardcoded admin links (Manage Users, Manage Roles). These should be removed now that admin has its own layout.

**Files:**
- Modify: `modules/Users/src/Users/Pages/Account/ManageLayout.tsx`

**Step 1: Remove the `adminItems` array and its rendering**

Remove the `adminItems` constant and any JSX that renders admin links in the manage layout. Keep the account management nav items.

**Step 2: Build and verify**

Run: `npm run check`
Expected: No errors.

**Step 3: Commit**

```bash
git add modules/Users/src/Users/Pages/Account/ManageLayout.tsx
git commit -m "refactor(users): remove hardcoded admin links from ManageLayout"
```

---

### Task 9: Style the admin layout for full viewport

The Blazor `MainLayout.razor` renders a `<main>` tag with `max-w-6xl` and centered padding. When admin pages render with their own sidebar, this constrains the layout.

**Files:**
- Modify: `modules/Admin/src/Admin/Pages/AdminLayout.tsx` (or `packages/SimpleModule.Client/src/admin-layout.tsx` after Task 6 move)

**Step 1: Use fixed positioning for full-viewport admin layout**

Have `AdminLayout.tsx` render with styles that overlay the Blazor shell:

```tsx
<div className="fixed inset-0 z-40 flex bg-surface">
  {/* sidebar + content */}
</div>
```

This overlays the Blazor shell entirely for admin pages. The Blazor nav is still rendered server-side but hidden behind the React overlay. This avoids any Blazor changes.

**Step 2: Verify visually**

Run: `dotnet run --project template/SimpleModule.Host`
Navigate to `/admin/users` and verify the sidebar layout renders correctly.

**Step 3: Commit**

```bash
git add packages/SimpleModule.Client/src/admin-layout.tsx
git commit -m "feat(admin): use fixed positioning for full-viewport admin layout"
```

---

### Task 10: End-to-end verification and cleanup

**Step 1: Build everything**

Run: `dotnet build && npm run check`
Expected: Both pass.

**Step 2: Run all tests**

Run: `dotnet test`
Expected: All pass. The menu system tests should pass with the new `AdminSidebar` section.

**Step 3: Manual verification**

Run: `dotnet run --project template/SimpleModule.Host`

Verify:
- `/admin/users` — shows sidebar layout with Users, Roles, OAuth Clients links
- Sidebar collapses to icon-only on toggle
- "Back to app" link returns to home
- Active link is highlighted
- `/admin/roles`, `/openiddict/clients` — also show sidebar layout
- `/` (home), `/products` — still show normal top navbar layout
- Mobile: sidebar is hidden, hamburger menu works

**Step 4: Commit any cleanup**

```bash
git commit -m "chore: admin sidebar layout cleanup"
```
