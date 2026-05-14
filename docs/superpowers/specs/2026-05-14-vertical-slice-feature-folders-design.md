# Vertical-Slice Feature Folders

**Status:** Design approved 2026-05-14. Pending implementation plan.
**Owner:** Anto Subash (@subashjanto)
**Spec date:** 2026-05-14

## Problem

The SimpleModule codebase is already substantially feature-organized at the module level: every module is a feature, `Endpoints/<Resource>/` groups API operations by aggregate, and `Pages/<Section>/{*.tsx,*Endpoint.cs}` co-locates IViewEndpoints with their React components. However, three soft spots remain:

1. **Type-based folders inside modules** — `Services/`, `EntityConfigurations/`, `Channels/`, `Jobs/`, and a sprinkling of cross-cutting files at module root (e.g. Users has 17 `*.cs` files in the project root: services + DbContext + module class + permissions + options).
2. **Operation logic is split between the endpoint file and a horizontal service.** Endpoints are skinny shims that call into a contract-implementing service (e.g. `UserAdminService.CreateUserAsync`). The endpoint, its DTO, its validator (when present), and the actual implementation method live in three or four different folders.
3. **Inconsistent validator placement.** Email puts FluentValidation validators in `Validators/` at module root; RateLimiting and Tenants already co-locate validators with their endpoints (`Endpoints/Policies/CreateRequestValidator.cs`). The codebase is drifting toward co-location organically; no rule guides it.

The goal: organize each module so that **everything needed to read, modify, or test one operation lives in a single folder**, while honoring the framework's hard constraints (one contract → one implementation class per SM0025/SM0026; one endpoint per file per SM0049; entity classes in Contracts assemblies per SM0055).

## Non-Goals

- Changing the source generator or its diagnostics.
- Replacing the IEndpoint/IViewEndpoint abstractions with MediatR or similar.
- Restructuring the host app (`template/SimpleModule.Host`) or the frontend packages (`packages/`).
- Moving Pages/ (the view-side `IViewEndpoint` + React file pair) — they are already vertical and Vite's entry point binds to `Pages/index.ts`. Cost > benefit.
- Cross-module reorganization. Modules remain independent units of work.
- Adding new SM diagnostics to enforce the convention. Soft convention to start; we can add a diagnostic later if we see drift.

## Approach

A "feature folder" is the smallest unit that fully describes one API operation. For an operation `<Op>` on aggregate `<Agg>` in module `<Mod>`, the canonical contents are:

| File | Role | Required? |
|---|---|---|
| `<Op>Endpoint.cs` | The `IEndpoint` implementation (route + handler) | yes |
| `<Op>Request.cs` (in Contracts) | Request DTO consumed by the endpoint | yes if the operation accepts a body |
| `<Op>Validator.cs` | FluentValidation rules for the request | required if the operation already has a validator today; optional for new operations (add when validation rules exceed what data annotations naturally express) |
| `<Service>.<Op>.cs` | Partial-class fragment with the method body (e.g. `UserAdminService.Create.cs` declaring `CreateUserAsync`) | yes, one fragment per service the operation extends — see C2 for the multi-service case |

These pieces sit in `Features/<Agg>/<Op>/` on the impl side, mirrored by `Contracts/Features/<Agg>/<Op>/<Op>Request.cs` on the contracts side.

The cross-module contract implementation (e.g. `UserAdminService`) becomes a **partial class** whose root fragment lives in `Infrastructure/<Service>.cs` (constructor, fields, helpers shared across operations) and whose per-operation method bodies live next to their endpoints. The class remains a single type at runtime, so SM0025 (one implementation per contract) and SM0026 (no duplicate impls) stay green.

### Reference layout: Users module

