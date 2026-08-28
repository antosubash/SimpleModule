# Vertical-Slice Feature Folders — Settings Phase 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the vertical-slice feature-folder pattern (validated by the Phase 1 Notifications pilot) to the Settings module, converting 16 API endpoints across 3 resource groups and two cross-module contract services (`SettingsService` → `ISettingsContracts`; `PublicMenuService` → `IPublicMenuProvider`) without changing any externally visible behaviour.

**Architecture:** Reuse the migration tooling shipped in Phase 1 (`scripts/feature-folder-migrate.mjs` + per-module TSV manifest). Endpoints move from `Endpoints/<Group>/` to `Features/<Group>/<Op>/`. Both services become `public sealed partial class`es whose root fragments live in `Infrastructure/` and whose per-operation method bodies live next to their endpoints. Feature-scoped request DTOs (`CreateMenuItemRequest`, `UpdateMenuItemRequest`, `ReorderMenuItemsRequest`, `UpdateSettingRequest`) move to `Contracts/Features/<Group>/<Op>/`. Cross-module DTOs (`Setting`, `SettingsFilter`, `PublicMenuItemDto`), entity types, value objects, events, and the `ISettingsContracts` interface stay at `Contracts/` root to preserve external `using` statements (verified: 11 external consumer files; all use root-namespace types only).

**Tech Stack:** .NET 10, EF Core, xUnit.v3, FluentAssertions, Node 22+ (for migration script), git. No new runtime dependencies.

**Spec:** `docs/superpowers/specs/2026-05-14-vertical-slice-feature-folders-design.md`. Authority on conventions C1–C8.
**Phase 1 reference plan:** `docs/superpowers/plans/2026-05-14-vertical-slice-notifications-pilot.md` and PR #200.
**Pilot lessons:** `tasks/lessons.md` — load-bearing guidance, especially the "no partials in manifest" rule.

---

## Pre-flight check

```bash
git rev-parse --abbrev-ref HEAD
# Expected: a feature branch dedicated to this Phase 2 work (e.g. settings-vertical-slice)
git log --oneline -1 -- scripts/feature-folder-migrate.mjs
# Migration script must already be present (either via Phase 1 PR merged, or branched off Phase 1)
ls scripts/feature-folder-migrate.mjs scripts/manifests/notifications.tsv
# Both must exist (sanity check that tooling is in place)
dotnet tool restore
# csharpier required for pre-commit hook
```

If any check fails, stop and reconcile. Either the migration tooling isn't merged yet (wait for #200 to land or branch from it) or csharpier is missing.

---

## Scope inventory

**Files moving (impl project, `modules/Settings/src/SimpleModule.Settings/`):**

| From | To | Count |
|---|---|---|
| `SettingsDbContext.cs` | `Infrastructure/SettingsDbContext.cs` | 1 |
| `SettingsService.cs` | `Infrastructure/SettingsService.cs` (becomes root partial) | 1 |
| `Services/PublicMenuService.cs` | `Infrastructure/PublicMenuService.cs` (becomes root partial) | 1 |
| `EntityConfigurations/*.cs` | `Infrastructure/EntityConfigurations/*.cs` | 2 |
| `Endpoints/Menus/*Endpoint.cs` | `Features/Menus/<Op>/<Op>Endpoint.cs` | 8 |
| `Endpoints/Settings/*Endpoint.cs` | `Features/Settings/<Op>/<Op>Endpoint.cs` | 5 |
| `Endpoints/UserSettings/*Endpoint.cs` | `Features/UserSettings/<Op>/<Op>Endpoint.cs` | 3 |
| **Total impl-side moves** | | **21** |

**Files moving (contracts project, `modules/Settings/src/SimpleModule.Settings.Contracts/`):**

| From | To |
|---|---|
| `CreateMenuItemRequest.cs` | `Features/Menus/Create/CreateMenuItemRequest.cs` |
| `UpdateMenuItemRequest.cs` | `Features/Menus/Update/UpdateMenuItemRequest.cs` |
| `ReorderMenuItemsRequest.cs` | `Features/Menus/Reorder/ReorderMenuItemsRequest.cs` |
| `UpdateSettingRequest.cs` | `Features/Settings/UpdateSetting/UpdateSettingRequest.cs` |

**Files staying put:**

- `Contracts/` root: `ISettingsContracts.cs`, `SettingEntity.cs`, `PublicMenuItemEntity.cs`, `SettingId.cs`, `PublicMenuItemId.cs`, `Setting.cs`, `SettingsFilter.cs`, `PublicMenuItemDto.cs`, `SettingsConstants.cs`, `Events/*` — all consumed externally or by EF migrations.
- `Pages/` (intact per spec C4): `AdminSettings*`, `MenuManager*`, `UserSettings*`, `index.ts`, `components/menu-helpers.ts`.
- Module-root files (C7): `SettingsModule.cs`, `SettingsModuleOptions.cs`, `SettingsPermissions.cs`, `Locales/`, `components/`, `types.ts`, `vite.config.ts`.

