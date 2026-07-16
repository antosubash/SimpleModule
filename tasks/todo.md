# Framework review fixes (2026-07-16)

Fixes from the July 2026 5-area framework review. The review initially ran against a
stale local main (12 behind origin/main); every item below was re-verified against
origin/main. Already fixed upstream (dropped): showErrorToast, validate-pages path,
ClientApp typecheck, ForwardedHeaders config, download-link click guard.

Deferred (recorded, not dropped): generator pipeline split onto
MetadataReferencesProvider / ForAttributeWithMetadataName (upstream todo already
tracks it), @simplemodule/ui shared-chunk vendoring, generated-literal escaping
helper, test-claim `;` escaping.

## High
- [x] 1. FeatureFlags endpoint gate reads NameIdentifier only — use sub-aware helper (Core/FeatureFlags/EndpointFeatureFlagExtensions.cs)
- [x] 2. `?v=` query alone applies 1-year public+immutable cache header to any response incl. authed HTML — gate on static-asset path (Hosting/SimpleModuleHostExtensions.Helpers.cs)
- [x] 3. LocalStorageProvider: ListAsync bypasses traversal guard; GetFullPath boundary check lacks separator + uses OrdinalIgnoreCase (Storage.Local)
- [x] 4. Multi-tenant filter bakes scoped ITenantContext into cached EF model (first tenant frozen forever); null-tenant coalesces to "" matching empty-tenant rows (Database/ModuleModelBuilderExtensions.cs)
- [~] 6. Generator scans every referenced assembly (BCL, ASP.NET) on each compile; [FormRequest] scanned twice (Generator/Discovery/SymbolDiscovery.cs) — IN PROGRESS (subagent)

## Medium
- [x] 7. InertiaResult.MergeProps double-serializes props on every shared-data render (Core/Inertia/InertiaResult.cs)
- [x] 8. InertiaLayoutDataMiddleware does antiforgery crypto + menu work for static assets/probes/APIs — gate to Inertia/HTML requests (Hosting/Middleware)
- [~] 10. Generated JSON resolver uncompilable for init-only props / missing parameterless ctor (Generator DtoPropertyExtractor + JsonResolverEmitter) — IN PROGRESS (subagent)
- [x] 11. EntityInterceptor/EntityChangeInterceptor only override async SaveChanges — sync path hard-deletes soft-delete entities silently (Database/Interceptors)
- [x] 12. vite-plugin-vendor skips rebuild when outputs exist — stale React after version bump (packages/SimpleModule.Client)

## Low
- [x] 13. PermissionMatcher wildcard allocates substring per check — span it
- [x] 14. GlobalExceptionHandler maps ArgumentNullException to client 400 at Warning
- [x] 15. InertiaMiddleware.Version throws on single-file publish (empty Assembly.Location)
- [~] 16. SymbolHelpers module-namespace prefix match lacks trailing-dot boundary — IN PROGRESS (subagent)
- [~] 17. ViewPagesEmitter hint name collides for same-named module classes in different namespaces — IN PROGRESS (subagent)
- [x] 18. SoftDeleteService.CoerceId: Convert.ChangeType throws for Guid keys
- [x] 19. Maintenance-gate comment claims static assets are spared — they aren't
- [x] 20. PermissionRegistryBuilder O(n²) list.Contains dedup
- [x] 21. app.tsx click interceptor: hash-only anchors re-request page instead of scrolling
- [x] 22. add-component.mjs UI path wrong for this repo (src/SimpleModule.UI vs packages/)
- [x] 23. typecheck.mjs unbounded tsc parallelism

## Verification
- [x] Regression tests added: cross-tenant isolation across shared model (#4), sub-claim GetUserId/GetRoles (#1), wildcard module-boundary (#13), ViewPages same-name collision (#17)
- [x] dotnet build — succeeded, 0 errors (warnings-as-errors)
- [x] dotnet test — Core 280, Database 94, Generator 220, DevTools 35 all pass; CLI exit 0
- [x] npm checks — biome clean, validate-pages (74 endpoints/577 files), framework-scope, i18n, typecheck 15/15
- [x] Review section (below)

## Review

All 22 findings addressed except the two strategic rewrites intentionally deferred
(generator pipeline split onto MetadataReferencesProvider; @simplemodule/ui shared-chunk
vendoring) plus a few defense-in-depth notes. Generator fixes (#6/#10/#16/#17) done by
subagent; verified by full generator test suite + new collision test.

Notable while fixing:
- The tenant-filter fix (#4) required a design change, not a patch: EF caches the model
  per context type, so capturing a scoped ITenantContext as an Expression.Constant froze
  the first request's tenant. Fixed by referencing the *executing* DbContext instance via
  a new `ITenantScopedDbContext.CurrentTenantId` — EF re-evaluates the context reference
  per query. New regression test (two contexts, one shared cached model + DB, different
  tenants) proves isolation and would fail under the old code. API change is safe: zero
  production callers today.
- Same sub-claim bug as #1 also lived in EntityInterceptor's audit stamping
  (ClaimTypes.NameIdentifier → GetUserId()); fixed alongside — CreatedBy/UpdatedBy would
  have been null under Keycloak.
- Sync SaveChanges gap (#11) fixed on BOTH interceptors; EntityChangeInterceptor's sync
  dispatch blocks on the async path (safe — ASP.NET Core has no SynchronizationContext).
- InertiaResult merge (#7): a correct flat merge needs props' keys, so props is still
  materialized once, but the extra Dictionary + full re-serialize is gone — now one
  Utf8JsonWriter pass, props win on collision, no duplicate keys. Integration tests
  (InertiaResultTests) confirm the JSON envelope is unchanged.
- Layout middleware gate (#8) keys on X-Inertia OR Accept: text/html — real browsers and
  Inertia nav both qualify; static-asset/probe/JSON requests skip the antiforgery crypto.

Deferred / documented (not silently dropped):
- Generator pipeline decomposition (metadata vs source providers) — upstream todo already
  tracks it; assembly-prefix filter is the cheap high-value win applied now.
- @simplemodule/ui bundled per module — payload-size only, correctness verified fine.
- LocalStorage traversal (#3) & CoerceId Guid (#18) verified by inspection + build; no
  dedicated storage test project exists (adding one needs slnx + Dockerfile wiring).
