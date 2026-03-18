# Codebase Concerns

**Analysis Date:** 2026-03-18

## Tech Debt

**Feature Template Scaffolding Incomplete:**
- Issue: CLI feature templates generate code with TODO placeholders rather than complete, buildable code
- Files: `cli/SimpleModule.Cli/Templates/FeatureTemplates.cs`
- Impact: Generated endpoints and validators require manual completion before compilation; developers must write validation logic, HTTP method handling, and business logic themselves
- Fix approach: Enhance template adaptation logic to extract more sophisticated reference implementations; build fallback templates that match common patterns (CRUD operations, validation structures)

**Event Handler Failure Aggregation:**
- Issue: EventBus collects exceptions from all handlers and throws AggregateException, which can mask individual handler failures and complicate error handling in calling code
- Files: `framework/SimpleModule.Core/Events/EventBus.cs`
- Impact: Caller cannot distinguish which handler failed or what operation failed; partial handler completion means some side effects may have occurred before exception is thrown; difficult to test individual handler failures
- Fix approach: Add event-level error handling policy (fail-fast vs. best-effort), implement separate logging/metrics for handler failures, consider returning a result object instead of throwing

**Database Schema Applied at Runtime in Non-Prod:**
- Issue: `EnsureCreatedAsync()` is called on application startup for non-production environments, making schema evolution implicit and difficult to track
- Files: `template/SimpleModule.Host/Program.cs` (line 141)
- Impact: Production deployment requires manual migration management which is not yet documented; development schemas can diverge from migrations if both paths exist; no version control of schema changes
- Fix approach: Migrate off `EnsureCreatedAsync()` to explicit EF Core migrations from day one; generate initial migration after module discovery; document production migration strategy

**No PublishAot Configuration:**
- Issue: CLAUDE.md states the framework is "Fully AOT-compatible" but no projects are configured with `<PublishAot>true</PublishAot>`, and PermissionRegistryBuilder uses reflection (GetFields with BindingFlags)
- Files: `framework/SimpleModule.Core/Authorization/PermissionRegistryBuilder.cs` (line 14), Host project `.csproj`
- Impact: AOT compilation will fail if attempted; reflection-based permission discovery is not AOT-safe; false confidence about AOT readiness
- Fix approach: Add PublishAot to Host project; replace reflection-based permission discovery with source generator or static registration pattern; run `dotnet publish -c Release /p:PublishAot=true` in CI to catch AOT violations

## Known Bugs

**Feature Template Namespace Replacement Incorrect:**
- Bug: When adapting an endpoint from a reference template, namespace replacement is a no-op
- Symptoms: Generated endpoint still has old module namespace instead of new module
- Files: `cli/SimpleModule.Cli/Templates/FeatureTemplates.cs` (lines 99-103)
- Trigger: Run `sm new feature <name>` on any project with existing modules
- Analysis: The replacement target and replacement value are identical — should replace old module name with new
- Workaround: Manually edit generated endpoint file to fix namespace

**Inertia Shell Rendering Context Loss:**
- Bug: InertiaPageRenderer passes HttpContext to the Blazor renderer via dictionary, but HttpContext is not serializable and may not be properly scoped to the rendering lifetime
- Symptoms: May see null reference exceptions when Inertia shell attempts to access HttpContext properties; context may be disposed before rendering completes
- Files: `framework/SimpleModule.Blazor/Inertia/InertiaPageRenderer.cs` (line 27)
- Cause: HttpContext passed as component parameter to HtmlRenderer.RenderComponentAsync; HtmlRenderer may execute on different dispatcher thread where context is not available
- Safe modification: Extract needed context properties (request headers, user claims, response status) before passing to renderer; pass primitives only

## Security Considerations

**Admin Role Bypass Without Fine-Grained Controls:**
- Risk: PermissionAuthorizationHandler grants all permissions to users in "Admin" role without consulting the permission registry; no per-endpoint admin override capability
- Files: `framework/SimpleModule.Core/Authorization/PermissionAuthorizationHandler.cs` (line 13)
- Current mitigation: Relies on strict role assignment at user creation time; assumes all admins are trusted
- Recommendations: Implement permission-based admin actions (e.g., "Admin.Users.Delete"); add audit logging for admin actions; support temporary admin elevation via MFA; test that admin role is not assigned in default seed data for production

**Permission Claims Claimed but Not Enforced at Endpoint Level:**
- Risk: Endpoints marked with `[RequirePermission]` are missing `[Authorize]` attribute; permission checks only happen if explicitly added
- Files: Multiple endpoint files use only `[RequirePermission]` without `[Authorize]`
- Current mitigation: Authorization middleware runs before endpoint handler; unauthenticated requests fail at auth level
- Recommendations: Ensure every `[RequirePermission]` endpoint also has `[Authorize]` or make `[RequirePermission]` imply authorization; add analyzer to enforce this pattern; test unauthenticated access to all protected endpoints