```
modules/Users/
├── src/
│   ├── SimpleModule.Users.Contracts/
│   │   ├── Features/
│   │   │   ├── Users/
│   │   │   │   ├── Create/CreateUserRequest.cs
│   │   │   │   ├── Update/UpdateUserRequest.cs
│   │   │   │   └── Delete/  (no request body)
│   │   │   ├── AdminUsers/
│   │   │   │   ├── Create/CreateAdminUserRequest.cs
│   │   │   │   └── Update/UpdateAdminUserRequest.cs
│   │   │   └── Account/
│   │   ├── Events/                      # unchanged — cross-module events
│   │   │   ├── UserCreatedEvent.cs
│   │   │   └── ...
│   │   ├── Shared/                      # DTOs/types used by ≥2 features OR another module
│   │   │   ├── UserDto.cs
│   │   │   ├── AdminUserDto.cs
│   │   │   ├── RoleDto.cs
│   │   │   ├── UserId.cs
│   │   │   ├── UsersConstants.cs
│   │   │   └── Constants/
│   │   │       ├── ConfigKeys.cs
│   │   │       ├── PersonalDataKeys.cs
│   │   │       └── SeedConstants.cs
│   │   ├── ApplicationUser.cs           # EF entities stay at root (SM0055)
│   │   ├── ApplicationRole.cs
│   │   ├── IUserContracts.cs            # contract interfaces at root
│   │   ├── IUserAdminContracts.cs
│   │   ├── IRoleAdminContracts.cs
│   │   └── IAccountUnlockEmailSender.cs
│   │
│   └── SimpleModule.Users/
│       ├── Features/
│       │   ├── Users/
│       │   │   ├── Create/
│       │   │   │   ├── CreateEndpoint.cs
│       │   │   │   ├── CreateUserValidator.cs
│       │   │   │   └── UserAdminService.Create.cs
│       │   │   ├── Update/
│       │   │   │   ├── UpdateEndpoint.cs
│       │   │   │   ├── UpdateUserValidator.cs
│       │   │   │   └── UserAdminService.Update.cs
│       │   │   ├── Delete/
│       │   │   │   ├── DeleteEndpoint.cs
│       │   │   │   └── UserAdminService.Delete.cs
│       │   │   ├── GetAll/
│       │   │   │   ├── GetAllEndpoint.cs
│       │   │   │   └── UserAdminService.GetAll.cs
│       │   │   ├── GetById/
│       │   │   │   ├── GetByIdEndpoint.cs
│       │   │   │   └── UserAdminService.GetById.cs
│       │   │   ├── GetCurrent/
│       │   │   │   ├── GetCurrentEndpoint.cs
│       │   │   │   └── UserService.GetCurrent.cs
│       │   │   └── DownloadPersonalData/
│       │   │       ├── DownloadPersonalDataEndpoint.cs
│       │   │       └── UserService.DownloadPersonalData.cs
│       │   ├── Passkeys/
│       │   │   ├── Get/GetPasskeysEndpoint.cs + UserService.GetPasskeys.cs
│       │   │   ├── Delete/DeletePasskeyEndpoint.cs + ...
│       │   │   ├── LoginBegin/...
│       │   │   ├── LoginComplete/...
│       │   │   ├── RegisterBegin/...
│       │   │   ├── RegisterComplete/...
│       │   │   └── PasskeyHelpers.cs       # shared inside the Passkeys vertical
│       │   └── Account/
│       │       └── Security/
│       │           └── AccountSecurityEndpoint.cs
│       ├── Pages/                          # UNCHANGED — IViewEndpoint + React stay here
│       │   ├── Account/
│       │   │   ├── LoginEndpoint.cs + Login.tsx
│       │   │   ├── Manage/...
│       │   │   └── ...
│       │   └── index.ts                    # registry
│       ├── Infrastructure/
│       │   ├── UsersDbContext.cs
│       │   ├── UserAdminService.cs         # partial root (ctor, fields, shared helpers)
│       │   ├── UserService.cs              # same pattern
│       │   ├── RoleAdminService.cs
│       │   ├── ApplyUsersModuleOptions.cs
│       │   ├── ApplySecurityStampValidatorOptions.cs
│       │   └── Services/
│       │       ├── UserSeedService.cs
│       │       ├── ConsoleEmailSender.cs
│       │       └── ConsoleAccountUnlockEmailSender.cs
│       ├── components/                     # shared React (unchanged)
│       ├── Locales/keys.ts                 # unchanged
│       ├── types.ts                        # unchanged
│       ├── vite.config.ts                  # unchanged
│       ├── UsersModule.cs                  # IModule at root
│       ├── UsersModuleOptions.cs
│       └── UsersPermissions.cs             # IModulePermissions at root
│
└── tests/
    └── SimpleModule.Users.Tests/
        ├── Features/                       # mirrors impl tree
        │   ├── Users/Create/CreateUserTests.cs
        │   ├── Users/Update/UpdateUserTests.cs
        │   └── Passkeys/...
        ├── Unit/                           # cross-cutting (UserIdTests, etc.)
        └── Integration/                    # cross-feature flows (e.g. AccountUnlock end-to-end)
```

