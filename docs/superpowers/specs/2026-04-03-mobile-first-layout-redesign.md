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
  - Hamburger icon button (left) — toggles sidebar open/closed
  - App logo + "SimpleModule" text (left of center)
- Add a **backdrop element** (`.app-sidebar-backdrop`) to the DOM — clicking it closes the sidebar
- **Swipe gesture support**:
  - Swipe right from left edge (touch start X ≤ 30px) opens the sidebar
  - Swipe left on the sidebar or backdrop closes it
  - Minimum swipe distance: 50px to trigger
- Sidebar opens with `app-sidebar-open` class (CSS already defines `translateX(0)`)
- Sidebar collapse toggle button hidden on mobile

**Desktop (>= 768px):**
- No changes. Existing collapse/expand behavior preserved.

**Files to modify:**
- `framework/SimpleModule.Blazor/Components/Layout/AppLayout.razor` — add mobile header + backdrop HTML, update JS
- `template/SimpleModule.Host/wwwroot/js/shell.js` — add swipe gesture handler + mobile toggle function
- `packages/SimpleModule.Theme.Default/theme.css` — minor tweaks if needed (most mobile CSS already exists)

### 2. PublicLayout — Full-Screen Overlay Menu

**Mobile (< 768px):**

- Add a **hamburger icon button** visible only on mobile (`md:hidden`)
- Tapping opens a **full-screen overlay** (`fixed inset-0 z-50`):
  - Background matches surface color with fade-in animation
  - Close button (X icon) in top-right corner
  - Nav items rendered as large, touch-friendly links (`py-3 text-lg`)
  - Nested dropdown children become **expandable accordion sections** with chevron toggle
  - Login/Sign up buttons at the bottom of the overlay
- Overlay state managed via inline JS (classList toggle)

**Desktop (>= 768px):**
- No changes. Existing hover dropdowns preserved.

**Files to modify:**
- `framework/SimpleModule.Blazor/Components/Layout/PublicLayout.razor` — add hamburger button + overlay HTML + JS

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

**Desktop (>= 768px):**
- No changes. Existing sticky sidebar preserved.

**Files to modify:**
- `framework/SimpleModule.Blazor/Components/Layout/ManageLayout.razor` — conditional rendering for mobile tabs vs desktop sidebar
- `framework/SimpleModule.Blazor/Components/Layout/ManageNav` (if separate component) — add horizontal variant
- `packages/SimpleModule.Theme.Default/theme.css` — scrollbar-hide utility, tab active styles

### 4. Shared Conventions

- All breakpoints use Tailwind's `md:` (768px) as the mobile/desktop threshold, matching the existing `.app-sidebar` media query
- Touch targets minimum 44px height on mobile (WCAG)
- Animations use CSS transitions (0.25s ease) for consistency with existing sidebar animation
- All JS is vanilla — no framework dependencies — matching existing `shell.js` pattern
- State managed via CSS classes toggled by JS, matching existing patterns

### 5. Files Changed (Summary)

| File | Change |
|------|--------|
| `AppLayout.razor` | Add mobile header, backdrop element, update inline JS |
| `PublicLayout.razor` | Add hamburger button, full-screen overlay with accordion nav |
| `ManageLayout.razor` | Add horizontal tab bar for mobile, keep sidebar for desktop |
| `shell.js` | Add swipe gesture handler, mobile sidebar toggle |
| `theme.css` | Tab styles, scrollbar-hide, minor mobile refinements |

### 6. What Is NOT Changing

- React page components — all layout changes are Blazor SSR
- Desktop behavior for any layout
- Sidebar content (nav items, admin section, footer)
- Theme colors, typography, or design system tokens
- Any module pages or their responsive patterns (already handled in prior commit)
