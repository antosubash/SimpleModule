# Codebase Concerns

**Analysis Date:** 2026-03-18

## Tech Debt

**Permission System Admin Bypass Hard-Coded:**
- Issue: Admin role bypass is encoded directly in `PermissionAuthorizationHandler`, checking for hardcoded role name "Admin"
- Files: `framework/SimpleModule.Core/Authorization/PermissionAuthorizationHandler.cs:12-15`
- Impact: Cannot configure or disable admin bypass without code changes. No audit trail for why access succeeded. Admin role name is not configurable.
- Fix approach: Extract role bypass to `AuthorizationOptions` in Core. Inject configuration into handler. Log bypass attempts for security auditing.

**Permission Claims Projection Missing Validation:**
- Issue: `AuthorizationEndpoint` loads permissions for all roles without validating role existence or permission format
- Files: `modules/Users/src/Users/Endpoints/Connect/AuthorizationEndpoint.cs:85-113`
- Impact: Invalid permissions in database could cause token bloat or claim pollution. No validation that permissions are registered in `PermissionRegistry`.
- Fix approach: Validate loaded permissions against registered permissions in `PermissionRegistry`. Log mismatches. Filter invalid permissions before adding to claims.

**PermissionSeedService Silent Failure:**
- Issue: Service silently returns if Admin role doesn't exist instead of failing or logging warning
- Files: `modules/Users/src/Users/Services/PermissionSeedService.cs:24-26`
- Impact: Permissions may not be seeded if Admin role initialization is delayed or fails. No indication to operator that seeding was skipped.
- Fix approach: Add explicit warning log if Admin role not found. Consider dependency on OpenIddictSeedService to ensure role exists first. Verify in integration tests.

**CLI Feature Templates Contain TODO Placeholders:**
- Issue: `sm new feature` command generates endpoint and validator templates with `// TODO: implement` placeholders
- Files: `cli/SimpleModule.Cli/Templates/FeatureTemplates.cs:137,162,253,282,342,368`
- Impact: Generated code doesn't compile or run without manual editing. Developer experience gap in scaffolding.
- Fix approach: Complete template implementations. Add test generation. Consider AST-based scaffolding instead of string templates.

