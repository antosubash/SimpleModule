# Fix critical issues from framework review (2026-06-09)

## Critical 1 — page-registry guard broken on both ends
- [x] Fix `validate-pages.mjs` path: scan `modules/*/src/*/` (real layout is `src/SimpleModule.<Name>`), skip `obj`/`bin`
- [x] Add self-check: fail when zero C# files or zero view endpoints are found repo-wide (path drift can never silently disable the guard again)
- [x] Fix `app.tsx:232` `showErrorToast(...)` → `showToast({ variant: 'error', ... })` (ReferenceError on failed page load)
- [x] Add `ClientApp/tsconfig.json` and include ClientApp in `scripts/typecheck.mjs`

## Critical 2 — default deployment one request from admin token
- [x] `UserSeedService`: fail fast in non-Development when `Seed:AdminPassword` unset; never seed test user with default password outside Development
- [x] OpenIddict: refuse `AllowPasswordFlow` in Production; fail fast on ephemeral signing/encryption keys in Production
- [x] `SimpleModuleHostExtensions`: stop clearing `KnownProxies`/`KnownIPNetworks` unconditionally — config-driven (`ForwardedHeaders:KnownProxies`/`KnownNetworks`/`TrustAllProxies`)
- [x] `docker-compose.yml`: explicit `OpenIddict__AllowPasswordGrant: "false"`, required seed-password env vars
- [x] Enforce `FileStorageModuleOptions` (MaxFileSizeMb / AllowedExtensions) in UploadEndpoint

## Critical 3 — remaining #242-class DbContext races
- [x] `AdminService.GetAdminOverviewAsync` — sequential awaits
- [x] `Admin/Pages/Admin/UsersEditEndpoint` — sequential awaits
- [x] `TenantFeatureHelper.GetOverridesForTenantAsync` — sequential awaits (throws with 2+ active flags)

## Outbox gaps (events lost on crash after SaveChanges)
- [x] `TenantService.CreateTenantAsync` — use `IDbContextOutbox` like UpdateTenantAsync already does
- [x] `FileStorageService.UploadFileAsync` — same

## Critical 4 — docs describe phantom modules
- [x] CLAUDE.md: load-test section lists 11 scenarios incl. Products/Orders/Marketplace/PageBuilder — only 6 exist (Admin, AuditLogs, FeatureFlags, FileStorage, Settings, Users)
- [x] CLAUDE.md: `modules/Products/src/Products` examples use wrong layout (`src/SimpleModule.<Name>`) — likely origin of the validate-pages bug
- [x] Sweep docs/CONSTITUTION.md + skills for phantom-module/wrong-layout references
- (untracked WIP dirs in the main checkout are stale build artifacts — left alone)

## CI gaps
- [ ] PostgreSQL test leg — DEFERRED: `SimpleModuleWebApplicationFactory` is deeply SQLite-coupled (shared in-memory connection, static env bootstrap, Wolverine SQLite file); a reliable dual-provider factory is its own change. Docs no longer claim it exists.
- [x] CodeQL workflow
- [x] Dependabot config (nuget, npm, github-actions)
- [x] `dotnet list package --vulnerable` CI step

## Code-review round 1 — fixes applied
- [x] ForwardedHeaders KnownProxies/KnownNetworks: accept comma-separated scalar (the form the docker-compose comment documents) as well as arrays; TryParse with a clear error instead of an opaque FormatException
- [x] docker-compose worker: add Seed__AdminPassword/Seed__UserPassword — the worker runs UserSeedService too and would otherwise race-seed the default admin password
- [x] FileStorageService.UploadFileAsync: scope blob-rollback to the pre-commit window so a post-commit outbox-flush failure can't dangle a committed row against a deleted blob
- [x] Env-predicate consistency: shared HostEnvironmentExtensions.IsLocalOrTest (Development+Testing) used by both UserSeedService and OpenIddictProductionGuard — closes the Staging bypass and the Testing-startup-crash in one predicate
- [x] UsersEditEndpoint: reverted to Task.WhenAll — the three contracts use distinct DbContexts (Users/Permissions/OpenIddict), so there was no race; corrected the misleading comment
- [x] ConfigKeys.OpenIddictAllowPasswordGrant constant — replaced the three hardcoded "OpenIddict:AllowPasswordGrant" string literals (drift would silently disable the guard)
- [x] validate-pages: detect interpolated Inertia.Render($"…") as unresolved instead of silently skipping it
- [x] validate-i18n: same fail-on-zero self-check as validate-pages (locale dirs exist today; zero = path drift)
- [x] UploadEndpoint: hoist size/extension parse to Map() time (resolve IOptions once, capture) instead of allocating a HashSet per request

## Deferred (follow-ups — recorded, not silently dropped)
- **Roslyn diagnostic (SM0060)** for Task.WhenAll-over-shared-DbContext and SaveChanges+Publish-without-outbox — durable fix for the recurring race/outbox classes (fixed by hand 3× now). Analyzer-grade flow analysis; separate PR. An interim regex guard like validate-framework-scope.mjs is a cheaper stopgap.
- **Shared outbox helper** (`SaveAndPublishAsync<TDb>`) in SimpleModule.Database to encode the BeginTransaction→SaveChanges→Publish→flush ritual once — currently duplicated across TenantService/FileStorageService. Also: WolverineConfiguration wires PublishDomainEventsFromEntityFrameworkCore<IHasDomainEvents> but no entity implements it (dead idiom) — reconcile.
- **Framework production-config validation abstraction** (IValidateOptions/ValidateOnStart) — OpenIddictProductionGuard + UserSeedService are two ad-hoc startup guards; a shared mechanism is the right altitude and fixes implicit guard ordering.
- **Module options ↔ Settings store / appsettings binding**: generated RegisterModuleOptionsDefaults only calls AddOptions<T>() with no config binding, so the FileStorage admin Settings/appsettings keys don't drive the enforced IOptions values (enforcement honors code-configured/default values only).
- **TenantFeatureHelper N+1**: serial GetOverridesAsync per flag; needs a bulk IFeatureFlagContracts method to collapse to one query.
- **ForwardedHeaders typed options** on SimpleModuleOptions (typo'd keys currently silently yield loopback-only trust).
- Generator decomposition to ForAttributeWithMetadataName.
- @simplemodule/ui vendoring + stale wwwroot chunk cleanup.

## Review

All four criticals addressed, plus the concrete improvements. Verified with:
`dotnet build` (0 warnings/errors), `dotnet test` (19/19 assemblies, 0 failures),
`npm run check` (biome + validate-pages + i18n + framework-scope + typecheck 14/14),
`npm run build` (production bundles), plus a negative test proving validate-pages
now fails when a page registration is removed.

Notable finds while fixing:
- The resurrected guard immediately caught that `UnlockAccountEndpoint` renders via a
  `const string ComponentName` — the script now resolves same-file string consts and
  reports unresolvable `Inertia.Render` arguments as failures.
- The new ClientApp typecheck exposed two latent runtime bugs beyond the reported
  `showErrorToast`: `router.on('exception', ...)` is not an Inertia v3 event (the
  network-error toast was dead code — now `networkError`), and the page resolver
  returned a module where Inertia's types want the component.
- `FileStorageService.DeleteFileAsync` had the same lost-event bug as Upload (worse:
  a failed blob delete also skipped the event after the DB commit) — fixed via outbox.
- Create flows with DB-generated ids (Tenant, StoredFile) use an explicit transaction +
  `SaveChangesAndFlushMessagesAsync`, which per Wolverine commits the open transaction
  before flushing. `FakeDbContextOutbox` now mirrors that commit behavior.