**New partial-class fragment files (created from service split):**

| Service | Fragments (one per operation method, in feature folder) |
|---|---|
| `SettingsService` (root in `Infrastructure/`) | `Features/Settings/GetSetting/SettingsService.GetSetting.cs` (covers both `GetSettingAsync` overloads), `ResolveUserSetting/SettingsService.ResolveUserSetting.cs`, `UpdateSetting/SettingsService.UpdateSetting.cs` (covers `SetSettingAsync`), `DeleteSetting/SettingsService.DeleteSetting.cs`, `GetSettings/SettingsService.GetSettings.cs` |
| `PublicMenuService` (root in `Infrastructure/`) | `Features/Menus/GetMenuTree/PublicMenuService.GetMenuTree.cs`, `GetHomePageUrl/PublicMenuService.GetHomePageUrl.cs` (no endpoint — provider-interface method), `GetAvailablePages/PublicMenuService.GetAll.cs`, `Create/PublicMenuService.Create.cs`, `Update/PublicMenuService.Update.cs`, `Delete/PublicMenuService.Delete.cs`, `Reorder/PublicMenuService.Reorder.cs`, `SetHomePage/PublicMenuService.SetHomePage.cs`, `ClearHomePage/PublicMenuService.ClearHomePage.cs` |

> **Spec C2 reminder:** every fragment file declares `namespace SimpleModule.Settings.Infrastructure;` regardless of folder. The folder identifies the slice; the namespace identifies the type.

**Tests (`modules/Settings/tests/SimpleModule.Settings.Tests/`):**

Existing layout:
- `Unit/SettingsServiceTests.cs` — direct service tests
- `Unit/PublicMenuServiceTests.cs` — direct service tests
- `Integration/SettingsEndpointTests.cs` — HTTP tests using `SimpleModuleWebApplicationFactory`
- `Integration/MenuEndpointTests.cs` — HTTP tests

Target layout (mirrors impl per spec C5):
- `Features/Settings/<Op>/<Op>Tests.cs` (split from `Unit/SettingsServiceTests.cs`), plus shared `Features/Settings/SettingsServiceTestFixture.cs`
- `Features/Menus/<Op>/<Op>Tests.cs` (split from `Unit/PublicMenuServiceTests.cs`), plus shared `Features/Menus/PublicMenuServiceTestFixture.cs`
- Integration tests **stay in `Integration/`** because they cover cross-feature flows and don't fit one operation folder. The plan does not split them.

---

## Task 1: Pre-flight + write the Settings manifest

**Files:**
- Create: `scripts/manifests/settings.tsv`

The manifest covers Infrastructure moves (5 rows: DbContext + 2 services + 2 entity configs), endpoint moves (16 rows across 3 resource groups), and contracts moves (4 rows). Per the Phase 1 pilot lesson: **the manifest must not include the two services as partial-class moves** — but they aren't partials yet, so they're safe to include here. The split into partials happens in Tasks 4–5.

- [ ] **Step 1: Pre-flight (per the section above)**

- [ ] **Step 2: Write `scripts/manifests/settings.tsv`**

Use literal TAB characters between columns. Sections:

```
# Settings Phase 2 — feature-folder migration manifest.
# Columns: OLD_PATH<TAB>NEW_PATH<TAB>ASSEMBLY_NAME

# --- Infrastructure moves (impl project) ---
modules/Settings/src/SimpleModule.Settings/SettingsDbContext.cs<TAB>modules/Settings/src/SimpleModule.Settings/Infrastructure/SettingsDbContext.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/SettingsService.cs<TAB>modules/Settings/src/SimpleModule.Settings/Infrastructure/SettingsService.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Services/PublicMenuService.cs<TAB>modules/Settings/src/SimpleModule.Settings/Infrastructure/PublicMenuService.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/EntityConfigurations/PublicMenuItemEntityConfiguration.cs<TAB>modules/Settings/src/SimpleModule.Settings/Infrastructure/EntityConfigurations/PublicMenuItemEntityConfiguration.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/EntityConfigurations/SettingEntityConfiguration.cs<TAB>modules/Settings/src/SimpleModule.Settings/Infrastructure/EntityConfigurations/SettingEntityConfiguration.cs<TAB>SimpleModule.Settings

# --- Feature moves: Menus endpoints ---
modules/Settings/src/SimpleModule.Settings/Endpoints/Menus/ClearHomePageEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Menus/ClearHomePage/ClearHomePageEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Menus/CreateMenuItemEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Menus/Create/CreateMenuItemEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Menus/DeleteMenuItemEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Menus/Delete/DeleteMenuItemEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Menus/GetAvailablePagesEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Menus/GetAvailablePages/GetAvailablePagesEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Menus/GetMenuTreeEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Menus/GetMenuTree/GetMenuTreeEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Menus/ReorderMenuItemsEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Menus/Reorder/ReorderMenuItemsEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Menus/SetHomePageEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Menus/SetHomePage/SetHomePageEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Menus/UpdateMenuItemEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Menus/Update/UpdateMenuItemEndpoint.cs<TAB>SimpleModule.Settings

# --- Feature moves: Settings endpoints ---
modules/Settings/src/SimpleModule.Settings/Endpoints/Settings/DeleteSettingEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Settings/DeleteSetting/DeleteSettingEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Settings/GetDefinitionsEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Settings/GetDefinitions/GetDefinitionsEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Settings/GetSettingEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Settings/GetSetting/GetSettingEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Settings/GetSettingsEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Settings/GetSettings/GetSettingsEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/Settings/UpdateSettingEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/Settings/UpdateSetting/UpdateSettingEndpoint.cs<TAB>SimpleModule.Settings

# --- Feature moves: UserSettings endpoints ---
modules/Settings/src/SimpleModule.Settings/Endpoints/UserSettings/DeleteMySettingEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/UserSettings/DeleteMySetting/DeleteMySettingEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/UserSettings/GetMySettingsEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/UserSettings/GetMySettings/GetMySettingsEndpoint.cs<TAB>SimpleModule.Settings
modules/Settings/src/SimpleModule.Settings/Endpoints/UserSettings/UpdateMySettingEndpoint.cs<TAB>modules/Settings/src/SimpleModule.Settings/Features/UserSettings/UpdateMySetting/UpdateMySettingEndpoint.cs<TAB>SimpleModule.Settings

# --- Feature-scoped DTO moves (contracts project) ---
modules/Settings/src/SimpleModule.Settings.Contracts/CreateMenuItemRequest.cs<TAB>modules/Settings/src/SimpleModule.Settings.Contracts/Features/Menus/Create/CreateMenuItemRequest.cs<TAB>SimpleModule.Settings.Contracts
modules/Settings/src/SimpleModule.Settings.Contracts/UpdateMenuItemRequest.cs<TAB>modules/Settings/src/SimpleModule.Settings.Contracts/Features/Menus/Update/UpdateMenuItemRequest.cs<TAB>SimpleModule.Settings.Contracts
modules/Settings/src/SimpleModule.Settings.Contracts/ReorderMenuItemsRequest.cs<TAB>modules/Settings/src/SimpleModule.Settings.Contracts/Features/Menus/Reorder/ReorderMenuItemsRequest.cs<TAB>SimpleModule.Settings.Contracts
modules/Settings/src/SimpleModule.Settings.Contracts/UpdateSettingRequest.cs<TAB>modules/Settings/src/SimpleModule.Settings.Contracts/Features/Settings/UpdateSetting/UpdateSettingRequest.cs<TAB>SimpleModule.Settings.Contracts
```

Total rows: **25** (5 Infrastructure + 16 endpoints + 4 contracts).

- [ ] **Step 3: Verify manifest parses and sources exist**

```bash
node -e "
import('./scripts/feature-folder-migrate.mjs').then(({ parseManifest }) => {
  const fs = require('fs');
  const text = fs.readFileSync('scripts/manifests/settings.tsv', 'utf8');
  const rows = parseManifest(text);
  console.log('Parsed rows:', rows.length);
  for (const r of rows) {
    if (!fs.existsSync(r.oldPath)) { console.error('MISSING:', r.oldPath); process.exit(1); }
  }
  console.log('All source files exist.');
});
"
```

Expected: `Parsed rows: 25` and `All source files exist.`

- [ ] **Step 4: Commit**

```bash
git add scripts/manifests/settings.tsv
git -c commit.gpgsign=false commit -m "chore(settings): add feature-folder migration manifest"
```

---

## Task 2: Apply Infrastructure moves

Apply the 5 Infrastructure rows of the manifest, fix `using` statements, verify build + tests.

- [ ] **Step 1: Extract and apply Infrastructure section**

```bash
sed -n '/# --- Infrastructure moves/,/# --- Feature moves: Menus endpoints/p' scripts/manifests/settings.tsv \
  | grep -v '^#' | grep -v '^$' > /tmp/settings-infra.tsv
wc -l /tmp/settings-infra.tsv   # expected: 5 /tmp/settings-infra.tsv
node scripts/feature-folder-migrate.mjs /tmp/settings-infra.tsv
rm /tmp/settings-infra.tsv
```

- [ ] **Step 2: Build, expect failure**

```bash
dotnet build modules/Settings/src/SimpleModule.Settings/SimpleModule.Settings.csproj 2>&1 | tail -40
```