**Large Generator Discovery File:**
- Issue: `SymbolDiscovery.cs` is 711 lines with multiple responsibilities (modules, endpoints, permissions, DbContexts, DTOs, components)
- Files: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`
- Impact: Difficult to extend or debug. Single point of failure for all symbol discovery. Permission discovery logic is mixed with other discovery concerns.
- Fix approach: Extract permission discovery into `PermissionDiscovery.cs`. Create separate discoverers for each concern (IEndpointDiscovery, IPermissionDiscovery, IDbContextDiscovery).

---

## Known Bugs

**PermissionSeedService Race Condition:**
- Symptoms: Concurrent requests during startup could add duplicate permission entries
- Files: `modules/Users/src/Users/Services/PermissionSeedService.cs:28-52`
- Trigger: Multiple app instances starting simultaneously or rapid requests during warmup
- Workaround: Ensure only one app instance initializes database on startup. Use database-level unique constraint on (RoleId, Permission).

**Test Claims Parsing Fragile:**
- Symptoms: Test authentication fails if claim values contain semicolons or equals signs
- Files: `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs:135-142`
- Trigger: Create test with claim value `"semicolon;value"` or `"key=value"`
- Workaround: URL-encode or escape special characters in X-Test-Claims header. Better: use base64 encoding or JSON payload instead of semicolon delimiters.

---

## Security Considerations

**Hard-Coded Admin Role Bypass:**
- Risk: Admin role bypass cannot be audited, disabled, or made configurable. Violates principle of least privilege.
- Files: `framework/SimpleModule.Core/Authorization/PermissionAuthorizationHandler.cs:13`
- Current mitigation: Admin role requires authentication. No guest access.
- Recommendations: Add configurable bypass policies. Log all bypasses. Consider tiered admin roles (SuperAdmin, Admin, Manager).

**Permission Claims Not Validated at Token Issuance:**
- Risk: Malformed or non-existent permissions can be injected into JWT tokens
- Files: `modules/Users/src/Users/Endpoints/Connect/AuthorizationEndpoint.cs:93-113`
- Current mitigation: Permissions stored in database only. No external sources.
- Recommendations: Validate all permissions against registered registry before adding to token. Use signed JWTs (already done via OpenIddict). Add claim validation middleware.

**Test Factory Bypasses OpenIddict Validation:**
- Risk: Test auth scheme disabled OpenIddict validation entirely, masking real authorization bugs
- Files: `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs:38-45`
- Current mitigation: Only used in tests, not production. Tests do validate permission requirements.
- Recommendations: Create integration tests that use real OpenIddict flow (slower but more realistic). Keep fast test harness for unit tests.

**No Rate Limiting on OAuth2 Endpoints:**
- Risk: Brute-force attacks on login and token endpoints unprotected
- Files: `modules/Users/src/Users/Endpoints/Connect/` (all endpoints)
- Current mitigation: None
- Recommendations: Add rate limiting to `/connect/token` and `/connect/authorize` per IP/username. Use ASP.NET Core rate limiting.

**No CSRF Protection on View Endpoints:**
- Risk: State-changing views (create, update, delete) may be vulnerable to CSRF
- Files: All module view endpoints in `IViewEndpoint` implementations
- Current mitigation: Inertia middleware may provide some protection via re-render headers
- Recommendations: Validate Inertia CSRF token. Add explicit antiforgery token validation. Document Inertia security model.

---

## Performance Bottlenecks

**Synchronous Permission Loading at Token Issuance:**
- Problem: Each login loads role permissions from database sequentially
- Files: `modules/Users/src/Users/Endpoints/Connect/AuthorizationEndpoint.cs:88-96`
- Cause: Three sequential database queries (roles, role permissions, user permissions)
- Improvement path: Consolidate into single query using `JOIN`. Cache user roles/permissions (invalidate on role/permission update). Consider Redis caching.

**Entire Permission Registry Injected as Singleton:**
- Problem: Memory footprint grows with permissions. Registry built at startup for all modules.
- Files: `framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs:52-61`
- Cause: All permissions loaded into memory regardless of current request context
- Improvement path: Lazy-load permission registry. Cache permission lookups. Consider on-demand registry querying from database.

**CLI Templates Use String Concatenation:**
- Problem: Template generation via string manipulation is slow and hard to maintain
- Files: `cli/SimpleModule.Cli/Templates/FeatureTemplates.cs`, `ModuleTemplates.cs`, `ProjectTemplates.cs`
- Cause: 374-1068 line template files with hardcoded strings
- Improvement path: Switch to Scriban or Liquid templates. Precompile templates. Cache parsed output.

---

## Fragile Areas

**Source Generator Discovery System:**
- Files: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`
- Why fragile: Single large discovery method for all symbol types. Any change to attribute usage breaks discovery. Permission discovery mixes with other concerns.
- Safe modification: Add comprehensive tests for each discovery concern separately. Extract into focused methods. Test with real projects.
- Test coverage: `tests/SimpleModule.Generator.Tests/` has good coverage for module/endpoint discovery. No dedicated permission discovery tests.

**Permission Claim Destination Configuration:**
- Files: `modules/Users/src/Users/Endpoints/Connect/AuthorizationEndpoint.cs:130-164`
- Why fragile: Manual switch statement decides which destinations (AccessToken, IdentityToken) each claim type goes to. Permission claims always added to AccessToken only. No validation that destinations are valid.
- Safe modification: Create explicit destination configuration per claim type. Test all claim/destination combinations. Document why each claim goes to each destination.
- Test coverage: No tests validating claim destinations.

**Test Database Shared Across Test Classes:**
- Files: `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs:23`
- Why fragile: In-memory SQLite connection shared and kept open. Seed data created once per factory instance. Tests relying on isolation may interfere.
- Safe modification: Ensure each test has atomic setup/teardown. Consider per-test isolation. Document data isolation guarantees.
- Test coverage: Tests appear to handle isolation, but contract not explicit in fixture.

---

## Scaling Limits

**All Modules in Single Application:**
- Current capacity: Framework supports unlimited modules via source generator discovery. Host app grows with each module (endpoints, DTOs, permissions).
- Limit: Single executable size, startup time, permission registry memory. ~5-10 modules before noticeable slowdown.
- Scaling path: Split into microservices. Generator outputs separate module SDKs. API Gateway aggregates. Events via message bus.

**Permission Registry Build at Startup:**
- Current capacity: All permissions registered at startup. Fast for ~100 permissions.
- Limit: Startup time grows linearly. ~1000 permissions = slow cold start.
- Scaling path: Lazy-load permissions. Query database per-request (with caching). Stream permission discovery from modules on demand.

