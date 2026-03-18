# Roadmap: SimpleModule Release Hardening

## Overview

This roadmap resolves every security, correctness, and reliability concern from the codebase audit so SimpleModule can ship with confidence. Work proceeds from highest-risk security fixes through bug fixes, infrastructure improvements, and finally code quality polish. Each phase delivers a coherent, verifiable capability.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Permission Hardening** - Fallback authorization policy and granular admin permissions
- [ ] **Phase 2: Endpoint Security** - Rate limiting and production seed data guard
- [ ] **Phase 3: CLI Bug Fixes** - Namespace replacement bug and fragile template parsing
- [ ] **Phase 4: Runtime Bug Fixes** - Inertia HttpContext rendering and ValidationBuilder mutation
- [ ] **Phase 5: EventBus Improvements** - Error handling policy, handler parallelization, and DI scoping
- [ ] **Phase 6: Database Migrations** - Per-module EF Core migrations replacing EnsureCreatedAsync
- [ ] **Phase 7: Test Infrastructure Foundation** - Testcontainers and Respawn for production-parity testing
- [ ] **Phase 8: Test Coverage** - Permission E2E tests, template validation, and edge case tests
- [ ] **Phase 9: Code Quality** - Home.tsx split, Meziantou analyzer, and documentation cleanup

## Phase Details

### Phase 1: Permission Hardening
**Goal**: Users are denied access by default and admins have auditable, granular permissions
**Depends on**: Nothing (first phase)
**Requirements**: SEC-01, SEC-02, SEC-03
**Success Criteria** (what must be TRUE):
  1. An unauthenticated request to any endpoint without `[AllowAnonymous]` returns 401/403
  2. Login, register, and landing page endpoints remain accessible without authentication
  3. An admin user without a specific permission (e.g., Products.Write) is denied that action
  4. All existing Playwright E2E tests continue to pass after the changes
**Plans**: TBD

Plans:
- [ ] 01-01: TBD
- [ ] 01-02: TBD

### Phase 2: Endpoint Security
**Goal**: Rate-sensitive endpoints are protected and test data cannot leak into production
**Depends on**: Phase 1
**Requirements**: SEC-04, SEC-05
**Success Criteria** (what must be TRUE):
  1. A user requesting personal data download more than once per hour receives a 429 Too Many Requests response
  2. Starting the application in Production environment does not execute seed data methods
  3. Starting the application in Development environment still seeds test data normally
**Plans**: TBD

Plans:
- [ ] 02-01: TBD

### Phase 3: CLI Bug Fixes
**Goal**: The `sm new feature` command generates compilable code with correct namespaces
**Depends on**: Nothing (independent)
**Requirements**: BUG-01, QUAL-04
**Success Criteria** (what must be TRUE):
  1. Running `sm new feature MyFeature` in a module produces files where all namespace declarations match the module name
  2. Template string parsing uses a robust replacement strategy (not fragile regex/substring)
  3. Generated code compiles without errors when added to an existing module
**Plans**: TBD

Plans:
- [ ] 03-01: TBD

### Phase 4: Runtime Bug Fixes
**Goal**: Inertia rendering and ValidationBuilder work correctly under concurrent and reuse scenarios
**Depends on**: Nothing (independent)
**Requirements**: BUG-02, BUG-03
**Success Criteria** (what must be TRUE):
  1. Inertia SSR shell renders without errors when HttpContext properties are accessed on the Blazor renderer thread
  2. Calling ValidationBuilder.Build() multiple times returns independent results with no cross-build error leakage
  3. Concurrent Inertia requests do not produce rendering errors from shared request-scoped state
**Plans**: TBD

Plans:
- [ ] 04-01: TBD