**Personal Data Download Endpoint No Rate Limiting:**
- Risk: DownloadPersonalDataEndpoint has no rate limiting or throttling; user could request personal data export repeatedly
- Files: `modules/Users/src/Users/Endpoints/Users/DownloadPersonalDataEndpoint.cs`
- Current mitigation: None apparent; only requires authentication
- Recommendations: Add IP-based or user-based rate limiting (e.g., 1 request per hour); log all data export requests; consider requiring MFA for sensitive personal data operations; add GDPR compliance messaging

**Test Users Hardcoded Email/Password:**
- Risk: Seed data may contain test credentials that are not changed before production deployment
- Files: Test setup files (not yet examined), test database seeding
- Current mitigation: Relies on CI/CD process to use separate test database
- Recommendations: Never seed test users in production; verify seed data logic only runs in development/test; implement separate seed data scripts for production vs. development; fail startup if seed data cannot be verified as dev-only

## Performance Bottlenecks

**Home Page Component Size:**
- Problem: Dashboard Home.tsx component is 553 lines of code; likely contains multiple concerns (authenticated view, unauthenticated view, quick actions section, statistics section)
- Files: `modules/Dashboard/src/Dashboard/Pages/Home.tsx`
- Cause: Multiple logical sections bundled in single file instead of extracted to sub-components
- Improvement path: Split into Home (shell), DashboardView (authenticated), LandingView (unauthenticated), QuickActionsCard (re-usable); measure component re-render frequency; profile JavaScript bundle size

**EventBus Synchronous Handler Execution:**
- Problem: All handlers for an event execute sequentially in PublishAsync; if any handler is slow (e.g., sends email, calls external API), it blocks other handlers and the caller
- Files: `framework/SimpleModule.Core/Events/EventBus.cs`
- Cause: Handlers are awaited in a foreach loop; no parallelization
- Improvement path: Identify which handlers can run in parallel (independent side effects); use Task.WhenAll for safe-to-parallelize handlers; implement configurable handler execution policies (sequential, parallel, fire-and-forget with error collection)

**Database Connection Pool Management:**
- Problem: No explicit connection pool configuration in ModuleDbContextOptions; default pool size may be exhausted under high concurrency
- Files: `framework/SimpleModule.Database/ModuleDbContextOptionsBuilder.cs`
- Cause: Default EF Core connection pooling (128 connections) may not suit high-traffic scenarios
- Improvement path: Add connection pool size configuration; add health check for pool exhaustion; monitor connection usage in production; document recommended pool sizes for expected load

## Fragile Areas

