# Domain Pitfalls: Release Hardening

**Domain:** .NET modular monolith framework hardening
**Researched:** 2026-03-18

## Critical Pitfalls

Mistakes that cause rewrites, security breaches, or major regressions.

### Pitfall 1: Fallback Policy Breaks Existing Public Endpoints

**What goes wrong:** Adding `SetFallbackPolicy(RequireAuthenticatedUser)` immediately returns 401 on all currently-anonymous endpoints (login page, register, landing page, health checks, static files).

**Why it happens:** Developers add the fallback policy and test happy-path authenticated flows, missing that unauthenticated flows now fail.

**Consequences:** Application is unusable for new users -- cannot log in or register. Health check endpoints fail in production monitoring.

**Prevention:** Before enabling fallback policy, inventory ALL endpoints. Mark public ones with `[AllowAnonymous]`. Write tests for anonymous access to login, register, and public pages. Static files served by `UseStaticFiles()` middleware are NOT affected (middleware runs before authorization), but Inertia-rendered public pages ARE affected.

**Detection:** Run the full E2E test suite immediately after adding fallback policy. Any 401 on a public page is a signal.

### Pitfall 2: Per-Module Migration History Table Collision

**What goes wrong:** Two modules use the same default migration history table name (`__EFMigrationsHistory` in the default schema), causing EF Core to think one module's migrations apply to another.

**Why it happens:** Developer forgets to configure `MigrationsHistoryTable` with the module's schema name when setting up per-module migrations.

**Consequences:** Migrations fail silently or apply the wrong schema changes. Database ends up in an inconsistent state that is hard to diagnose.

**Prevention:** Each module's `OnConfiguring` must specify both `HasDefaultSchema("module_name")` and `MigrationsHistoryTable("__EFMigrationsHistory", "module_name")`. Add a test that verifies each module DbContext has a unique schema and migration history table.

**Detection:** `dotnet ef migrations list` shows unexpected migrations for a context.

### Pitfall 3: Testcontainers Port Collision and Connection String Hardcoding

**What goes wrong:** Tests hardcode `localhost:5432` for PostgreSQL instead of using the dynamic port assigned by Testcontainers. Tests pass locally but fail in CI where port 5432 is already in use.

**Why it happens:** Copy-pasting connection strings from development config instead of reading from the Testcontainer instance.

**Consequences:** Flaky CI, tests that work locally but fail in parallel CI runs.

**Prevention:** Always get the connection string from `container.GetConnectionString()`. Never hardcode ports. The `WebApplicationFactory.ConfigureWebHost` override should inject the container's connection string.

**Detection:** Tests fail intermittently in CI with "connection refused" errors.

### Pitfall 4: Admin Permission Refactor Breaks Existing Admin Functionality

**What goes wrong:** Removing the `if (IsAdmin) return true` bypass from PermissionAuthorizationHandler without ensuring admins have all permissions assigned causes admins to lose access to everything.

**Why it happens:** The current system relies on the bypass. No permissions are explicitly assigned to admin users -- they just get a blanket pass.

**Consequences:** Admin users locked out of the application after deploying the "security fix."

**Prevention:** Two-step approach: (1) First, ensure admin role seed data assigns all permissions explicitly. (2) Then remove the bypass. Test both steps independently. Never deploy step 2 without step 1.

**Detection:** Admin user gets 403 on previously-accessible endpoints.

## Moderate Pitfalls

### Pitfall 5: EnsureCreatedAsync and MigrateAsync Conflict

**What goes wrong:** Calling `EnsureCreatedAsync()` on a database that already has migration history prevents future migrations from running (EF Core sees the database as "created" and skips migration application).

**Prevention:** Never call both on the same database. Use `EnsureCreatedAsync()` only for test SQLite in-memory. Use `MigrateAsync()` for development/production with real databases. Guard with environment check.

### Pitfall 6: Rate Limiter Middleware Order

**What goes wrong:** Placing `UseRateLimiter()` after `UseAuthorization()` means rate limiting only applies to authenticated requests. Unauthenticated brute-force attacks bypass rate limits entirely.

**Prevention:** Middleware order should be: `UseRouting()` -> `UseRateLimiter()` -> `UseAuthentication()` -> `UseAuthorization()`. This ensures rate limiting applies before authentication/authorization.

### Pitfall 7: Respawn Deleting Seed Data

**What goes wrong:** Respawn resets ALL tables including lookup/seed data (roles, permissions, default settings). Tests fail because required seed data is missing.

**Prevention:** Configure Respawn to skip tables that contain seed data: `new RespawnerOptions { TablesToIgnore = ["roles", "permissions"] }`. Alternatively, re-seed after each reset (slower but more deterministic).

### Pitfall 8: EventBus Policy Change Breaks Existing Handler Assumptions

**What goes wrong:** Switching from sequential to parallel handler execution exposes thread-safety bugs in handlers that assumed sequential execution (shared state, non-thread-safe DbContext access).

**Prevention:** Default to sequential (backward compatible). Parallel execution is opt-in per event type. Each handler gets its own DI scope (and therefore its own DbContext instance).

## Minor Pitfalls

### Pitfall 9: Meziantou.Analyzer False Positives on Generated Code

**What goes wrong:** Source-generated code triggers Meziantou analyzer warnings, causing build failures (TreatWarningsAsErrors is enabled).

**Prevention:** Add `[GeneratedCode]` attribute to generated files (source generator should already do this). If needed, add `generated_code = true` for the generated files directory in `.editorconfig`.

### Pitfall 10: Playwright Tests Flaky After Authorization Changes

**What goes wrong:** E2E tests that relied on endpoints being accidentally public now fail after adding fallback authorization policy.

**Prevention:** Review all Playwright tests for authentication setup. Ensure test fixtures create authenticated sessions before testing protected pages.

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Permission system fixes | Pitfall 1 (fallback policy breaks public endpoints) + Pitfall 4 (admin lockout) | Inventory endpoints first; two-step admin migration |
| Rate limiting | Pitfall 6 (middleware order) | Follow exact middleware ordering in ARCHITECTURE.md |
| Database migrations | Pitfall 2 (history collision) + Pitfall 5 (EnsureCreated conflict) | Per-module schema isolation; environment guards |
| Test infrastructure | Pitfall 3 (port collision) + Pitfall 7 (Respawn deletes seed) | Dynamic ports; Respawn table exclusions |
| EventBus improvements | Pitfall 8 (parallel execution thread safety) | Default sequential; opt-in parallel with per-handler DI scope |
| Static analysis | Pitfall 9 (false positives on generated code) | GeneratedCode attribute on source generator output |

## Sources

- [EF Core EnsureCreated vs Migrate](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
- [ASP.NET Core Middleware Order](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)
- [Testcontainers Dynamic Ports](https://dotnet.testcontainers.org/modules/postgres/)
- [Respawn Configuration (GitHub)](https://github.com/jbogard/Respawn)
- Project concerns: `.planning/codebase/CONCERNS.md`