### Conventions

**C1. Folder = namespace.** A file at `Features/Users/Create/CreateEndpoint.cs` declares `namespace SimpleModule.Users.Features.Users.Create;`. This matches the existing project convention (folders map to namespaces) so no `.editorconfig` rules change.

**C2. Partial-class split.** Each service that implements a cross-module contract is a `public sealed partial class`. The root partial lives in `Infrastructure/` and owns the constructor, private fields, and any helper methods called by ≥2 operations. Each `<Service>.<Op>.cs` fragment under a feature folder declares the same partial and adds **only** the public method for that operation (and any private helpers used by that operation alone). Field declarations and constructors live in **exactly one** fragment — the root.

**Multi-service case:** if one feature legitimately touches two services (e.g. `Users/Create/` calls both `UserAdminService` and an event-emitting helper on `UserService`), the feature folder holds a fragment per service: `Users/Create/UserAdminService.Create.cs` *and* `Users/Create/UserService.OnCreate.cs`. Keep this rare — usually the operation belongs to a single service, and cross-service orchestration happens via DI inside the endpoint or via events.

**C3. Shared vs feature DTO rule.** A request DTO lives in its feature folder until a second consumer (another feature or another module) appears. At that point it moves to `Contracts/Shared/`. Heuristic at refactor time: if the file is referenced by exactly one endpoint, it's a feature DTO; otherwise it's shared.

**C4. Pages/ is intentionally untouched.** View endpoints and their React companions stay in `Pages/<Section>/{*Endpoint.cs,*.tsx}`. Moving them would require rewriting `vite.config.ts` entry paths and every `Pages/index.ts` dynamic import, with no readability gain since they're already co-located.

**C5. Tests mirror impl folders.** `tests/<Mod>.Tests/Features/<Agg>/<Op>/<Op>Tests.cs`. Tests remain in their own project (production assemblies stay xUnit-free). `Unit/` and `Integration/` keep their roles for cross-feature concerns.

**C6. Validators stay co-located.** FluentValidation `AbstractValidator<TRequest>` files sit in the feature folder, named `<Op><Resource>Validator.cs` to match the request type they validate. Modules that currently park validators in a top-level `Validators/` folder (Email today) move them to feature folders during migration.

**C7. Module-level files stay at impl-project root.** `<Mod>Module.cs` (IModule), `<Mod>ModuleOptions.cs`, `<Mod>Permissions.cs`, `types.ts`, `vite.config.ts`. These are *not* feature-specific.

**C8. EF entity configurations go to `Infrastructure/EntityConfigurations/`.** They map persistence shape, not feature behavior. They stay grouped because the DbContext references all of them at once during `OnModelCreating`.

## Migration plan

**Phase 0 — design & tooling (this spec + small follow-ups).**
- Land this design doc.
- Specify a migration script: `git mv` per file + `sed`-style namespace rewrite for the moved files. Use the script to keep history (git follows renames automatically). Optionally `dotnet format` after each phase.

