# SimpleModule — Release Hardening

## What This Is

A hardening milestone for the SimpleModule modular monolith framework. The framework already works — modules, endpoints, permissions, event bus, Inertia SSR — but a codebase audit surfaced security gaps, bugs, tech debt, test coverage holes, and performance concerns that need to be addressed before release.

## Core Value

Every security, correctness, and reliability concern identified in the codebase audit is resolved so the framework can ship with confidence.

## Requirements

### Validated

<!-- Shipped and confirmed valuable. -->

- ✓ Modular monolith with compile-time module discovery via Roslyn source generators — existing
- ✓ Permission-based authorization with `[RequirePermission]` attribute — existing
- ✓ Event bus for inter-module communication — existing
- ✓ Inertia.js SSR via Blazor shell — existing
- ✓ CLI scaffolding (`sm new module`, `sm new feature`) — existing
- ✓ Multi-provider database support (SQLite, PostgreSQL, SQL Server) — existing
- ✓ React 19 + Inertia.js frontend with per-module Vite builds — existing
- ✓ Test infrastructure with in-memory SQLite and test auth scheme — existing

### Active

- [ ] Fix admin role bypassing fine-grained permission checks
- [ ] Ensure `[RequirePermission]` implies authorization (no missing `[Authorize]`)
- [ ] Add rate limiting to personal data download endpoint
- [ ] Prevent test seed data from running in production
- [ ] Fix feature template namespace replacement bug
- [ ] Fix Inertia shell HttpContext rendering context loss
- [ ] Improve EventBus error handling (fail-fast vs best-effort policy)
- [ ] Document/improve database migration strategy (move off EnsureCreatedAsync)
- [ ] Remove AOT-compatible claim from docs (PermissionRegistryBuilder uses reflection)
- [ ] Add E2E tests for non-admin users with partial permissions
- [ ] Add template generation validation (compile generated code in tests)
- [ ] Add tests for ValidationBuilder reuse/mutation
- [ ] Add tests for DbContext registration conflicts
- [ ] Split large Home.tsx component into sub-components
- [ ] Fix sequential EventBus handler execution (parallelize independent handlers)
- [ ] Add DB connection pool configuration
- [ ] Fix fragile CLI template string parsing

### Out of Scope

- API endpoint versioning — new feature, not hardening
- Request/response logging middleware — new feature, not hardening
- Swagger/OpenAPI metadata (contact, license, terms) — cosmetic, not release-blocking
- AOT compilation support — deferred; remove claim from docs instead
- Event sourcing / message queue persistence — architecture change, not hardening

## Context

- Codebase audit completed 2026-03-18, documented in `.planning/codebase/CONCERNS.md`
- Framework is functional but has known security gaps in the permission system
- CLI template generation has a confirmed namespace replacement bug
- EventBus design works but has error handling and performance limitations
- Test coverage exists but has gaps in security and edge case scenarios

## Constraints

- **Tech stack**: .NET 10 + React 19 + Inertia.js — no changes to core stack
- **No reflection additions**: Framework aims toward AOT eventually; don't add more reflection
- **Source generator is netstandard2.0**: Generator changes must stay compatible
- **Backward compatibility**: Existing modules (Products, Users, Dashboard) must continue to work

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Remove AOT claim instead of fixing it | PermissionRegistryBuilder uses reflection; fixing requires source generator work that's a separate initiative | — Pending |
| Exclude new features (versioning, logging) | This milestone is about hardening what exists, not adding capabilities | — Pending |
| Include performance fixes | EventBus parallelization and Home.tsx split are correctness-adjacent and improve release quality | — Pending |

---
*Last updated: 2026-03-18 after initialization*