Expected errors in `SettingsModule.cs` and possibly elsewhere because `using SimpleModule.Settings.Services;` (PublicMenuService's old namespace) no longer resolves. Other affected files are likely the endpoints (they `inject PublicMenuService` by type) and any test infra that wires PublicMenuService.

**Namespace mapping after these moves:**
- `SimpleModule.Settings` (root namespace, where SettingsService/SettingsDbContext previously lived) → unchanged for module-root files, but the moved DbContext+SettingsService now live in `SimpleModule.Settings.Infrastructure`
- `SimpleModule.Settings.Services` → `SimpleModule.Settings.Infrastructure`
- `SimpleModule.Settings.EntityConfigurations` → `SimpleModule.Settings.Infrastructure.EntityConfigurations`

- [ ] **Step 3: Fix `using` statements in consumers**

Run `dotnet build` after each batch. Likely consumers (verify with the compiler errors):

| File | Substitute |
|---|---|
| `modules/Settings/src/SimpleModule.Settings/SettingsModule.cs` | Add `using SimpleModule.Settings.Infrastructure;` (covers `SettingsDbContext`, `PublicMenuService`); remove `using SimpleModule.Settings.Services;` |
| All endpoints under `modules/Settings/src/SimpleModule.Settings/Endpoints/Menus/*.cs` | If any references `PublicMenuService` by type, add `using SimpleModule.Settings.Infrastructure;` |
| Tests in `modules/Settings/tests/SimpleModule.Settings.Tests/Unit/*.cs` | Replace `using SimpleModule.Settings.Services;` with `using SimpleModule.Settings.Infrastructure;`; replace `using SimpleModule.Settings;` (for SettingsDbContext / SettingsService) with `using SimpleModule.Settings.Infrastructure;` |
| `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory*.cs` | Grep for `SimpleModule.Settings.Services` and `SimpleModule.Settings.EntityConfigurations`; replace as above. **The Phase 1 pilot showed these factory files are real cross-cutting consumers — check them.** |
| `framework/SimpleModule.Hosting/*.cs` | The Hosting framework references `PublicMenuService` indirectly via `IPublicMenuProvider` (interface in Core). No `using` change needed unless it imports the implementation namespace directly. Grep to verify. |

- [ ] **Step 4: Build and test**

```bash
dotnet build 2>&1 | tail -10
# 0 errors expected
dotnet test modules/Settings/tests/SimpleModule.Settings.Tests/SimpleModule.Settings.Tests.csproj --nologo 2>&1 | tail -10
# All tests pass
dotnet build 2>&1 | tail -10  # full solution
```

- [ ] **Step 5: Commit**

```bash
git add modules/Settings/ tests/
git -c commit.gpgsign=false commit -m "refactor(settings): move infrastructure files into Infrastructure/ subfolder"
```

(If the diff includes any unrelated `tests/` changes outside `tests/SimpleModule.Tests.Shared/Fixtures/`, abort and investigate.)

---

## Task 3: Apply Features moves (endpoints + contracts)

Apply the 16 endpoint rows + 4 contracts rows. Fix the cascading `using` statements for the moved request DTOs.

- [ ] **Step 1: Extract and apply Feature+Contracts sections**

```bash
sed -n '/# --- Feature moves: Menus endpoints/,$p' scripts/manifests/settings.tsv \
  | grep -v '^#' | grep -v '^$' > /tmp/settings-features.tsv
wc -l /tmp/settings-features.tsv   # expected: 20
node scripts/feature-folder-migrate.mjs /tmp/settings-features.tsv
rm /tmp/settings-features.tsv
```

- [ ] **Step 2: Build, expect failures for moved DTOs**

```bash
dotnet build 2>&1 | tail -40
```

Each moved request DTO is now in a sub-namespace. Likely failing files:
- `Infrastructure/SettingsService.cs` (references `UpdateSettingRequest`)
- `Infrastructure/PublicMenuService.cs` (references `CreateMenuItemRequest`, `UpdateMenuItemRequest`, `ReorderMenuItemsRequest`)
- The four endpoint files for the moved DTOs (now in `Features/...`)
- `Contracts/ISettingsContracts.cs` — its method signatures reference `Setting`, `SettingsFilter` (both stay at root), and (importantly) NOT the request DTOs — confirm. If it does reference a moved DTO, add the new `using`.

- [ ] **Step 3: Add new `using` statements**

For each consumer of a moved DTO, add the appropriate `using` after the existing `using SimpleModule.Settings.Contracts;`:

| Moved DTO | New `using` to add |
|---|---|
| `CreateMenuItemRequest` | `using SimpleModule.Settings.Contracts.Features.Menus.Create;` |
| `UpdateMenuItemRequest` | `using SimpleModule.Settings.Contracts.Features.Menus.Update;` |
| `ReorderMenuItemsRequest` | `using SimpleModule.Settings.Contracts.Features.Menus.Reorder;` |
| `UpdateSettingRequest` | `using SimpleModule.Settings.Contracts.Features.Settings.UpdateSetting;` |

Find consumers:

```bash
for type in CreateMenuItemRequest UpdateMenuItemRequest ReorderMenuItemsRequest UpdateSettingRequest; do
  echo "== $type =="
  grep -rln "\\b$type\\b" --include='*.cs' .
done
```

- [ ] **Step 4: Build and test**

```bash
dotnet build 2>&1 | tail -10
dotnet test modules/Settings/tests/SimpleModule.Settings.Tests/SimpleModule.Settings.Tests.csproj --nologo 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add modules/Settings/ tests/
git -c commit.gpgsign=false commit -m "refactor(settings): move endpoints to Features/ and slice Contracts

Endpoints/{Menus,Settings,UserSettings}/* moved to Features/<Group>/<Op>/.
Feature-scoped request DTOs moved to Contracts/Features/<Group>/<Op>/.
Cross-feature DTOs and entity types remain at Contracts root."
```

---

## Task 4: Split `SettingsService` into partial-class fragments

`SettingsService` implements `ISettingsContracts` (6 methods). It becomes a `public sealed partial class`. The root fragment in `Infrastructure/SettingsService.cs` keeps the primary constructor and any private helper methods used by ≥2 operations. Per-operation method bodies move to `Features/Settings/<Op>/SettingsService.<Op>.cs`.

> **Mandatory:** All fragments declare `namespace SimpleModule.Settings.Infrastructure;` (the owning class's namespace), not the folder-derived namespace. This is the Spec C1 exception confirmed during the Phase 1 pilot.

> **Verify the method/feature mapping by reading the current `Infrastructure/SettingsService.cs`.** The recon at plan-write time showed 6 public methods:
> - `GetSettingAsync(string key, SettingScope scope, string? userId)` and `GetSettingAsync<T>(string key, SettingScope scope, string? userId)` (overload; both belong in `Features/Settings/GetSetting/`)
> - `ResolveUserSettingAsync(string key, string userId)` → `Features/Settings/ResolveUserSetting/`
> - `SetSettingAsync(string key, string value, SettingScope scope, string? userId)` → `Features/Settings/UpdateSetting/` (the public-facing operation is "update")
> - `DeleteSettingAsync(string key, SettingScope scope, string? userId)` → `Features/Settings/DeleteSetting/`
> - `GetSettingsAsync(SettingsFilter? filter)` → `Features/Settings/GetSettings/`

- [ ] **Step 1: Rewrite `Infrastructure/SettingsService.cs` as the root partial**

The root keeps the primary constructor signature (whatever it is — read the current file). It exposes the implements clause `: ISettingsContracts` and any private fields/helpers shared across ≥2 fragments.

```csharp
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Settings.Infrastructure;

public sealed partial class SettingsService(/* primary ctor params here — copy from existing */) : ISettingsContracts
{
    // Shared private helpers (e.g. serialization, cache invalidation) live here if any.
}
```

If `SettingsService` already uses a primary constructor with multiple parameters, preserve them verbatim. If it has a regular constructor, convert it to a primary constructor (the Notifications pilot used primary constructor form for both Phase 1 services).

- [ ] **Step 2–6: Create five fragment files**

Create one file per operation under `Features/Settings/<Op>/SettingsService.<Op>.cs`. For each, copy the corresponding method body verbatim from the current `SettingsService.cs`, wrap in `public sealed partial class SettingsService { ... }` with `namespace SimpleModule.Settings.Infrastructure;`, and include the minimal `using`s the method body needs.

Specific files to create:

- `Features/Settings/GetSetting/SettingsService.GetSetting.cs` — contains BOTH overloads of `GetSettingAsync` (six methods total share this fragment because both overloads belong to the same logical operation: "get a setting").
- `Features/Settings/ResolveUserSetting/SettingsService.ResolveUserSetting.cs` — `ResolveUserSettingAsync`.
- `Features/Settings/UpdateSetting/SettingsService.UpdateSetting.cs` — `SetSettingAsync`. Co-located with `UpdateSettingEndpoint.cs`, `UpdateSettingRequest.cs`.
- `Features/Settings/DeleteSetting/SettingsService.DeleteSetting.cs` — `DeleteSettingAsync`.
- `Features/Settings/GetSettings/SettingsService.GetSettings.cs` — `GetSettingsAsync(SettingsFilter?)`.

Notice that some operations have **no API endpoint** (`ResolveUserSettingAsync` is contract-only, called by other modules through `ISettingsContracts`). Their feature folder holds only the fragment, no endpoint. This matches the Notifications pilot's `GetById` case.

- [ ] **Step 7: Build, then test**

```bash
dotnet build 2>&1 | tail -10
# 0 errors. If "ISettingsContracts is not fully implemented" → method missing in some fragment.
# If SM0025/SM0026 → namespace mismatch.
dotnet test modules/Settings/tests/SimpleModule.Settings.Tests/SimpleModule.Settings.Tests.csproj --nologo 2>&1 | tail -10
```

- [ ] **Step 8: Commit**

```bash
git add modules/Settings/
git -c commit.gpgsign=false commit -m "refactor(settings): split SettingsService into per-feature partials

SettingsService becomes a sealed partial class. Each operation method moves to
its feature folder fragment. Root partial in Infrastructure/ retains the
primary constructor and shared helpers."
```

---

## Task 5: Split `PublicMenuService` into partial-class fragments

Same shape as Task 4, but for the second cross-module service. `PublicMenuService` implements `IPublicMenuProvider` (from `framework/SimpleModule.Core/Menu/IPublicMenuProvider.cs`) AND is also injected directly by type into Menus endpoints.

> **Method-to-feature mapping** (verify by reading current `Infrastructure/PublicMenuService.cs`):
>
> | Method | Feature folder | Has endpoint? |
> |---|---|---|
> | `GetMenuTreeAsync` | `Features/Menus/GetMenuTree/` | yes |
> | `GetHomePageUrlAsync` | `Features/Menus/GetHomePageUrl/` | no — provider-interface method, called by Inertia middleware |
> | `GetAllAsync` (returns `List<PublicMenuItemDto>`) | `Features/Menus/GetAvailablePages/` | yes (this is the admin "list all menu items" operation backing `GetAvailablePagesEndpoint`) |
> | `CreateAsync(CreateMenuItemRequest)` | `Features/Menus/Create/` | yes |
> | `UpdateAsync(...)` | `Features/Menus/Update/` | yes |
> | `DeleteAsync(PublicMenuItemId)` | `Features/Menus/Delete/` | yes |
> | `ReorderAsync(ReorderMenuItemsRequest)` | `Features/Menus/Reorder/` | yes |
> | `SetHomePageAsync(PublicMenuItemId)` | `Features/Menus/SetHomePage/` | yes |
> | `ClearHomePageAsync()` | `Features/Menus/ClearHomePage/` | yes |
>
> **If the recon-time method-name → endpoint mapping above doesn't match what `PublicMenuService.cs` actually has, follow the actual file. Operation folders are named for the endpoint they serve.**

- [ ] **Step 1: Rewrite `Infrastructure/PublicMenuService.cs` as the root partial**

```csharp
using SimpleModule.Core.Menu;

namespace SimpleModule.Settings.Infrastructure;

public sealed partial class PublicMenuService(/* primary ctor params */) : IPublicMenuProvider
{
    // Shared helpers (e.g. URL normalization, tree assembly) live here if any.
}
```

- [ ] **Step 2–10: Create nine fragment files**

One per method, per the table above. Each declares `namespace SimpleModule.Settings.Infrastructure;` regardless of folder. Each includes only the `using`s it needs (e.g. `Microsoft.EntityFrameworkCore`, `SimpleModule.Settings.Contracts`, the moved request DTOs' new namespaces, etc.).

- [ ] **Step 11: Build and test**

Same commands as Task 4 step 7. Expect 0 errors, all Settings tests pass.

- [ ] **Step 12: Commit**

```bash
git add modules/Settings/
git -c commit.gpgsign=false commit -m "refactor(settings): split PublicMenuService into per-feature partials

PublicMenuService becomes a sealed partial class implementing IPublicMenuProvider.
Each menu operation (Create, Update, Delete, Reorder, SetHomePage, ClearHomePage,
GetMenuTree, GetHomePageUrl, GetAvailablePages) moves to its feature folder."
```

---

## Task 6: Split `SettingsServiceTests` and `PublicMenuServiceTests` by feature

Mirror the impl tree. Each test file moves into the feature folder of the method it covers. A shared fixture per service lives at the **aggregate folder level** (per spec C5 amendment from Phase 1).

Per the Phase 1 lesson on analyzer rules: shared fixture classes must use **private backing fields + protected properties** (not `protected readonly` fields), and implement the canonical `Dispose(bool)` pattern, to satisfy `CA1051` and `CA1063` under `TreatWarningsAsErrors=true`.

- [ ] **Step 1: Create `Features/Settings/SettingsServiceTestFixture.cs`**

Read `Unit/SettingsServiceTests.cs` to identify its setup pattern. Extract the DbContext bootstrap + any shared seed helpers into an abstract fixture at `tests/SimpleModule.Settings.Tests/Features/Settings/SettingsServiceTestFixture.cs`. Use the Phase 1 pattern (see `NotificationServiceTestFixture.cs` in the same test project, added in PR #200) as the template — including private backing fields + protected properties + `Dispose(bool)`.

- [ ] **Step 2: Split `Unit/SettingsServiceTests.cs` into per-operation files**

Group tests by the method they exercise:
- Tests for `GetSettingAsync` (either overload) → `tests/.../Features/Settings/GetSetting/GetSettingAsyncTests.cs`
- Tests for `ResolveUserSettingAsync` → `tests/.../Features/Settings/ResolveUserSetting/ResolveUserSettingAsyncTests.cs`
- Tests for `SetSettingAsync` → `tests/.../Features/Settings/UpdateSetting/SetSettingAsyncTests.cs`
- Tests for `DeleteSettingAsync` → `tests/.../Features/Settings/DeleteSetting/DeleteSettingAsyncTests.cs`
- Tests for `GetSettingsAsync` → `tests/.../Features/Settings/GetSettings/GetSettingsAsyncTests.cs`

Each new file inherits from `SettingsServiceTestFixture`. `git rm tests/.../Unit/SettingsServiceTests.cs` after extraction.

- [ ] **Step 3: Create `Features/Menus/PublicMenuServiceTestFixture.cs`**

Same as Step 1, for `Unit/PublicMenuServiceTests.cs`. Fixture goes at `tests/.../Features/Menus/PublicMenuServiceTestFixture.cs`.

- [ ] **Step 4: Split `Unit/PublicMenuServiceTests.cs` into per-operation files**

One per `PublicMenuService` method that has tests. Place each under `tests/.../Features/Menus/<Op>/<Method>Tests.cs`. Methods without existing tests don't need empty files. `git rm` the original after.

- [ ] **Step 5: Integration tests stay put**

`Integration/SettingsEndpointTests.cs` and `Integration/MenuEndpointTests.cs` cover cross-feature HTTP flows. Do NOT split them. They remain in `Integration/`. Add a comment at the top of each clarifying they intentionally span multiple feature folders (one line).

- [ ] **Step 6: Build and test**

```bash
dotnet build 2>&1 | tail -10
dotnet test modules/Settings/tests/SimpleModule.Settings.Tests/SimpleModule.Settings.Tests.csproj --nologo 2>&1 | tail -20
```

Same test count as before, all passing.

- [ ] **Step 7: Commit**

```bash
git add modules/Settings/tests/
git -c commit.gpgsign=false commit -m "test(settings): split SettingsServiceTests and PublicMenuServiceTests by feature

Per-operation test files mirror the impl tree under Features/. Shared fixtures
live at the aggregate folder level. Integration tests covering cross-feature
flows remain in Integration/ unchanged."
```

---

## Task 7: Pilot verification

Same gates as the Phase 1 pilot, plus an explicit cross-module check because Settings has 11 external Contracts consumers.

- [ ] **Step 1: `dotnet build`**

```bash
dotnet build 2>&1 | tail -10
```

Expected: 0 errors. (Warnings about `node_modules` are environmental.)

- [ ] **Step 2: `dotnet test` (full solution)**

```bash
dotnet test --nologo 2>&1 | tail -15
```

Expected: total pass count matches pre-refactor (995 from Phase 1 baseline; Settings tests are within that count).

- [ ] **Step 3: Frontend gates**

```bash
npm install 2>&1 | tail -5
npm run validate-pages 2>&1 | tail -5
npm run build:dev 2>&1 | tail -5
npm run check 2>&1 | tail -5
```

All clean (only the pre-existing `routes.ts` format issue is acceptable).

- [ ] **Step 4: Cross-module sanity check**

```bash
# Build the 4 known external consumer modules in isolation to confirm Contracts compat.
for proj in modules/AuditLogs/src/SimpleModule.AuditLogs/SimpleModule.AuditLogs.csproj \
            modules/Localization/src/SimpleModule.Localization/SimpleModule.Localization.csproj \
            modules/Users/src/SimpleModule.Users/SimpleModule.Users.csproj \
            tests/SimpleModule.Core.Tests/SimpleModule.Core.Tests.csproj; do
  echo "== $proj =="
  dotnet build "$proj" 2>&1 | tail -3
done
```

Expected: each builds with 0 errors. If any complains about a missing `Setting` or `SettingsFilter` namespace, an entity-adjacent file was moved by mistake — investigate and revert.

- [ ] **Step 5: Source-generator output check**

```bash
dotnet build template/SimpleModule.Host/SimpleModule.Host.csproj /p:EmitCompilerGeneratedFiles=true /p:CompilerGeneratedFilesOutputPath=obj/generated 2>&1 | tail -3
grep -A1 'CreateMenuItemEndpoint\|UpdateSettingEndpoint\|GetMySettingsEndpoint' \
  template/SimpleModule.Host/obj/generated/SimpleModule.Generator/*/*.cs 2>/dev/null | head -20
```

Expected: endpoints registered under their new `Features.<Group>.<Op>` namespaces. Zero references to old `Endpoints.Menus`, `Endpoints.Settings`, `Endpoints.UserSettings` paths.

After: `rm -rf template/SimpleModule.Host/obj/generated`.

- [ ] **Step 6: Directory shape**

```bash
ls modules/Settings/src/SimpleModule.Settings/
```

Expected: `Features/`, `Infrastructure/`, `Pages/`, `bin/`, `obj/`, `SettingsModule.cs`, `SettingsModuleOptions.cs`, `SettingsPermissions.cs`, `SimpleModule.Settings.csproj`, `Locales/`, `components/`, `package.json`, `types.ts`, `vite.config.ts`, optionally `wwwroot/`, `node_modules/`. **No** top-level `Endpoints/`, `Services/`, `EntityConfigurations/`.

```bash
ls modules/Settings/src/SimpleModule.Settings/Infrastructure/
# Expected: EntityConfigurations/, PublicMenuService.cs, SettingsDbContext.cs, SettingsService.cs
ls modules/Settings/src/SimpleModule.Settings/Features/
# Expected: Menus/, Settings/, UserSettings/
```

- [ ] **Step 7: No commit unless something needed fixing in Steps 1–6**

If any step required a fix, commit it now with a precise message. Otherwise this task produces no commit.

---

## Task 8: Update lessons.md with Phase 2 findings

Append new lessons learned to `tasks/lessons.md`. The Phase 1 section stays untouched at the top.

Expected new lessons (capture during execution; the list below is the *expected* set — add or revise as actually observed):

```markdown
## Vertical-slice feature-folder Phase 2 (Settings, 2026-05-15)

- **Two services in one module is straightforward.** The partial-class split works the same whether a module has one or several contract implementations. Each service gets its own root partial in `Infrastructure/` and its own fan-out of fragments. SM0025/SM0026 verify each separately.
- **Cross-module Contracts consumers grew to 11 (vs zero for Notifications), but the blast radius stayed near zero.** All external consumers used the shared types (`ISettingsContracts`, `Setting`, `SettingsFilter`, events) — exactly the types we deliberately left at Contracts root. This validates the "feature-scoped requests move, everything else stays" rule.
- **Integration tests don't fit one feature folder.** They span multiple endpoints/operations by design. Keep them in `Integration/`; do NOT force them into `Features/<Op>/`. Unit tests of services are the ones that split per-feature.
- **Provider-interface methods without an API endpoint follow the same pattern as contract-only methods.** `PublicMenuService.GetHomePageUrlAsync` (called by Inertia middleware, no HTTP endpoint) gets its own feature folder with just the fragment, no endpoint file — same as `NotificationService.GetByIdAsync` in Phase 1.
- **Add any further surprises here as they appear during execution.**
```

- [ ] **Step 1: Append the new section to `tasks/lessons.md`**

Preserve the existing Phase 1 section. Add the new section at the end.

- [ ] **Step 2: Commit**

```bash
git add tasks/lessons.md
git -c commit.gpgsign=false commit -m "docs: log Phase 2 (Settings) feature-folder pilot lessons"
```

---

## Exit criteria

Phase 2 is successful when **all** of the following hold:

1. `dotnet build` from the worktree root: 0 errors, 0 warnings other than environmental `node_modules not found`.
2. `dotnet test`: total pass count equals the pre-refactor count (the Settings module's test count is preserved; tests were split but not lost).
3. `npm run validate-pages` exits 0.
4. `npm run build:dev` builds all workspaces.
5. `npm run check` shows no new issues outside the pre-existing `routes.ts`.
6. Source-generator output references endpoints by their new `SimpleModule.Settings.Features.<Group>.<Op>.<X>Endpoint` namespaces; zero references to old `Endpoints.<Group>` paths.
7. The 4 known external consumer modules build cleanly (Task 7 Step 4).
8. `modules/Settings/src/SimpleModule.Settings/` has no top-level `Endpoints/`, `Services/`, or `EntityConfigurations/` folders.
9. `git log --oneline <branch> ^main` shows ~8 commits with clean scoped messages.

If any of (1)–(7) fails, Phase 2 is **not** ready to propagate. Diagnose and fix before declaring success.

---

## Out of scope

- Any other module (Phase 3 modules each get their own plan).
- Moving `Setting`, `SettingsFilter`, `PublicMenuItemDto`, entity types, value objects, or events out of `Contracts/` root.
- Reshaping `Pages/` (intact per spec C4).
- Updating `sm new module` / `sm new feature` CLI scaffolds.
- Adding any SM diagnostic to enforce the new layout.
- Splitting `Integration/SettingsEndpointTests.cs` or `Integration/MenuEndpointTests.cs` — they intentionally cover cross-feature flows.

---

## Execution prerequisites

Before starting Task 1, confirm:

1. PR #200 is **merged to `main`** (or the worktree branched from #200 is up-to-date with main's tooling). Otherwise the migration script is missing and the plan can't execute.
2. A fresh feature branch is checked out (recommended: `settings-vertical-slice`). Do not execute Phase 2 commits on top of PR #200's branch.
3. `dotnet tool restore` has been run (one-time setup per worktree — see Phase 1 lessons).