**CLI Template Parsing and String Replacement:**
- Files: `cli/SimpleModule.Cli/Templates/FeatureTemplates.cs`, `cli/SimpleModule.Cli/Templates/ModuleTemplates.cs`, `cli/SimpleModule.Cli/Infrastructure/TemplateExtractor.cs`
- Why fragile: Complex string parsing and replacement logic using regex and substring operations is brittle; small changes to template structure or naming conventions break generation
- Safe modification: Extract parsing logic into explicit AST-based manipulation (e.g., Roslyn for C#, proper parser for TypeScript); test all replacement patterns with edge cases (names containing special characters, names similar to keywords); version templates and bump version when structure changes
- Test coverage: No dedicated tests found for template generation edge cases; only end-to-end tests via `sm new feature`

**Source Generator Discovery Logic:**
- Files: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`, `framework/SimpleModule.Generator/Emitters/HostDbContextEmitter.cs`
- Why fragile: Namespace-based module matching relies on exact naming conventions; if a DbContext lives in a namespace that doesn't match any module, it silently fails to register; discovery order matters for duplicate detection
- Safe modification: Add diagnostic warnings for unmatched DbContexts and EntityConfigs; implement explicit `[DbContext(Module = "...")]` attribute as alternative to namespace inference; test discovery with edge case namespaces (nested modules, shared namespaces)
- Test coverage: Exists but gaps in edge cases like orphaned DbContexts

**Form Binding Without Explicit Validation Metadata:**
- Files: All endpoint files using `[FromForm]` (e.g., `modules/Products/src/Products/Views/CreateEndpoint.cs`)
- Why fragile: No custom validation attributes; relies on ASP.NET's implicit binding behavior which can silently ignore unknown form fields or coerce types unexpectedly
- Safe modification: Add FluentValidation or custom ModelValidator for all form-bound parameters; make validation explicit; add test for each form endpoint with invalid/missing fields
- Test coverage: Endpoints exist but no tests for malformed form data

## Scaling Limits

**In-Memory Database for Testing Against Real Schema:**
- Current capacity: Tests use SQLite in-memory; works for unit/integration tests but schema doesn't match production (PostgreSQL/SQL Server)
- Limit: Some EF Core migrations, advanced queries (CTEs, window functions), and multi-database features are not tested until CI with real database
- Scaling path: Expand PostgreSQL integration tests; use testcontainers for consistent production-like environments in CI; profile slow queries in both SQLite and PostgreSQL; document which features are database-specific

**Single HostDbContext Consolidating All Modules:**
- Current capacity: Single unified DbContext across all modules; works for monolith but creates single point of contention
- Limit: Schema changes in one module require migrations that affect all modules; high traffic queries from one module can block connections for others
- Scaling path: Keep logical separation via `ModuleDbContextInfo`; consider physical separation into per-module databases if throughput becomes issue; implement read replicas for read-heavy modules; monitor database connection usage per module

**EventBus No Message Queue or Persistence:**
- Current capacity: Events are in-memory and lost on application restart; no event sourcing or audit trail
- Limit: Cannot replay events for debugging/auditing; event failures are not recoverable; no integration with other systems
- Scaling path: If event-driven workflows are critical, implement event sourcing (e.g., MassTransit, NServiceBus); add persistent event log for audit; implement compensating transactions for multi-step workflows

## Dependencies at Risk

**Reflection in PermissionRegistryBuilder (AOT Incompatible):**
- Risk: Uses reflection to discover permission constants; not AOT-safe
- Impact: Cannot build with AOT enabled; type discovery will fail at compile time
- Migration plan: Replace with source generator that scans for `public const string` fields in permission classes and generates static registration code

**HtmlRenderer Dependency on Blazor Runtime:**
- Risk: InertiaPageRenderer depends on Blazor's HtmlRenderer which may not be optimized for Inertia's SSR use case
- Impact: Performance issues if rendering becomes bottleneck; Blazor SSR lifecycle may not align with Inertia component expectations
- Migration plan: Profile rendering latency; if bottleneck, consider custom Inertia middleware that renders React directly to HTML instead of via Blazor; benchmark both approaches

## Missing Critical Features

**No Endpoint Versioning Support:**
- Problem: Endpoints are URI-based only (e.g., `/api/products`); no API versioning (v1, v2) mechanism
- Blocks: Breaking changes to API contracts require migration of all clients simultaneously; cannot support multiple API versions during transition
- Solution approach: Implement ASP.NET versioning (api-version header or URL segment); generate separate routes per version; test backward compatibility for deprecated versions

**No Request/Response Logging by Default:**
- Problem: No centralized request/response logging middleware; only exception logging
- Blocks: Difficult to debug client issues or audit API usage; no request tracing for distributed debugging
- Solution approach: Add middleware that logs request (method, path, headers, user) and response (status, size); make loggable with correlation IDs; configure log sampling for high-traffic endpoints

**No Swagger/OpenAPI Contact/License/Terms:**
- Problem: Swagger UI is configured but missing contact info, license, terms of service
- Blocks: External API consumers cannot find support contact; unclear about API SLA or terms
- Solution approach: Add SwaggerGen configuration for contact, license, external documentation URLs

## Test Coverage Gaps

**Feature Template Generated Code Not Validated:**
- What's not tested: Generated endpoint and validator code from `sm new feature` is not compiled or executed in tests
- Files: `cli/SimpleModule.Cli/Commands/New/NewFeatureCommand.cs`, template generation tests
- Risk: Generated code may have syntax errors, incorrect namespaces, or incompatible types; developers discover issues only when they try to build
- Priority: High — blocks developer productivity
- Fix: Add post-generation validation step that compiles generated code; run generated endpoint through analyzer to ensure compliance

**Permission System With Custom User Claims:**
- What's not tested: E2E tests only test admin bypass; no tests for users with partial permissions (e.g., user with "Products.Read" but not "Products.Write")
- Files: `tests/e2e/tests/flows/permissions.spec.ts`, unit tests for PermissionAuthorizationHandler
- Risk: Permission granularity may not work as expected; permission denial might return 403 instead of expected behavior
- Priority: High — core security feature
- Fix: Add E2E test for non-admin users with specific permissions; add unit tests for handler with mixed permission claims

**Validation Builder No Mutation Tracking:**
- What's not tested: ValidationBuilder can be reused after calling Build(); state mutation is not prevented
- Files: `framework/SimpleModule.Core/Validation/ValidationBuilder.cs`
- Risk: If builder is reused, errors from previous build are still present; unclear if Build() is idempotent
- Priority: Medium
- Fix: Add test that validates Build() returns same result on multiple calls and that new instances don't share state

**No Tests for DbContext Registration Conflicts:**
- What's not tested: What happens if multiple modules try to register DbSet for same entity; duplicate DbSet property names
- Files: `framework/SimpleModule.Generator/Emitters/HostDbContextEmitter.cs` (has some checks but limited test coverage)
- Risk: Silent failures or runtime errors during schema generation
- Priority: Medium
- Fix: Add generator tests for conflict scenarios; validate diagnostic output is clear

---

*Concerns audit: 2026-03-18*
