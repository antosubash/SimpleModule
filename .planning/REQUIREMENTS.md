# Requirements: SimpleModule Release Hardening

**Defined:** 2026-03-18
**Core Value:** Every security, correctness, and reliability concern from the codebase audit is resolved so the framework can ship with confidence.

## v1 Requirements

Requirements for release hardening. Each maps to roadmap phases.

### Security

- [ ] **SEC-01**: Permission system enforces authorization on all endpoints via fallback policy — `[RequirePermission]` blocks unauthorized access without needing a separate `[Authorize]` attribute
- [ ] **SEC-02**: All currently-public endpoints (login, register, landing) have explicit `[AllowAnonymous]` before fallback policy is enabled
- [ ] **SEC-03**: Admin role checks actual permissions instead of blanket bypass — admin actions are auditable and granular
- [ ] **SEC-04**: Personal data download endpoint has rate limiting (max 1 request per hour per user)
- [ ] **SEC-05**: Test seed data is guarded by environment check — cannot run in production

### Bugs

- [ ] **BUG-01**: CLI `sm new feature` generates code with correct namespace (fix no-op replacement in FeatureTemplates.cs)
- [ ] **BUG-02**: Inertia shell rendering extracts needed HttpContext properties into a plain DTO before dispatching to Blazor renderer thread — no request-scoped objects cross execution contexts
- [ ] **BUG-03**: ValidationBuilder prevents state mutation after Build() — reuse does not carry errors from previous builds

### EventBus

- [ ] **EBUS-01**: EventBus supports configurable error handling policy per event type (fail-fast vs best-effort)
- [ ] **EBUS-02**: EventBus parallelizes independent handlers via Task.WhenAll with opt-out for handlers that require sequential execution
- [ ] **EBUS-03**: Each parallel handler gets its own DI scope for DbContext safety

### Database

- [ ] **DB-01**: Per-module EF Core migrations replace EnsureCreatedAsync for non-test environments
- [ ] **DB-02**: Migration history tables are schema-isolated per module to avoid collisions
- [ ] **DB-03**: Database connection pool size is configurable per module via ModuleDbContextOptions
- [ ] **DB-04**: Production migration workflow is documented

### Test Infrastructure

- [ ] **TEST-01**: E2E tests cover non-admin users with partial permissions (user has Products.Read but not Products.Write)
- [ ] **TEST-02**: Template generation validation — test runs `sm new feature`, then compiles the generated code
- [ ] **TEST-03**: ValidationBuilder tests verify Build() idempotency and no cross-instance state leakage
- [ ] **TEST-04**: DbContext registration conflict tests verify diagnostic output for duplicate DbSet names
- [ ] **TEST-05**: Testcontainers.PostgreSql integration for production-parity database testing
- [ ] **TEST-06**: Respawn integration for fast test database reset

### Code Quality

- [ ] **QUAL-01**: Home.tsx split into focused sub-components (DashboardView, LandingView, QuickActionsCard, etc.)
- [ ] **QUAL-02**: Meziantou.Analyzer added to build pipeline via Directory.Build.props with appropriate .editorconfig suppressions
- [ ] **QUAL-03**: AOT-compatible claim removed from CLAUDE.md and any other documentation
- [ ] **QUAL-04**: CLI template string parsing improved — reduce fragility of regex/substring operations in FeatureTemplates.cs

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### API Evolution

- **API-01**: Endpoint versioning support (v1, v2 URL segments or headers)
- **API-02**: Swagger/OpenAPI metadata (contact, license, terms of service)

### Observability

- **OBS-01**: Request/response logging middleware with correlation IDs
- **OBS-02**: Permission audit logging for admin actions

### Architecture

- **ARCH-01**: Replace reflection in PermissionRegistryBuilder with source generator for AOT support
- **ARCH-02**: Event sourcing / message queue persistence for EventBus

## Out of Scope

| Feature | Reason |
|---------|--------|
| AOT compilation support | Requires source generator replacement of PermissionRegistryBuilder reflection — separate initiative |
| API endpoint versioning | New capability, not hardening; framework hasn't shipped yet |
| Request/response logging middleware | New feature; can be added post-release as a module |
| Swagger metadata (contact, license, terms) | Cosmetic; not release-blocking |
| Event sourcing / message queue | Architecture change; in-memory EventBus is fine for v1 |
| Per-module database separation | Scaling optimization; single HostDbContext works for release |
| Real-time chat / WebSocket features | Not part of framework hardening |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| SEC-01 | Phase 1: Permission Hardening | Pending |
| SEC-02 | Phase 1: Permission Hardening | Pending |
| SEC-03 | Phase 1: Permission Hardening | Pending |
| SEC-04 | Phase 2: Endpoint Security | Pending |
| SEC-05 | Phase 2: Endpoint Security | Pending |
| BUG-01 | Phase 3: CLI Bug Fixes | Pending |
| BUG-02 | Phase 4: Runtime Bug Fixes | Pending |
| BUG-03 | Phase 4: Runtime Bug Fixes | Pending |
| EBUS-01 | Phase 5: EventBus Improvements | Pending |
| EBUS-02 | Phase 5: EventBus Improvements | Pending |
| EBUS-03 | Phase 5: EventBus Improvements | Pending |
| DB-01 | Phase 6: Database Migrations | Pending |
| DB-02 | Phase 6: Database Migrations | Pending |
| DB-03 | Phase 6: Database Migrations | Pending |
| DB-04 | Phase 6: Database Migrations | Pending |
| TEST-01 | Phase 8: Test Coverage | Pending |
| TEST-02 | Phase 8: Test Coverage | Pending |
| TEST-03 | Phase 8: Test Coverage | Pending |
| TEST-04 | Phase 8: Test Coverage | Pending |
| TEST-05 | Phase 7: Test Infrastructure Foundation | Pending |
| TEST-06 | Phase 7: Test Infrastructure Foundation | Pending |
| QUAL-01 | Phase 9: Code Quality | Pending |
| QUAL-02 | Phase 9: Code Quality | Pending |
| QUAL-03 | Phase 9: Code Quality | Pending |
| QUAL-04 | Phase 3: CLI Bug Fixes | Pending |

**Coverage:**
- v1 requirements: 25 total
- Mapped to phases: 25
- Unmapped: 0

---
*Requirements defined: 2026-03-18*
*Last updated: 2026-03-18 after roadmap creation*
