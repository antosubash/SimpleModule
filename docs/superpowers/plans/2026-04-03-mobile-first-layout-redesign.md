# Mobile-First Layout Redesign — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make all three Blazor SSR layouts (AppLayout, PublicLayout, ManageLayout) fully functional on mobile with proper navigation, accessibility, and touch support.

**Architecture:** All changes are in Blazor `.razor` files and vanilla JS/CSS — no React changes. Mobile behavior is driven by CSS class toggling from JS, matching the existing codebase pattern. The `md:` breakpoint (768px) is the universal mobile/desktop threshold.

**Tech Stack:** Blazor SSR (Razor components), vanilla JavaScript, Tailwind CSS v4, inline SVG icons.

**Spec:** `docs/superpowers/specs/2026-04-03-mobile-first-layout-redesign.md`

---

## File Structure

| File | Responsibility |
| ---- | -------------- |
| `framework/SimpleModule.Blazor/Components/Layout/AppLayout.razor` | Add mobile header + backdrop HTML elements, ARIA attributes |
| `framework/SimpleModule.Blazor/Components/Layout/PublicLayout.razor` | Add hamburger + full-screen overlay with accordion nav, ARIA |
| `framework/SimpleModule.Blazor/Components/Layout/ManageLayout.razor` | Switch to `md:` breakpoint, add mobile tab bar markup |
| `framework/SimpleModule.Blazor/Components/ManageNav.razor` | Add `Horizontal` parameter, responsive class variants |
| `template/SimpleModule.Host/wwwroot/js/shell.js` | Mobile sidebar toggle, swipe gestures, scroll lock, escape key, matchMedia cleanup |
| `packages/SimpleModule.Theme.Default/theme.css` | Hide collapse toggle on mobile, scrollbar-hide, tab underline styles, prefers-reduced-motion |

---

## Chunk 1: Theme CSS + shell.js Foundation

### Task 1: Add mobile CSS utilities to theme.css

**Files:**
- Modify: `packages/SimpleModule.Theme.Default/theme.css:582-614`

- [ ] **Step 1: Hide sidebar collapse toggle on mobile**

In the `.app-sidebar-toggle` rule (line 583), remove `flex` from the `@apply` directive and add `display: none` to hide on mobile. Then show on desktop with `display: flex`:

```css
.app-sidebar-toggle {
    @apply fixed bottom-4 z-50 w-7 h-7 rounded-full bg-surface border border-border items-center justify-center text-text-muted hover:text-text hover:bg-surface-raised cursor-pointer transition-all duration-200;
    left: 1rem;
    display: none;
}
```

Note: `flex` was removed from `@apply` because `display: none` needs to win on mobile. On desktop, `display: flex` is set explicitly.

In the `@media (min-width: 768px)` block, add:
```css
.app-sidebar-toggle {
    display: flex;
}
```

- [ ] **Step 2: Add scrollbar-hide utility**

After the existing `.table-responsive` block (around line 339), add inside `@layer components`:

```css
/* --- Scrollbar hide (for horizontal tab bars) --- */
.scrollbar-hide {
    -ms-overflow-style: none;
    scrollbar-width: none;
}
.scrollbar-hide::-webkit-scrollbar {
    display: none;
}
```

- [ ] **Step 3: Add manage tab styles**

After the scrollbar-hide block, add inside `@layer components`:

```css
/* --- Manage account tab bar --- */
.manage-tab {
    @apply inline-flex items-center gap-2 px-4 py-2.5 text-sm whitespace-nowrap text-text-muted no-underline transition-colors duration-150 border-b-2 border-transparent shrink-0;
}
.manage-tab:hover {
    @apply text-text;
}
.manage-tab-active {
    @apply inline-flex items-center gap-2 px-4 py-2.5 text-sm whitespace-nowrap text-primary no-underline font-semibold border-b-2 border-primary shrink-0;
}
```

- [ ] **Step 4: Add prefers-reduced-motion rule**

At the end of the file, before the closing, add:

```css
/* --- Reduced motion --- */
@media (prefers-reduced-motion: reduce) {
    .app-sidebar,
    .app-sidebar-backdrop,
    .app-sidebar-toggle,
    .public-overlay {
        transition: none !important;
    }
}
```

- [ ] **Step 5: Add public overlay styles**

Inside the `@media (min-width: 768px)` block within the App Layout section, after `.app-mobile-header { display: none; }`:

```css
.public-overlay {
    display: none;
}
```

