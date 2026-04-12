# SimpleModule Framework Gap Analysis

**Date:** 2026-04-10
**Scope:** Full framework review across architecture, modules, testing, frontend, security, and source generator diagnostics.

---

## Executive Summary

SimpleModule is a well-architected modular monolith with strong foundations: compile-time module discovery, a comprehensive source generator with 37 diagnostics, fine-grained permission system, and solid security posture. However, the review identified **28 gaps** across 7 categories ranging from documentation drift to missing cross-cutting features.

---

## 1. Documentation & Diagnostic Drift

### 1.1 CLAUDE.md says "SM0001-SM0044" but implementation has SM0001-SM0054
**Severity:** Medium
The source generator implements 37 diagnostics up through SM0054, but CLAUDE.md only documents through SM0044. Nine diagnostics (SM0045-SM0054) covering feature flags, module naming, endpoint structure, and contracts are undocumented.

### 1.2 SM0049 numbering mismatch in CONSTITUTION.md
**Severity:** High
The Constitution says SM0049 is "Module has `IStringLocalizer` injection but no `Locales/en.json` embedded resource." The actual implementation is "Multiple endpoints in a single file" (Error). The localization diagnostic was apparently renumbered or displaced.

### 1.3 SM0050 defined in Constitution but not implemented
**Severity:** Medium
Constitution defines SM0050 as "Locales/en.json exists but is not marked as `EmbeddedResource` in `.csproj`." This rule has no enforcement in the source generator. Localization resource validation is entirely manual.

### 1.4 Large diagnostic ID gaps suggest abandoned or deferred plans
**Severity:** Low
Gaps at SM0008-0009, SM0016-0024, SM0030, SM0036-0037, SM0051 suggest rules were planned but never implemented. No tracking of what those were intended to be.

---

## 2. Cross-Module Communication Gaps

### 2.1 Only 6 of 23 modules publish events
**Severity:** High
The event bus exists and works well (with pipeline behaviors, background dispatch, exception isolation), but only Email, Tenants, Agents, FeatureFlags, Orders, and Datasets publish events. Major modules that should publish events but don't:

- **Products** — no event on create/update/delete (Orders module can't react to product changes)
- **Users** — no event on registration, profile update, role change, password reset
- **FileStorage** — no event on upload/delete (AuditLogs could subscribe)
- **PageBuilder** — no event on publish/unpublish
- **Permissions** — no event on role/permission changes
- **Settings** — no event on setting value changes (modules can't react to config changes)

### 2.2 No event handler discovery pattern
**Severity:** Medium
Events are defined in Contracts projects, but there are no standalone `IEventHandler<T>` implementation files visible. Handlers appear to be registered via lambdas in `ConfigureServices`. This makes it hard to discover which modules subscribe to which events. A convention for handler classes (like endpoint classes) would improve discoverability.

### 2.3 No saga/orchestration pattern for multi-step workflows
**Severity:** Low (documented as out of scope, but worth noting)
Cross-module workflows (e.g., "create order -> reserve product -> send email -> log audit") rely on sequential event handlers. There's no compensating transaction or saga pattern if a step fails mid-way.

---

## 3. Module Consistency Issues

### 3.1 Inconsistent contract interface naming
**Severity:** Low
The Rag module uses `SimpleModule.Rag.Module` instead of `SimpleModule.Rag` for its implementation project. All other modules follow the `SimpleModule.{Name}` convention.

### 3.2 Modules without permissions that probably should have them
**Severity:** Medium
8 modules have no permission definitions: Agents, Dashboard, Localization, Marketplace, Permissions (meta), Rag, Settings, Users. Some of these are reasonable (Dashboard is read-only), but:

- **Settings** — managing system-wide settings should require admin permissions
- **Users** — user management operations should have granular permissions (the module appears to rely on role checks instead of the permission system)
- **Marketplace** — install/uninstall operations should be permission-gated

### 3.3 FakeDataGenerators only covers 3 of 23 modules
**Severity:** Medium
The shared `FakeDataGenerators` in `SimpleModule.Tests.Shared` only has pre-built Bogus fakers for Users, Products, and Orders. The other 20 modules either create fakers inline in tests or don't have them, leading to inconsistent test data patterns.

### 3.4 Five modules have no API endpoints (Agents, Dashboard, Localization, Permissions, Rag)
**Severity:** Low
Some are by design (Localization provides translations via shared data, Permissions is middleware-based). But Agents uses a custom `AgentEndpoints.MapAgentEndpoints()` escape hatch rather than the standard `IEndpoint` pattern, which means it bypasses source generator discovery and diagnostics.

---

## 4. Frontend Gaps

### 4.1 No form validation library
**Severity:** High
There is no client-side form validation framework integrated. All validation is server-side only, which means:
- Users don't get instant feedback on invalid input
- Every validation round-trip requires a server request
- No type-safe form handling (React Hook Form + Zod would be natural fits)

### 4.2 Missing UI components for a full-featured framework
**Severity:** Medium
The UI library has 48 components (excellent foundation), but lacks:
- **Combobox / Async Select** — needed for entity lookups
- **Multi-select** — needed for tag/category assignment
- **File upload component** — FileStorage module exists but no upload widget
- **Date range picker** — needed for audit log filtering, reports
- **Rich text editor** — PageBuilder exists but no WYSIWYG component
- **Error page templates** — no 404/500/403 page components

### 4.3 No network error handling
**Severity:** Medium
The global HTTP error handler converts non-Inertia errors to toasts, but:
- Network failures (offline, timeout) are not explicitly handled
- No offline detection or retry UI
- No request timeout configuration
- Users see browser-default errors for network issues

### 4.4 No loading state management
**Severity:** Low
Individual pages handle loading states manually. No framework-level convention for:
- Optimistic updates
- Skeleton loading patterns
- Navigation progress beyond the basic progress bar

### 4.5 Four modules have no frontend (Agents, Localization, Permissions, Rag)
**Severity:** Low
Backend-only modules are valid, but Agents and Rag could benefit from admin UIs for:
- Viewing agent execution history and tool calls
- Managing RAG document ingestion and search testing

---

## 5. Testing Gaps

### 5.1 No integration tests for event bus cross-module flows
**Severity:** High
The event bus has unit tests for publish/subscribe mechanics, but there are no integration tests verifying that Module A publishing an event correctly triggers Module B's handler in the full application context.

### 5.2 E2E tests only run Chromium
**Severity:** Low
Playwright is configured for Chromium only. Firefox and WebKit are not tested, which could miss browser-specific rendering or JS behavior issues.

### 5.3 No contract testing between modules
**Severity:** Medium
When a Contracts interface changes, there's no automated check that all consumers still compile and work correctly. The source generator catches missing implementations (SM0025), but not behavioral contract violations (e.g., a method now throws where it didn't before).

### 5.4 No mutation testing
**Severity:** Low
Test coverage is measured by line/branch coverage, but there's no mutation testing (e.g., Stryker.NET) to verify that tests actually catch regressions vs. just executing code paths.

---

## 6. Security Gaps

### 6.1 Limited string input validation
**Severity:** Medium
The custom validation framework (`ValidationBuilder`) handles null/empty and numeric range checks, but there's limited evidence of:
- String length limits on user input fields
- Pattern validation (email format, phone numbers)
- HTML/script injection prevention at the input layer (relies on framework-level output encoding)

### 6.2 No request body size limits beyond file uploads
**Severity:** Low
File uploads have configurable size limits, but general API request bodies don't appear to have explicit size limits beyond ASP.NET defaults (28.6MB). A crafted large JSON payload could consume server memory.

### 6.3 No IP-based blocking or suspicious activity detection
**Severity:** Low
Rate limiting exists (per-IP, per-user, fixed/sliding/token-bucket), but there's no:
- IP blocklist capability
- Account lockout after failed login attempts (beyond ASP.NET Identity defaults)
- Anomaly detection for unusual request patterns

---

## 7. Infrastructure & Operations Gaps

### 7.1 No health check aggregation endpoint
**Severity:** Medium
Each module implements `CheckHealthAsync()` returning `ModuleHealthStatus`, but there's no visible `/health` endpoint that aggregates all module health statuses into a single response for load balancers/orchestrators.

### 7.2 No structured logging convention
**Severity:** Medium
Modules use standard `ILogger<T>` but there's no framework-level convention for:
- Structured log fields (correlation IDs, module name, endpoint name)
- Log level guidelines per module
- Centralized log configuration beyond what ASP.NET provides by default

### 7.3 No database migration strategy documentation
**Severity:** Medium
The Constitution says "one migration history" but there's no documented pattern for:
- How modules add migrations
- How migration conflicts between modules are resolved
- Whether EF Core migrations are used vs. a different strategy

### 7.4 No module dependency graph visualization
**Severity:** Low
The source generator detects circular dependencies (SM0010) and illegal implementation references (SM0011), but there's no tool to visualize the actual module dependency graph. This would help new developers understand the architecture.

### 7.5 No OpenTelemetry / distributed tracing integration
**Severity:** Low
For a modular monolith that could eventually extract modules, having tracing spans per module/endpoint would be valuable for performance debugging and future extraction planning.

---

## Priority Ranking

### P0 — Fix Now (Correctness Issues)
1. **SM0049 numbering mismatch** in Constitution — misleading documentation
2. **CLAUDE.md diagnostic range** — update to SM0001-SM0054

### P1 — High Impact Improvements
3. **Add events to Products, Users, FileStorage, PageBuilder, Settings** — enables reactive cross-module patterns
4. **Integrate client-side form validation** (React Hook Form + Zod) — major UX gap
5. **Add integration tests for cross-module event flows**
6. **Expand FakeDataGenerators** to cover all modules

### P2 — Important but Not Urgent
7. **Add missing UI components** (combobox, multi-select, file upload widget, date range picker)
8. **Add permissions to Settings, Users, Marketplace modules**
9. **Document string validation expectations** and add length limits
10. **Add health check aggregation endpoint**
11. **Implement SM0050** (localization resource validation)
12. **Add structured logging conventions**
13. **Add network error handling** to frontend
14. **Document database migration strategy**

### P3 — Nice to Have
15. **Module dependency graph visualization tool**
16. **Event handler discovery convention** (standalone handler classes)
17. **Multi-browser E2E testing**
18. **OpenTelemetry integration**
19. **Error page templates** (404, 500, 403)
20. **Mutation testing setup**