**Phase 1 — Notifications pilot.** Notifications is the smallest non-trivial module: 4 API endpoints (`ListNotifications`, `MarkAllRead`, `MarkRead`, `UnreadCount`), 1 view page (`Inbox`), 1 service (`NotificationService`) plus helpers (`NotificationsLog`, `Notifier`), 1 channel registry, 1 background job, 1 DbContext. Convert end-to-end. Validates:
- Source generator endpoint/permission/contract discovery survives the folder move (we expect yes — the generator works off types and assemblies, not folders, per CONSTITUTION.md §11).
- Partial-class split compiles and `SM0025`/`SM0026` stay green.
- `Pages/index.ts`, `npm run validate-pages`, and Vite entry points are untouched and still pass.
- Test discovery and CI (`ci` skill / GitHub Actions) pass after the rename.

**Phase 2 — Settings.** Medium module with 3 resource groups (`Menus`, `Settings`, `UserSettings`), shared services (`SettingsService`, `PublicMenuService`), and 3 view pages. Confirms the pattern scales beyond trivial.

**Phase 3 — propagate.** One module per PR, dependency-leaf-first to limit blast radius if anything breaks:
1. AuditLogs
2. FeatureFlags
3. Localization
4. FileStorage
5. Email (consolidate the `Validators/` folder into features)
6. BackgroundJobs
7. Permissions
8. OpenIddict
9. Tenants
10. RateLimiting
11. Dashboard
12. Admin
13. Users (largest; last to absorb lessons from earlier conversions)

**Phase 4 — tooling and docs.**
- Update the `sm new module` and `sm new feature` CLI scaffolds to emit the new shape.
- Add a section to `docs/CONSTITUTION.md` §6 (or a new §6.1) describing the feature-folder convention.
- Consider but defer: a soft `Info`-level SM diagnostic that flags new code added outside `Features/`.

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| Source generator misses types after a folder move | Phase 1 pilot is the smoke test. Run `dotnet build` and verify generated `MapModuleEndpoints` still lists every endpoint. |
| Partial-class fragment forgets `using` for a type the root partial uses | Compiler error; quick fix. Each fragment is self-contained. |
| Method body uses a private helper defined in a different fragment's same partial | Allowed in C# (privates are visible across all partials of the same type in the same assembly). |
| Inconsistent validator placement across modules (Email under `Validators/`, Tenants/RateLimiting already co-locate next to endpoints) | The pattern already works in the co-locating modules; migration consolidates everyone on it. Email's `Validators/` folder is dissolved into feature folders during Phase 3. |
| Inconsistent operation naming (e.g. `Create` vs `CreateUser` vs `Add`) | Migration script preserves existing names; we don't rename operations mid-flight. Convention for *new* features: verb-noun (`CreateUser`, `MarkRead`). |
| Tests that span multiple endpoints (e.g. `UsersEndpointTests.cs`) | Decide per file at migration time. If the file tests one cohesive use case, keep as-is in `Integration/`. If it bundles unrelated tests, split during the Phase. |
| Phase 1 reveals the pattern doesn't work | Pilot is one module; rollback is one revert. |

## Open questions (resolved during plan-writing)

- **Operation folder naming**: PascalCase verb-only (`Create/`) vs verb+noun (`CreateUser/`)? Decision: **PascalCase verb-only** when the aggregate is already implied by the parent folder (`Features/Users/Create/`); verb+noun only when no aggregate group exists.
- **Should each operation always get a folder, or only when ≥2 files belong together?** Decision: **always a folder**. Even a one-file operation (`Delete/DeleteEndpoint.cs`) gets its own folder for consistency and to make adding a validator later a non-event.
- **Migration script vs manual moves**: write a small bash/PowerShell script that takes a manifest of moves and applies `git mv` + namespace rewrite. Targeted in the implementation plan.

## Success criteria

1. Every module's source code physically groups by feature, with module-root files limited to `<Mod>Module.cs`, `<Mod>ModuleOptions.cs`, `<Mod>Permissions.cs`, and frontend config (`types.ts`, `vite.config.ts`).
2. `dotnet build` is clean across the solution after each Phase. `npm run check`, `npm run build`, `npm run validate-pages`, and `dotnet test` all pass.
3. No new SM diagnostics fire; no existing diagnostics are suppressed.
4. The `sm new feature` CLI generates the new shape for new operations.
5. CONSTITUTION.md documents the convention.