And in `@layer components` (outside the media query), add:

```css
/* --- Public layout mobile overlay --- */
.public-overlay {
    @apply fixed inset-0 z-50 bg-surface;
    opacity: 0;
    pointer-events: none;
    transition: opacity 0.25s ease;
}
.public-overlay.open {
    opacity: 1;
    pointer-events: auto;
}
```

- [ ] **Step 6: Verify CSS builds**

Run: `npm run build`
Expected: Build succeeds with no errors.

- [ ] **Step 7: Commit**

```bash
git add packages/SimpleModule.Theme.Default/theme.css
git commit -m "feat: add mobile CSS utilities for layout redesign"
```

---

### Task 2: Add mobile sidebar logic to shell.js

**Files:**
- Modify: `template/SimpleModule.Host/wwwroot/js/shell.js`

- [ ] **Step 1: Add mobile sidebar toggle function**

Append to `shell.js` after the existing user dropdown code:

```javascript
// ── Mobile sidebar ──────────────────────────────────────
(function () {
  var sidebar = document.getElementById('app-sidebar');
  var backdrop = document.getElementById('app-sidebar-backdrop');
  var hamburger = document.getElementById('app-mobile-hamburger');
  if (!sidebar) return;

  function openSidebar() {
    sidebar.classList.add('app-sidebar-open');
    if (backdrop) backdrop.classList.add('visible');
    if (hamburger) {
      hamburger.setAttribute('aria-expanded', 'true');
      hamburger.setAttribute('aria-label', 'Close navigation');
    }
    document.body.style.overflow = 'hidden';
  }

  function closeSidebar() {
    sidebar.classList.remove('app-sidebar-open');
    if (backdrop) backdrop.classList.remove('visible');
    if (hamburger) {
      hamburger.setAttribute('aria-expanded', 'false');
      hamburger.setAttribute('aria-label', 'Open navigation');
    }
    document.body.style.overflow = '';
  }

  function isMobile() {
    return !window.matchMedia('(min-width: 768px)').matches;
  }

  // Public API
  window.openMobileSidebar = openSidebar;
  window.closeMobileSidebar = closeSidebar;

  // Backdrop click
  if (backdrop) {
    backdrop.addEventListener('click', closeSidebar);
  }

  // Escape key
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && isMobile() && sidebar.classList.contains('app-sidebar-open')) {
      closeSidebar();
    }
  });

  // Close on desktop resize
  var mql = window.matchMedia('(min-width: 768px)');
  mql.addEventListener('change', function (e) {
    if (e.matches) {
      closeSidebar();
    }
  });

  // Swipe gesture
  var touchStartX = 0;
  var touchStartY = 0;
  var swiping = false;

  document.addEventListener('touchstart', function (e) {
    var touch = e.touches[0];
    touchStartX = touch.clientX;
    touchStartY = touch.clientY;
    swiping = false;
    // Only detect edge swipe when sidebar is closed
    if (!sidebar.classList.contains('app-sidebar-open') && touchStartX <= 25) {
      swiping = true;
    }
    // Detect swipe-to-close on sidebar or backdrop
    if (sidebar.classList.contains('app-sidebar-open')) {
      swiping = true;
    }
  }, { passive: true });

  document.addEventListener('touchend', function (e) {
    if (!swiping || !isMobile()) return;
    var touch = e.changedTouches[0];
    var dx = touch.clientX - touchStartX;
    var dy = Math.abs(touch.clientY - touchStartY);
    // Ignore vertical scrolls
    if (dy > Math.abs(dx)) return;
    var isOpen = sidebar.classList.contains('app-sidebar-open');
    if (!isOpen && dx > 50 && touchStartX <= 25) {
      openSidebar();
    } else if (isOpen && dx < -50) {
      closeSidebar();
    }
  }, { passive: true });
})();
```

- [ ] **Step 2: Verify JS is syntactically valid**

Run: `node -c template/SimpleModule.Host/wwwroot/js/shell.js`
Expected: No syntax errors.

- [ ] **Step 3: Commit**

```bash
git add template/SimpleModule.Host/wwwroot/js/shell.js
git commit -m "feat: add mobile sidebar toggle, swipe gestures, and accessibility handlers"
```

---

## Chunk 2: AppLayout Mobile Header + Backdrop

### Task 3: Add mobile header and backdrop to AppLayout.razor

**Files:**
- Modify: `framework/SimpleModule.Blazor/Components/Layout/AppLayout.razor`

