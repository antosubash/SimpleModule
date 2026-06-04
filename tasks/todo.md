# Task: Design-system consistency pass across all module pages

Goal: every page uses the design system consistently → run /qa → open PR with screenshots.

Scope: 13 tracked modules with `.tsx` source. The 8 untracked dirs (Agents, Chat, Datasets,
Map, Marketplace, Orders, PageBuilder, Products) are stale build artifacts with NO source — left untouched, flagged to user.

Out of scope (noted, not changed): i18n hardcoded-string gaps; the intentional centered-card
auth-page layout (only token/control bugs inside auth pages are fixed, not forced into PageShell).

## Fixes (from parallel line-level audit)

### HIGH — color tokens breaking dark mode / raw controls
- [ ] Tenants/tenantStatus.ts — raw palette → Badge variant map (success/warning/danger)
- [ ] Tenants/Browse.tsx, Manage.tsx — status span → <Badge>
- [ ] Tenants/Features.tsx — text-green/red-600 → <Badge>
- [ ] BackgroundJobs/Dashboard.tsx — text-red-500, border-red-200, hover:bg-red-50 → semantic
- [ ] BackgroundJobs/Detail.tsx — text-red-600, border-red-200, bg-red-50/text-red-800 → semantic
- [ ] FeatureFlags/Manage.tsx:264 — raw <input type=checkbox> → <Checkbox>
- [ ] RateLimiting/components/RulesTable.tsx — text-muted-foreground (undefined) → text-text-muted
- [ ] Email/History.tsx — text-destructive (undefined) → text-danger
- [ ] OpenIddict/OAuthCallback.tsx — text-muted → text-text-muted
- [ ] Dashboard/Home.tsx — text-white → text-text-inverse
- [ ] Users/Login.tsx, Register.tsx — text-white + inline var → bg-primary text-text-inverse; raw checkbox → Checkbox
- [ ] Users/LoginWith2fa.tsx — raw checkbox → Checkbox

### MEDIUM — hand-rolled layout → PageShell; custom markup → DS components
- [ ] Settings/UserSettings.tsx — Container+h1 → PageShell; error banner → Alert
- [ ] Settings/AdminSettings.tsx — error banner → Alert
- [ ] Admin/RolesCreate, RolesEdit, UsersCreate, UsersEdit — Container+Breadcrumb+h1 → PageShell
- [ ] Admin/Roles.tsx — raw <button> dismiss → Button
- [ ] OpenIddict/ClientsCreate, ClientsEdit — Container+Breadcrumb+h1 → PageShell
- [ ] OpenIddict/ActiveSessions.tsx — custom empty <p> → EmptyState
- [ ] Tenants/Create, Edit, Features — Container+Breadcrumb+h1 → PageShell
- [ ] Tenants/Features.tsx — custom empty → EmptyState
- [ ] Notifications/Inbox.tsx — custom empty → EmptyState
- [ ] Users/ManageIndex.tsx, Email.tsx — custom span badge → Badge
- [ ] Users/ExternalLogins.tsx — raw <table> → Table
- [ ] Users/Logout.tsx, PersonalData.tsx — <a class=btn-*> → Button
- [ ] Users/ManagePasskeys.tsx — custom empty → EmptyState

### LOW
- [ ] BackgroundJobs — bare border/border-b → border-border
- [ ] FileStorage/Browse.tsx — hover:bg-muted/50 → hover:bg-surface-raised

## Verify
- [ ] npm run check (biome) + typecheck
- [ ] dotnet build
- [ ] npm run validate-pages
- [ ] /qa
- [ ] PR with screenshots

## Review

34 page files across 13 modules refactored onto the design system. Net −120 LOC
(PageShell migrations removed boilerplate). No behavior/i18n changes.

- **PageShell migrations:** Admin Roles/Users Create+Edit, OpenIddict Clients Create+Edit,
  Tenants Create/Edit/Features, Settings UserSettings — hand-rolled Container+Breadcrumb+h1
  replaced with `<PageShell>`.
- **Semantic color tokens:** removed raw palette (text-red/green/yellow-*, text-white) and
  undefined tokens (text-destructive, text-muted-foreground, text-muted) across BackgroundJobs,
  Tenants, Email, Dashboard, RateLimiting, OpenIddict, Users → dark-mode-correct semantic tokens.
- **DS component swaps:** Checkbox (FeatureFlags, Login, LoginWith2fa), Badge (Tenants, Users),
  Table (Users ExternalLogins), Alert (Settings), Button (Users), EmptyState (Tenants,
  Notifications, OpenIddict, Users).
- **Bonus fix:** FeatureFlags override form `form.reset()` crash (pre-existing; found in QA).

Out of scope (left as-is): i18n hardcoded strings; the intentional centered-card auth layout;
8 untracked stale build-artifact dirs (Agents/Chat/Datasets/Map/Marketplace/Orders/PageBuilder/
Products) — flagged to the user, not touched.

## Verification (done)
- [x] biome check · validate-pages · validate:i18n (0/0) · validate:framework-scope · typecheck 13/13
- [x] dotnet build SimpleModule.Host — 0 warnings, 0 errors
- [x] npm run build:dev — all workspaces
- [x] /qa in real browser (light + dark): PageShell, color tokens, Badge/Table/EmptyState/Alert,
      Checkbox form posting — all verified; 1 pre-existing bug found + fixed + re-tested
