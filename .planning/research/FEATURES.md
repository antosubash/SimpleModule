# Feature Landscape: Release Hardening

**Domain:** .NET modular monolith framework hardening
**Researched:** 2026-03-18

## Table Stakes

Features that MUST be addressed before release. Missing any of these = shipping with known defects.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Permission system enforces authorization | Users expect `[RequirePermission]` to actually block unauthorized access without needing a second attribute | Low | Apply fallback authorization policy; `[AllowAnonymous]` for public endpoints |
| Admin role respects fine-grained permissions | Admin bypass without per-action control is a security hole; any serious deployment needs auditable admin actions | Medium | Refactor `PermissionAuthorizationHandler` to check actual permissions, not just role name |
| Rate limiting on sensitive endpoints | Personal data download without throttling is a compliance risk (GDPR) | Low | Built-in `AddRateLimiter` with named policy on the endpoint |
| Test seed data excluded from production | Hardcoded test credentials in production is a critical security vulnerability | Low | Guard seed logic with `IHostEnvironment.IsDevelopment()` check |
| CLI template namespace replacement works | Broken code generation destroys developer trust in the framework | Low | Fix the no-op string replacement in `FeatureTemplates.cs` |
| EventBus error handling policy | Silent exception aggregation masks failures; callers cannot reason about partial success | Medium | Add configurable policy (fail-fast vs best-effort) per event type |
| Database migration strategy documented | `EnsureCreatedAsync()` cannot handle schema evolution; first schema change in production breaks everything | Medium | Implement per-module EF Core migrations, document the workflow |
| Remove AOT claim from documentation | False claims erode trust; PermissionRegistryBuilder uses reflection | Low | Update CLAUDE.md and any other docs |

## Differentiators

Features that improve quality beyond minimum viable but are not strictly blocking release.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Testcontainers PostgreSQL integration | Production-parity testing catches provider-specific bugs that SQLite misses | Medium | Add to test shared project; requires Docker on dev machines |
| Respawn for test isolation | Faster, more reliable test runs; eliminates flaky test state leakage | Low | Pairs with Testcontainers; ~50ms reset vs full DB recreate |
| Meziantou.Analyzer in build pipeline | Catches security and correctness issues at compile time; zero runtime cost | Low | Add to Directory.Build.props; suppress false positives in .editorconfig |
| EventBus parallel handler execution | Independent handlers should not block each other; improves throughput | Medium | Use `Task.WhenAll` for independent handlers; keep sequential option |
| Template generation validation tests | Compile generated code in tests to catch template regressions early | Medium | Add test that runs `sm new feature`, then `dotnet build` on output |
| Home.tsx component split | 553-line component is a maintenance burden and re-render performance issue | Low | Extract to 4-5 focused components; pure refactor |

## Anti-Features

Features to explicitly NOT build during hardening. These are out of scope.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| API versioning | New capability, not hardening; adds complexity to a framework that hasn't shipped yet | Document as future roadmap item |
| Request/response logging middleware | New feature; can be added post-release as a module | Note in docs as recommended addition for deployments |
| AOT compilation support | Requires replacing reflection in PermissionRegistryBuilder with source generator; significant scope | Remove AOT claims; add as separate milestone |
| Event sourcing / message queue | Architecture change, not hardening; current in-memory EventBus is fine for v1 | Document scaling path in architecture docs |
| Swagger metadata (contact, license) | Cosmetic; not release-blocking | Can be added in a 5-minute PR post-release |

## Feature Dependencies

```
Fallback authorization policy -> Permission system E2E tests (test the fix)
Rate limiting middleware -> Personal data endpoint rate limit test
EF Core migrations -> Migration documentation -> Remove EnsureCreatedAsync from non-test
Testcontainers setup -> PostgreSQL integration tests -> DB registration conflict tests
EventBus error policy -> EventBus parallel execution (policy determines parallelization safety)
CLI namespace fix -> Template validation tests (tests verify the fix)
```

## MVP Recommendation (Release-Blocking Priority)

Prioritize in this order:

1. **Permission system fixes** -- security is non-negotiable; fallback policy + admin refinement
2. **Rate limiting on personal data endpoint** -- compliance risk; 30 minutes of work
3. **Test seed data production guard** -- security; trivial fix
4. **CLI namespace replacement bug** -- correctness; small fix with high developer-facing impact
5. **Remove AOT claim** -- honesty; documentation change only
6. **EventBus error handling policy** -- correctness; medium complexity but important for reliability
7. **Database migration strategy** -- technical debt; medium effort but blocks production deployments

Defer to post-release-blocking:
- **Testcontainers/Respawn** -- improves test quality but doesn't fix a defect
- **EventBus parallelization** -- performance optimization, not correctness
- **Home.tsx split** -- code quality, not user-facing
- **Template validation tests** -- nice to have after the namespace bug is fixed

## Sources

- Project concerns audit: `.planning/codebase/CONCERNS.md`
- Project requirements: `.planning/PROJECT.md`
