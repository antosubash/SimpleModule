# Research Summary: SimpleModule Release Hardening

**Domain:** .NET modular monolith framework hardening
**Researched:** 2026-03-18
**Overall confidence:** HIGH

## Executive Summary

SimpleModule is a functional modular monolith framework with a solid architecture (source-generator module discovery, per-module DbContexts, contract-based communication, Inertia.js SSR). The hardening work is well-scoped: the codebase audit identified specific security gaps, bugs, and technical debt that need resolution before release. None of the issues require architectural changes or new dependencies beyond what the .NET ecosystem provides out of the box.

The hardening stack is straightforward. Rate limiting uses ASP.NET Core's built-in middleware (no new packages). The authorization gap is fixed with a fallback policy configuration (no new packages). Database migrations use EF Core's built-in migration system (already a dependency). The only new NuGet packages needed are Meziantou.Analyzer (compile-time security analysis), Testcontainers.PostgreSql (production-parity test database), and Respawn (test database reset). All three are mature, actively maintained, and widely adopted in the .NET ecosystem.

The highest-risk area is the permission system changes. Adding a fallback authorization policy and removing the admin bypass are both individually simple, but together they can lock out users if not sequenced correctly. The second-highest risk is the EF Core migration transition -- moving from `EnsureCreatedAsync()` to explicit migrations requires careful per-module schema isolation to avoid migration history collisions.

The lowest-risk, highest-impact items are the CLI namespace bug fix, test seed data production guard, and AOT claim removal. These are small, isolated changes with clear fixes.

## Key Findings

**Stack:** No new frameworks needed. Add Meziantou.Analyzer 2.0.260, Testcontainers.PostgreSql 4.11.0, and Respawn 7.0.0. All other needs are met by built-in ASP.NET Core features.

**Architecture:** Five patterns to apply: fallback authorization policy, per-module EF Core migrations, EventBus error handling policy, Testcontainers + Respawn test fixture, and named rate limiting policies. All are well-established .NET patterns.

**Critical pitfall:** The fallback authorization policy will break all currently-anonymous endpoints (login, register, landing page) unless `[AllowAnonymous]` is added first. The admin permission bypass removal will lock out admins unless explicit permissions are assigned first. Both require careful sequencing.

## Implications for Roadmap

Based on research, suggested phase structure:

1. **Security Fixes** - Highest priority, highest risk
   - Addresses: Fallback authorization policy, admin permission refinement, rate limiting, seed data guard
   - Avoids: Pitfall 1 (fallback breaks public endpoints) by inventorying endpoints first; Pitfall 4 (admin lockout) by assigning permissions before removing bypass
   - Sequence within phase: (a) inventory public endpoints + add `[AllowAnonymous]`, (b) enable fallback policy, (c) refactor admin permissions, (d) add rate limiting, (e) guard seed data

2. **Bug Fixes and Correctness** - Medium priority, low risk
   - Addresses: CLI namespace replacement, Inertia HttpContext rendering, EventBus error handling policy
   - Avoids: Pitfall 8 (EventBus parallel execution) by keeping default sequential

3. **Database Migration Strategy** - Medium priority, medium risk
   - Addresses: Per-module EF Core migrations, remove EnsureCreatedAsync from non-test, documentation
   - Avoids: Pitfall 2 (migration history collision) by enforcing per-module schema; Pitfall 5 (EnsureCreated conflict) by environment guard

4. **Test Infrastructure** - Lower priority, low risk
   - Addresses: Testcontainers + Respawn, permission E2E tests, template validation tests, ValidationBuilder tests, DbContext conflict tests
   - Avoids: Pitfall 3 (port collision) by using dynamic ports; Pitfall 7 (Respawn seed data) by excluding lookup tables

5. **Code Quality and Performance** - Lowest priority, lowest risk
   - Addresses: Meziantou.Analyzer, Home.tsx split, EventBus parallelization, DB connection pool config, AOT claim removal
   - Avoids: Pitfall 9 (analyzer false positives) by checking source generator output attributes

**Phase ordering rationale:**
- Security first because shipping with known security holes is unacceptable
- Bug fixes second because they affect developer experience with the framework
- Migrations third because they're needed before any production deployment but not before development/testing
- Test infrastructure fourth because it validates the fixes from phases 1-3
- Code quality last because none of it is release-blocking

**Research flags for phases:**
- Phase 1 (Security): Likely needs careful implementation sequencing -- research the exact endpoint inventory before starting
- Phase 3 (Migrations): May need phase-specific research on multi-provider migration generation (SQLite vs PostgreSQL migration differences)
- Phase 4 (Test Infrastructure): Standard patterns, unlikely to need additional research

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All recommended tools are built-in or mature NuGet packages with verified versions |
| Features | HIGH | Feature list comes directly from the codebase audit; scope is clear |
| Architecture | HIGH | All patterns are well-established ASP.NET Core / EF Core practices |
| Pitfalls | MEDIUM | Pitfalls are based on common .NET patterns and the specific codebase, but some edge cases (e.g., Inertia SSR + fallback policy interaction) may surface during implementation |

## Gaps to Address

- **Inertia SSR + fallback policy interaction**: How does the Blazor SSR shell handle 401 responses? Does it redirect to login or show an error page? This needs testing during phase 1.
- **Multi-provider migration differences**: SQLite and PostgreSQL handle schemas differently (SQLite uses table prefixes, PostgreSQL uses actual schemas). The migration strategy may need provider-specific handling.
- **EventBus DI scoping for parallel handlers**: If handlers are parallelized, each needs its own DI scope for DbContext safety. The current DI registration pattern needs verification.
- **Meziantou.Analyzer rule suppression**: Unknown how many false positives will trigger on the existing codebase. May need significant `.editorconfig` additions before the build passes.