**SQLite for Development, No Sharding Plan:**
- Current capacity: SQLite suitable for < 10GB, single developer. PostgreSQL in CI/prod scales further.
- Limit: No multi-tenant sharding. No partition strategy for very large role/permission tables.
- Scaling path: Add per-tenant schema isolation. Partition role/permission tables by module. Consider row-level security (RLS) for multi-tenant.

---

## Dependencies at Risk

**OpenIddict Self-Hosted:**
- Risk: Custom OpenIddict server implementation in Users module. Changes to OpenIddict API could break authentication flow.
- Impact: Login unavailable, all endpoints blocked.
- Migration plan: Monitor OpenIddict releases. Have integration tests that validate full OAuth2 flow. Consider Keycloak/IdentityServer if customization needed in future.

**Roslyn Source Generators (Netstandard2.0):**
- Risk: Generator targets netstandard2.0 (end-of-life). May break with future C# compiler changes.
- Impact: Code generation fails, project won't compile.
- Migration plan: Monitor Roslyn releases. Add comprehensive generator tests. Consider Roslyn 4.x+ features when netstandard2.0 dropped. Keep generator minimal.

**Vogen Value Objects:**
- Risk: Vogen generates converters/serializers. Upstream changes could break EF Core or JSON serialization.
- Impact: Entity persistence or API responses fail.
- Migration plan: Lock Vogen version. Test EF Core model snapshots. Validate JSON round-trips in tests.

---

## Missing Critical Features

**No Permission Update Propagation:**
- Problem: Once a permission claim is issued in JWT, it doesn't update if role permissions change until token refresh
- Blocks: Real-time permission revocation. Instant access control changes.
- Workaround: Implement token short TTL (~5-15 min). Add manual token refresh endpoint. Emit events when permissions change, trigger token invalidation.

**No Audit Trail for Permission Changes:**
- Problem: No logging or history of who changed what permissions when
- Blocks: Compliance audits. Security incident investigation.
- Workaround: Create `PermissionAuditLog` table. Log changes in `PermissionSeedService` and permission management endpoints (future). Use event bus.

**No Permission Soft-Delete or Archival:**
- Problem: Permission constants are compile-time, can't be deprecated or removed gracefully
- Blocks: Safe permission versioning. Backward compatibility during refactoring.
- Workaround: Create deprecated permissions that always fail. Add migration guide. Warn in generator if old permissions referenced.

---

## Test Coverage Gaps

**Permission System Authorization:**
- What's not tested: Integration tests for permission-protected endpoints. Full OAuth2 flow with permission claims. Permission revocation and re-grant.
- Files: `modules/Products/tests/Products.Tests/Integration/ProductsEndpointTests.cs` tests permissions but uses mock test auth. No real OpenIddict flow tests.
- Risk: Real OAuth2 permission projection could fail undetected. Claims might not serialize correctly to JWT.
- Priority: **High** — Authorization is security-critical. Add tests for full OAuth2 token issuance with permissions. Add test for concurrent permission updates.

**Permission Seed Service:**
- What's not tested: Admin role doesn't exist during startup. Permission registry empty. Concurrent seeding. Transaction failures.
- Files: `modules/Users/src/Users/Services/PermissionSeedService.cs` — no corresponding test file
- Risk: Permissions might not be seeded in production if initialization fails. Silent failures.
- Priority: **High** — Add unit tests for all startup scenarios. Add integration test validating Admin user can access all protected endpoints post-seed.

**Source Generator Permission Discovery:**
- What's not tested: Endpoints with multiple permissions. Endpoints with both `[RequirePermission]` and `[AllowAnonymous]`. Invalid permission strings.
- Files: `tests/SimpleModule.Generator.Tests/` has broad coverage but no focused permission discovery tests
- Risk: Generator could silently miss or misparse permissions. Generated authorization wiring could be incomplete.
- Priority: **Medium** — Add dedicated `PermissionDiscoveryTests.cs`. Test attribute parsing, endpoint registration, registry building.

**Claim Destination Assignment:**
- What's not tested: JWT token structure validates correct claims in AccessToken vs IdentityToken. Permission claim format in JWT.
- Files: No tests validating OpenIddict claim destinations
- Risk: Claims might end up in wrong token type. Identity tokens could leak sensitive info.
- Priority: **Medium** — Add test that issues token, decodes JWT, validates permission claims present and in correct destination.

---

*Concerns audit: 2026-03-18*
