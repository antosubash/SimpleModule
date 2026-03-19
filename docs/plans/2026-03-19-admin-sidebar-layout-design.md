# Admin Sidebar Layout Design

## Overview

Admin pages (`/admin/*`) get a dedicated sidebar layout rendered in React. The rest of the app keeps the current top navbar layout unchanged.

## Layout Structure

```
┌─────────────────────────────────────────────┐
│  Logo        [Back to app]            [User]│
├────────┬────────────────────────────────────┤
│        │                                    │
│ Sidebar│         Content Area               │
│        │                                    │
│ Users  │    (existing admin pages render    │
│ Roles  │     here unchanged)                │
│ Clients│                                    │
│        │                                    │
│        │                                    │
│[toggle]│                                    │
└────────┴────────────────────────────────────┘
```

## Menu System Integration

- Add `AdminSidebar` to `MenuSection` enum.
- Modules register admin sidebar items in `ConfigureMenu` (e.g., Admin module registers Users, Roles; OpenIddict module registers Clients).
- Server shares admin sidebar menu items as Inertia shared props via middleware.
- `AdminLayout.tsx` reads these props and renders the sidebar dynamically.

## Sidebar

- Uses existing `Sidebar` component from `@simplemodule/ui`.
- Collapsible: `w-64` expanded (icons + labels), `w-16` collapsed (icons only).
- Toggle button at the bottom.
- Active state highlighting based on current URL.
- On mobile: hidden by default, toggled via hamburger.

## Simplified Top Bar

- Shown only on admin pages.
- Contains: logo (links to app home), "Back to app" link, user dropdown.
- Blazor `MainLayout.razor` still renders server-side; React `AdminLayout` handles its own chrome within the Inertia content area.

## Implementation Approach

- **`MenuSection.AdminSidebar`** — New enum value for admin sidebar items.
- **Module registration** — Each module adds items to `AdminSidebar` section in `ConfigureMenu`.
- **Inertia shared props** — Middleware resolves `IMenuRegistry` and shares admin sidebar items as props for authenticated admin users.
- **`AdminLayout.tsx`** — React wrapper rendering sidebar + top bar + content slot.
- **Admin pages** — Use Inertia persistent layouts to wrap with `AdminLayout`.
- **No Blazor layout changes** — Blazor SSR shell unchanged.

## Styling

- Follows existing theme (teal primary, glass-card effects, dark mode support).
- Sidebar background uses surface colors from the theme.
- Smooth transition animation on collapse/expand.