### Phase 5: EventBus Improvements
**Goal**: EventBus handlers execute with configurable error handling and safe parallelism
**Depends on**: Nothing (independent)
**Requirements**: EBUS-01, EBUS-02, EBUS-03
**Success Criteria** (what must be TRUE):
  1. Publishing an event with fail-fast policy stops execution after the first handler failure
  2. Publishing an event with best-effort policy executes all handlers and collects failures
  3. Independent handlers run in parallel via Task.WhenAll by default
  4. Handlers that opt out of parallelism execute sequentially
  5. Each parallel handler resolves services from its own DI scope (no shared DbContext instances)
**Plans**: TBD

Plans:
- [ ] 05-01: TBD
- [ ] 05-02: TBD

### Phase 6: Database Migrations
**Goal**: Production deployments use EF Core migrations instead of EnsureCreatedAsync
**Depends on**: Nothing (independent)
**Requirements**: DB-01, DB-02, DB-03, DB-04
**Success Criteria** (what must be TRUE):
  1. Each module has its own migration history table in its schema (no cross-module collision)
  2. Running `dotnet ef migrations add` for one module does not affect another module's database schema
  3. Connection pool size is configurable per module via ModuleDbContextOptions
  4. A migration workflow document exists describing how to create, apply, and troubleshoot migrations
  5. Test environments still use EnsureCreatedAsync (migrations only apply to non-test)
**Plans**: TBD

Plans:
- [ ] 06-01: TBD
- [ ] 06-02: TBD

### Phase 7: Test Infrastructure Foundation
**Goal**: Tests can run against real PostgreSQL via Testcontainers with fast database reset
**Depends on**: Nothing (independent)
**Requirements**: TEST-05, TEST-06
**Success Criteria** (what must be TRUE):
  1. Integration tests can run against a real PostgreSQL instance spun up by Testcontainers
  2. Database state resets between tests via Respawn without dropping/recreating the database
  3. Testcontainers uses dynamic ports to avoid CI port conflicts
**Plans**: TBD

Plans:
- [ ] 07-01: TBD

### Phase 8: Test Coverage
**Goal**: Security, template, and edge case scenarios have automated test coverage
**Depends on**: Phase 1 (permission tests need fallback policy), Phase 3 (template tests need CLI fixes), Phase 7 (some tests use Testcontainers)
**Requirements**: TEST-01, TEST-02, TEST-03, TEST-04
**Success Criteria** (what must be TRUE):
  1. A test verifies that a user with Products.Read but not Products.Write can browse but not create products
  2. A test runs `sm new feature`, compiles the output, and asserts zero compilation errors
  3. ValidationBuilder tests confirm Build() is idempotent and instances do not share state
  4. A test verifies that registering duplicate DbSet names produces a diagnostic error
**Plans**: TBD

Plans:
- [ ] 08-01: TBD
- [ ] 08-02: TBD

### Phase 9: Code Quality
**Goal**: Frontend is maintainable, static analysis catches issues, and documentation is accurate
**Depends on**: Nothing (independent, but best done last)
**Requirements**: QUAL-01, QUAL-02, QUAL-03
**Success Criteria** (what must be TRUE):
  1. Home.tsx is split into focused sub-components (DashboardView, LandingView, QuickActionsCard at minimum)
  2. Meziantou.Analyzer runs as part of `dotnet build` with appropriate suppressions — build passes cleanly
  3. CLAUDE.md and any other docs no longer claim AOT compatibility
**Plans**: TBD

Plans:
- [ ] 09-01: TBD
- [ ] 09-02: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9
(Phases 3, 4, 5, 6, 7 are independent and could execute in any order after Phase 1)

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Permission Hardening | 0/? | Not started | - |
| 2. Endpoint Security | 0/? | Not started | - |
| 3. CLI Bug Fixes | 0/? | Not started | - |
| 4. Runtime Bug Fixes | 0/? | Not started | - |
| 5. EventBus Improvements | 0/? | Not started | - |
| 6. Database Migrations | 0/? | Not started | - |
| 7. Test Infrastructure Foundation | 0/? | Not started | - |
| 8. Test Coverage | 0/? | Not started | - |
| 9. Code Quality | 0/? | Not started | - |
