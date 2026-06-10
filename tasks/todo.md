# Issue #162 — Policy classes for entity-level authorization — DONE

Laravel-style `IPolicy<TResource>` + `IAuthorizer` layered over string permissions.

## Review

All phases complete. Verification: full `dotnet build` clean; Core.Tests 259 ✓,
Generator.Tests 211 ✓ (incl. 5 new policy tests + catalog baseline), Database 93 ✓,
DevTools 35 ✓, all 15 module suites ✓ (Notifications 23 incl. 4 new e2e policy tests);
`npx biome check` clean on touched files; `npm run validate-pages` ✓. Generated
`AddModules()` verified to contain `AddScoped<IPolicy<Notification>, NotificationPolicy>()`.

Notable decisions:
- Reference module is **Notifications** (issue suggested Products, which no longer exists).
- Missing policy at check time throws `MissingPolicyException` (fail closed).
- Deny→404 mapping via `PolicyAuthorizationOptions.NotFoundActions` (default `view`);
  Notifications adds `markRead` to preserve its previous non-owner-404 behavior.
- Declarative `.AuthorizeResource<T>()` ships with `IResourceResolver<T>` (documented,
  not yet used by a module — imperative `IAuthorizer` is the primary path).

## Code-review fix round (all 10 findings addressed)

1. Unscoped contract surface → `INotificationsContracts` restored to owner-scoped
   `MarkReadAsync(id, userId)`; unscoped `FindAsync` moved to module-internal
   `INotificationStore`. Also restores 404-on-race (bool result back).
2. Internal policies silently skipped → SM0059 (Error: policy must be public).
3. SM0058 false positive for `[NoDtoGeneration]`/`IEvent` contracts entities →
   resource classified symbolically at discovery ([Dto] OR .Contracts assembly).
4. Discovery scope → PolicyFinder now scans contracts assemblies and nested types.
5. Global NotFoundActions mutation → `AuthorizationResult.DenyAsNotFound()` per
   decision; NotificationsModule no longer touches host options.
6. Worker host asymmetry → `AddSimpleModuleWorker` registers IAuthorizer + options.
7. Duplicate-registration double execution → generated code uses `TryAddEnumerable`.
8. Admin behavior widening → NotificationPolicy is owner-only again (admins not
   exempt from instance rules; documented as deliberate).
9. AuthorizeResource misconfig → missing route value throws InvalidOperationException;
   6 new TestServer-based filter tests (allow/deny/hide/missing/misnamed/no-resolver).
10. Foreign-module policies → SM0060 (Error: policy owned by resource's module).

## Plan

### Phase 1 — Core types (`framework/SimpleModule.Core/Authorization/Policies/`)
- [x] `AuthorizationResult` — Allowed/Reason, `Allow()` / `Deny(reason)`
- [x] `IPolicy<TResource>` — `AuthorizeAsync(ClaimsPrincipal, string action, TResource, CancellationToken)`
- [x] `PolicyActions` — common action constants (View/Create/Update/Delete)
- [x] `IAuthorizer` — `CheckAsync<T>` (result) + `AuthorizeAsync<T>` (throws)
- [x] `Authorizer` — resolves all `IPolicy<T>` from DI; deny wins; missing policy → `MissingPolicyException` (fail closed)
- [x] `PolicyAuthorizationOptions` — `NotFoundActions` (default: view) map deny → `NotFoundException` instead of `ForbiddenException`
- [x] `IResourceResolver<TResource>` + `AuthorizeResource<TResource>(action, routeParam)` endpoint filter
- [x] Register `IAuthorizer` in `SimpleModule.Hosting.AddSimpleModuleInfrastructure`

### Phase 2 — Source generator
- [x] `CoreSymbols`: resolve `IPolicy`1`
- [x] `PolicyFinder` + `PolicyInfo`/`PolicyRecord`, wire into `SymbolDiscovery` / `DiscoveryData` / `DiscoveryDataBuilder`
- [x] `ModuleExtensionsEmitter`: emit `services.AddScoped<IPolicy<X>, XPolicy>()`
- [x] SM0058 diagnostic — policy resource type must be a contracts DTO ([Dto]/convention)
- [x] CONSTITUTION diagnostics table + AnalyzerReleases.Unshipped.md

### Phase 3 — Tests
- [x] `tests/SimpleModule.Core.Tests/Authorization/AuthorizerTests.cs` — resolution, deny reason, missing policy, multi-policy precedence, 404 mapping
- [x] `tests/SimpleModule.Generator.Tests/PolicyAutoDiscoveryTests.cs` — registration emitted, SM0058 reported
- [x] Notifications policy unit tests

### Phase 4 — Reference refactor (Notifications)
- [x] `NotificationPolicy : IPolicy<Notification>` (owner or admin)
- [x] Refactor `MarkReadEndpoint` to load → authorize → act via `IAuthorizer`

### Phase 5 — Docs
- [x] `docs/site/guide/policies.md` + VitePress sidebar entry
- [x] CONSTITUTION.md authorization section update

### Phase 6 — Verify & ship
- [x] `dotnet build` clean (TreatWarningsAsErrors)
- [x] `dotnet test` — Core, Generator, Notifications
- [x] Commit, push branch, open PR closing #162

## Notes
- Products/Orders modules no longer exist (CLAUDE.md stale) → Notifications is the reference module
- Highest existing diagnostic: SM0057 → new one is SM0058
