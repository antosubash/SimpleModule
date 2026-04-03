# Mobile-First Layout Redesign

## Problem

The three Blazor SSR layouts (AppLayout, PublicLayout, ManageLayout) lack proper mobile navigation. On mobile:

- **AppLayout**: Sidebar is hidden off-screen with CSS (`translateX(-100%)`) but there is no hamburger button, no backdrop element, and no way to open it. Navigation is completely inaccessible.
- **PublicLayout**: Nav items use `hidden sm:flex` and disappear entirely on mobile with no alternative.
- **ManageLayout**: Stacks the sidebar nav above content vertically, consuming significant vertical space before the user reaches actual content.

## Design

### 1. AppLayout — Hamburger + Swipe Gesture Sidebar

**Mobile (< 768px):**

- Add a **sticky top header** (`.app-mobile-header` — CSS class already defined in theme.css) containing:
  - Hamburger icon button (left) with `aria-expanded` + `aria-label="Open navigation"` — toggles sidebar open/closed
  - App logo + "SimpleModule" text (left of center)
- Add a **backdrop element** (`.app-sidebar-backdrop`, `aria-hidden="true"`) to the DOM — clicking it closes the sidebar
- **Swipe gesture support**:
  - Swipe right from left edge (touch start X ≤ 25px) opens the sidebar
  - Swipe left on the sidebar or backdrop closes it
  - Minimum swipe distance: 50px to trigger
  - Note: iOS Safari reserves left-edge swipe for back navigation; this gesture is supplementary to the hamburger button and may not fire on all browsers
- Sidebar opens with `app-sidebar-open` class (CSS already defines `translateX(0)`)
- Sidebar collapse toggle button **hidden on mobile** — add `hidden md:block` to `.app-sidebar-toggle`
- **Body scroll lock**: toggle `overflow: hidden` on `document.body` when sidebar is open
- **Escape key**: close sidebar on `Escape` keypress
- **Resize cleanup**: `matchMedia('(min-width: 768px)')` listener clears mobile sidebar state when crossing to desktop

**Desktop (>= 768px):**
- No changes. Existing collapse/expand behavior preserved.

**Files to modify:**
- `framework/SimpleModule.Blazor/Components/Layout/AppLayout.razor` — add mobile header + backdrop HTML, update inline JS
- `template/SimpleModule.Host/wwwroot/js/shell.js` — add swipe gesture handler, mobile sidebar toggle, body scroll lock, escape key, matchMedia listener
- `packages/SimpleModule.Theme.Default/theme.css` — hide collapse toggle on mobile, `prefers-reduced-motion` rule

### 2. PublicLayout — Full-Screen Overlay Menu

**Mobile (< 768px):**

- Add a **hamburger icon button** visible only on mobile (`md:hidden`) with `aria-expanded` + `aria-label`
- Tapping opens a **full-screen overlay** (`fixed inset-0 z-50`, `role="dialog"`, `aria-modal="true"`):
  - Background matches surface color with fade-in animation
  - Close button (X icon) in top-right corner
  - Nav items rendered as large, touch-friendly links (`py-3 text-lg`, min 44px touch target)
  - Nested dropdown children become **expandable accordion sections** with chevron toggle
  - Login/Sign up buttons at the bottom of the overlay
  - **Focus trap**: vanilla JS traps Tab/Shift+Tab within the overlay while open
  - **Body scroll lock**: `overflow: hidden` on body while open
  - **Escape key**: closes overlay
- **Dual markup approach**: desktop hover dropdowns (`hidden md:block`) and mobile accordion nav (`md:hidden`) are separate markup trees — keeps CSS-only hover behavior on desktop while allowing click-driven accordions on mobile. Matches existing `hidden sm:flex` pattern already in PublicLayout.
- Overlay state managed via inline JS in PublicLayout.razor (matches existing pattern of layout-specific JS inline)

**Desktop (>= 768px):**
- No changes. Existing hover dropdowns preserved.

**Files to modify:**
- `framework/SimpleModule.Blazor/Components/Layout/PublicLayout.razor` — add hamburger button + overlay HTML + mobile accordion markup + inline JS

### 3. ManageLayout — Horizontal Scrollable Tabs

**Mobile (< 768px):**

- Replace the vertical nav sidebar with a **horizontal scrollable tab bar**:
  - Sits below the "Account Settings" heading, above content
  - Each tab = one account section (Profile, Password, 2FA, External Logins)
  - Active tab has primary color underline indicator
  - Overflows horizontally with hidden scrollbar (`overflow-x-auto scrollbar-hide`)
  - Uses `whitespace-nowrap` to prevent wrapping
- The glass-card wrapper around nav is removed on mobile (tabs are bare)
- Content card remains full-width
- **Breakpoint change**: ManageLayout currently uses `sm:` (640px) for the sidebar/content split. This changes to `md:` (768px) to match the AppLayout threshold. Between 640-768px (small tablets), users will now see horizontal tabs instead of the sidebar — this is intentional for consistency.

**Desktop (>= 768px):**
- No changes. Existing sticky sidebar preserved.

**Files to modify:**
- `framework/SimpleModule.Blazor/Components/Layout/ManageLayout.razor` — responsive classes for mobile tabs vs desktop sidebar
- `framework/SimpleModule.Blazor/Components/ManageNav.razor` — add responsive classes for horizontal variant (`flex-row overflow-x-auto` on mobile, `flex-col` on desktop)
- `packages/SimpleModule.Theme.Default/theme.css` — scrollbar-hide utility, tab active underline styles

### 4. Shared Conventions

- All breakpoints use Tailwind's `md:` (768px) as the mobile/desktop threshold, matching the existing `.app-sidebar` media query
- Touch targets minimum 44px height on mobile (WCAG 2.5.5)
- Animations use CSS transitions (0.25s ease) for consistency with existing sidebar animation
- **Reduced motion**: add `@media (prefers-reduced-motion: reduce)` rule that disables transitions on sidebar, backdrop, and overlay
- All JS is vanilla — no framework dependencies — matching existing `shell.js` pattern
- State managed via CSS classes toggled by JS, matching existing patterns
- **ARIA**: hamburger buttons get `aria-expanded`, overlays get `aria-hidden`, PublicLayout overlay gets `role="dialog"` + `aria-modal="true"`
- **Hamburger icon**: consistent 3-line SVG icon used in both AppLayout and PublicLayout (inline SVG, matching existing icon pattern)

### 5. Files Changed (Summary)

| File | Change |
|------|--------|
| `AppLayout.razor` | Add mobile header, backdrop element, update inline JS |
| `PublicLayout.razor` | Add hamburger button, full-screen overlay with accordion nav, dual markup |
| `ManageLayout.razor` | Add horizontal tab bar for mobile, keep sidebar for desktop |
| `ManageNav.razor` | Add responsive classes for horizontal/vertical variants |
| `shell.js` | Swipe gesture, mobile toggle, scroll lock, escape key, matchMedia cleanup |
| `theme.css` | Hide collapse toggle on mobile, scrollbar-hide, tab styles, prefers-reduced-motion |

### 6. What Is NOT Changing

- React page components — all layout changes are Blazor SSR
- Desktop behavior for any layout
- Sidebar content (nav items, admin section, footer)
- Theme colors, typography, or design system tokens
- Any module pages or their responsive patterns (already handled in prior commit)