- [ ] **Step 1: Add mobile header before the sidebar**

Insert before `<aside class="app-sidebar" ...>` (line 6):

```html
<!-- Mobile header -->
<div class="app-mobile-header">
    <button id="app-mobile-hamburger" onclick="openMobileSidebar()" aria-expanded="false" aria-label="Open navigation" class="p-1 -ml-1 text-text-muted hover:text-text">
        <svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16"/></svg>
    </button>
    <a href="/" class="flex items-center gap-2 no-underline font-bold text-text" style="font-family:'Sora',sans-serif;">
        <span class="w-7 h-7 rounded-lg flex items-center justify-center text-white text-xs font-bold" style="background:linear-gradient(135deg,var(--color-primary),var(--color-accent));">S</span>
        <span class="text-sm">SimpleModule</span>
    </a>
</div>
```

- [ ] **Step 2: Add backdrop element after the closing `</aside>` tag**

Insert right after `</aside>` (line 80) and before the sidebar collapse toggle button:

```html
<!-- Mobile backdrop -->
<div class="app-sidebar-backdrop" id="app-sidebar-backdrop" aria-hidden="true"></div>
```

- [ ] **Step 3: Update sidebar collapse toggle to be hidden on mobile**

Change the existing toggle button (line 83):
```html
<button class="app-sidebar-toggle" id="app-sidebar-toggle" onclick="toggleSidebar()" title="Toggle sidebar">
```
This is already handled by the CSS change in Task 1 Step 1 (`.app-sidebar-toggle { display: none }` on mobile). No HTML change needed.

- [ ] **Step 4: Verify the build compiles**

Run: `dotnet build framework/SimpleModule.Blazor/SimpleModule.Blazor.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Blazor/Components/Layout/AppLayout.razor
git commit -m "feat: add mobile header and backdrop to AppLayout"
```

---

## Chunk 3: PublicLayout Full-Screen Overlay

### Task 4: Add hamburger and overlay to PublicLayout.razor

**Files:**
- Modify: `framework/SimpleModule.Blazor/Components/Layout/PublicLayout.razor`

- [ ] **Step 0: Update existing breakpoints from `sm:` to `md:`**

For consistent 768px threshold across all layouts, update these existing classes in PublicLayout.razor:

- Line 14: Change `hidden sm:flex` to `hidden md:flex` (desktop nav container)
- Line 83: Change `hidden sm:inline-flex` to `hidden md:inline-flex` (Sign up button)

This ensures the desktop nav and Sign up button hide/show at the same `md:` breakpoint as the hamburger button.

- [ ] **Step 1: Add hamburger button in the navbar**

In the `<div class="ml-auto flex items-center gap-3">` (line 80), before the DarkModeToggle:

```html
<button class="md:hidden p-1 text-text-muted hover:text-text" id="public-menu-toggle" onclick="togglePublicOverlay()" aria-expanded="false" aria-label="Open menu">
    <svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16"/></svg>
</button>
```

- [ ] **Step 2: Add mobile overlay after the `</nav>` tag**

Insert after the closing `</nav>` and before `<main>`:

```html
<!-- Mobile overlay menu -->
<div class="public-overlay" id="public-overlay" role="dialog" aria-modal="true" aria-hidden="true">
    <div class="flex flex-col h-full">
        <!-- Overlay header -->
        <div class="flex items-center justify-between px-6 py-3 border-b border-border">
            <a href="/" class="flex items-center gap-2.5 no-underline font-bold text-text" style="font-family:'Sora',sans-serif;">
                <span class="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-bold" style="background:linear-gradient(135deg,var(--color-primary),var(--color-accent));">S</span>
                <span class="text-base">SimpleModule</span>
            </a>
            <button onclick="togglePublicOverlay()" class="p-2 text-text-muted hover:text-text" aria-label="Close menu">
                <svg class="w-6 h-6" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>
        </div>

        <!-- Overlay nav items -->
        <nav class="flex-1 overflow-y-auto px-6 py-6 space-y-1">
            @if (_dynamicMenu is { Count: > 0 })
            {
                @foreach (var item in _dynamicMenu)
                {
                    @if (item.Children.Count == 0)
                    {
                        <a href="@item.Url"
                           class="block py-3 text-lg text-text no-underline border-b border-border/50 @item.CssClass"
                           target="@(item.OpenInNewTab ? "_blank" : null)"
                           onclick="togglePublicOverlay()">
                            @item.Label
                        </a>
                    }
                    else
                    {
                        <div class="border-b border-border/50">
                            <button class="flex items-center justify-between w-full py-3 text-lg text-text bg-transparent border-none cursor-pointer"
                                    onclick="this.nextElementSibling.classList.toggle('hidden');this.querySelector('svg').classList.toggle('rotate-180')">
                                @item.Label
                                <svg class="w-5 h-5 transition-transform duration-200" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 9l-7 7-7-7"/></svg>
                            </button>
                            <div class="hidden pl-4 pb-2 space-y-1">
                                @foreach (var child in item.Children)
                                {
                                    <a href="@child.Url"
                                       class="block py-2.5 text-base text-text-secondary no-underline"
                                       target="@(child.OpenInNewTab ? "_blank" : null)"
                                       onclick="togglePublicOverlay()">
                                        @child.Label
                                    </a>
                                }
                            </div>
                        </div>
                    }
                }
            }
            else
            {
                <a href="/" class="block py-3 text-lg text-text no-underline border-b border-border/50" onclick="togglePublicOverlay()">Home</a>
                @* Mirror the ModuleNav fallback from the desktop nav *@
                <a href="/marketplace" class="block py-3 text-lg text-text no-underline border-b border-border/50" onclick="togglePublicOverlay()">Marketplace</a>
                <a href="/swagger" class="block py-3 text-lg text-text no-underline border-b border-border/50" onclick="togglePublicOverlay()">API Docs</a>
            }
        </nav>

        <!-- Overlay footer -->
        <div class="px-6 py-6 border-t border-border space-y-3">
            <a href="/Identity/Account/Login" class="btn-ghost w-full text-center no-underline block py-3">Log in</a>
            <a href="/Identity/Account/Register" class="btn-primary w-full text-center no-underline block py-3">Sign up</a>
        </div>
    </div>
</div>
```

- [ ] **Step 3: Add inline JS for overlay toggle**

Add after the existing `@code` block's closing brace, or better, add a `<script>` block at the end of the file (after `@code { ... }`):

```html
<script suppress-error="BL9992">
(function() {
    var overlay = document.getElementById('public-overlay');
    var toggle = document.getElementById('public-menu-toggle');
    if (!overlay) return;

    window.togglePublicOverlay = function() {
        var isOpen = overlay.classList.contains('open');
        if (isOpen) {
            overlay.classList.remove('open');
            overlay.setAttribute('aria-hidden', 'true');
            if (toggle) toggle.setAttribute('aria-expanded', 'false');
            document.body.style.overflow = '';
        } else {
            overlay.classList.add('open');
            overlay.setAttribute('aria-hidden', 'false');
            if (toggle) toggle.setAttribute('aria-expanded', 'true');
            document.body.style.overflow = 'hidden';
            // Focus trap
            var focusable = overlay.querySelectorAll('a[href], button, input');
            if (focusable.length) focusable[0].focus();
        }
    };

    // Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && overlay.classList.contains('open')) {
            window.togglePublicOverlay();
        }
    });

    // Focus trap
    overlay.addEventListener('keydown', function(e) {
        if (e.key !== 'Tab') return;
        var focusable = overlay.querySelectorAll('a[href], button:not([disabled])');
        if (!focusable.length) return;
        var first = focusable[0];
        var last = focusable[focusable.length - 1];
        if (e.shiftKey && document.activeElement === first) {
            e.preventDefault();
            last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
            e.preventDefault();
            first.focus();
        }
    });

    // Close on resize to desktop
    var mql = window.matchMedia('(min-width: 768px)');
    mql.addEventListener('change', function(e) {
        if (e.matches && overlay.classList.contains('open')) {
            window.togglePublicOverlay();
        }
    });
})();
</script>
```

- [ ] **Step 4: Verify the build compiles**

Run: `dotnet build framework/SimpleModule.Blazor/SimpleModule.Blazor.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Blazor/Components/Layout/PublicLayout.razor
git commit -m "feat: add mobile full-screen overlay menu to PublicLayout"
```

---

## Chunk 4: ManageLayout Horizontal Tabs

### Task 5: Add Horizontal parameter to ManageNav.razor

**Files:**
- Modify: `framework/SimpleModule.Blazor/Components/ManageNav.razor`

- [ ] **Step 1: Add Horizontal parameter and update NavClass**

Replace the entire `@code` block with:

```csharp
@code {
    [Parameter]
    public string? ActivePage { get; set; }

    [Parameter]
    public bool HasExternalLogins { get; set; }

    [Parameter]
    public bool Horizontal { get; set; }

    private string NavClass(string page)
    {
        var isActive = string.Equals(ActivePage, page, StringComparison.OrdinalIgnoreCase);
        if (Horizontal)
        {
            return isActive ? "manage-tab-active" : "manage-tab";
        }
        return isActive
            ? "nav-link-active flex items-center gap-3"
            : "nav-link-inactive flex items-center gap-3";
    }
}
```

- [ ] **Step 2: Update the nav element classes to be responsive**

Replace line 1:
```html
<nav class="space-y-1">
```
With:
```html
<nav class="@(Horizontal ? "flex overflow-x-auto scrollbar-hide border-b border-border -mx-4 px-4" : "space-y-1")">
```

- [ ] **Step 3: Hide SVG icons in horizontal mode**

Each `<a>` in ManageNav has an SVG icon. In horizontal (tab) mode, icons clutter the limited space. Wrap each SVG with a conditional:

For each `<svg class="w-4 h-4 shrink-0" ...>` element, wrap with:
```html
@if (!Horizontal)
{
    <svg class="w-4 h-4 shrink-0" ...>...</svg>
}
```

There are 6 SVG icons total (Profile, Email, Password, External Logins, Two-factor auth, Personal data). Wrap each one.

- [ ] **Step 4: Verify the build compiles**

Run: `dotnet build framework/SimpleModule.Blazor/SimpleModule.Blazor.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Blazor/Components/ManageNav.razor
git commit -m "feat: add Horizontal parameter to ManageNav for tab mode"
```

---

### Task 6: Update ManageLayout.razor to use tabs on mobile

**Files:**
- Modify: `framework/SimpleModule.Blazor/Components/Layout/ManageLayout.razor`

- [ ] **Step 1: Replace the layout markup**

Replace lines 6-27 (the entire content div) with:

```html
<div class="mx-auto w-full max-w-7xl px-4 md:px-6 lg:px-8 space-y-6">
    <div class="flex items-start justify-between gap-4">
        <div>
            <h1 class="text-xl md:text-2xl font-extrabold tracking-tight" style="font-family:'Sora',sans-serif;">
                <span class="gradient-text">Account Settings</span>
            </h1>
            <p class="text-text-muted text-sm mt-1">Manage your profile, security, and preferences</p>
        </div>
    </div>

    <!-- Mobile: horizontal tab bar -->
    <div class="md:hidden">
        <ManageNav ActivePage="@GetActivePage()" HasExternalLogins="@_hasExternalLogins" Horizontal="true" />
    </div>

    <div class="flex flex-col md:flex-row gap-6">
        <!-- Desktop: vertical sidebar nav -->
        <aside class="hidden md:block md:w-56 shrink-0">
            <div class="glass-card p-3 sticky top-20">
                <ManageNav ActivePage="@GetActivePage()" HasExternalLogins="@_hasExternalLogins" />
            </div>
        </aside>
        <div class="flex-1 min-w-0">
            <div class="glass-card p-4 md:p-8">
                @Body
            </div>
        </div>
    </div>
</div>
```

Key changes:
- `sm:` → `md:` breakpoints throughout (consistent 768px threshold)
- Mobile tab bar shown via `md:hidden` with `Horizontal="true"`
- Desktop sidebar hidden on mobile via `hidden md:block`
- Content card padding: `p-4 md:p-8`

- [ ] **Step 2: Verify the build compiles**

Run: `dotnet build framework/SimpleModule.Blazor/SimpleModule.Blazor.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add framework/SimpleModule.Blazor/Components/Layout/ManageLayout.razor
git commit -m "feat: add horizontal tab bar to ManageLayout for mobile"
```

---

## Chunk 5: Final Verification

### Task 7: Full build and visual verification

- [ ] **Step 1: Full solution build**

Run: `dotnet build`
Expected: Build succeeded with no errors.

- [ ] **Step 2: Frontend build**

Run: `npm run build`
Expected: Build completes with no errors.

- [ ] **Step 3: Lint check**

Run: `npm run check`
Expected: No errors (or only pre-existing warnings).

- [ ] **Step 4: Run tests**

Run: `dotnet test`
Expected: All tests pass (layout changes are SSR markup — no test changes needed).

- [ ] **Step 5: Commit any remaining fixes**

If any build/lint issues were found in steps 1-3, fix and commit:
```bash
git commit -m "fix: resolve build/lint issues from layout redesign"
```
