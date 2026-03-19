# SimpleModule: Future Plans & Problems Roadmap

## Context

SimpleModule is a modular monolith framework for .NET with compile-time module discovery via Roslyn source generators. It currently has three working modules (Products, Orders, Users), a React 19 + Inertia.js frontend served via Blazor SSR, and a CI pipeline. The framework is functional but pre-production — several infrastructure gaps need closing before it's ready for real-world use.

This document catalogs known problems, missing capabilities, and planned improvements, organized into prioritized phases and a full reference catalog.

> **Last updated**: 2026-03-18 — Phase 1 complete, significant progress on Phases 2–4. See status column in Known Problems tables for current state.

---

## Phase 1: Foundation (Fix What's Broken) ✅ COMPLETE

All Phase 1 items have been implemented.

### ~~1.1 Request Validation~~ ✅
**Done**: `ValidationBuilder` fluent helper in Core (`framework/SimpleModule.Core/Validation/`). Products and Orders modules have validators. `GlobalExceptionHandler` maps `ValidationException` → HTTP 400 ProblemDetails with field-level errors.

### ~~1.2 EF Core Migrations~~ ✅
**Done**: Initial EF Core migration created (`template/SimpleModule.Host/Migrations/`). `EnsureCreated()` used in dev; migrations available for production. Vogen value converters integrated.

### ~~1.3 Consistent Endpoint Pattern~~ ✅
**Done**: Products fully migrated to `IEndpoint` pattern. `ConfigureEndpoints()` escape hatch removed from Products. `CrudEndpoints` static helper in Core reduces boilerplate across all modules.

### ~~1.4 Error Handling & Problem Details~~ ✅
**Done**: `GlobalExceptionHandler` implements `IExceptionHandler` with RFC 7807 ProblemDetails for validation (400), not-found (404), conflict (409), and unhandled (500) exceptions.

### ~~1.5 Use Generated TypeScript Types~~ ✅
**Done**: `[Dto]` types generate per-module `types.ts` files. React pages import from generated types. `tools/extract-ts-types.mjs` wired into build pipeline.

---

## Phase 2: Capability (Build What's Missing)

These add significant value and unblock real application development.

### 2.1 Pagination & Filtering
**Problem**: All list endpoints return `ToListAsync()` — entire table. No pagination, search, or sort.
**Plan**: Add `PagedResult<T>` to Core. Standard query parameters (`page`, `pageSize`, `sort`, `search`). Source generator could generate filtered query extensions from [Dto] properties.
**Files**: SimpleModule.Core (PagedResult, query helpers), all list endpoints, React list pages.

### 2.2 Client-Side Form Validation & UX
**Problem**: No validation error display, no loading states on buttons, no toast/notification system.
**Plan**:
- Inertia error bag → display field-level errors
- `useForm` hook wrapper with loading state
- Toast component for success/error notifications
**Files**: ClientApp (shared components), all module page components.

### 2.3 Authorization Policies
**Problem**: Auth exists (Identity + OpenIddict) but no fine-grained authorization. No role-based endpoint protection.
**Plan**: Module-level authorization policies. Source generator discovers `[Authorize]` on endpoints. Admin-only routes for Products/Orders management.
**Files**: SimpleModule.Core (policy abstractions), module endpoints, generator.

### 2.4 Event Handler Auto-Discovery
**Problem**: `IEventHandler<T>` exists and works, but handlers must be manually registered in DI. `OrderCreatedEvent` is declared but no handlers are wired.
**Plan**: Source generator discovers `IEventHandler<T>` implementations and generates DI registration. Same pattern as IEndpoint discovery.
**Files**: SimpleModule.Generator, SimpleModule.Core (events).

### 2.5 Dashboard Module
**Problem**: No landing page after login. No overview of system state.
**Plan**: Dashboard module with widget system. Each module contributes widgets (order count, product stats, user activity). Uses event bus or contracts for cross-module data.
**Files**: New module: src/modules/Dashboard/.

### 2.6 File Upload Support
**Problem**: No file handling infrastructure. Common need for product images, user avatars, document attachments.
**Plan**: `IFileStorage` abstraction in Core (local disk + S3 compatible). Upload endpoint pattern. Image processing pipeline.
**Files**: SimpleModule.Core (IFileStorage), new endpoints, React upload components.

### 2.7 Strongly-Typed IDs ✅
**Done**: Vogen-based `ProductId`, `OrderId`, `UserId` value objects in module `.Contracts` projects. Compile-time type safety for entity IDs with auto-generated EF Core value converters, JSON converters, and route parameter binding.

---

## Phase 3: Polish (Production Readiness)

These make the difference between a demo and a deployable system.

### ~~3.1 Observability~~ ✅
**Done**: Aspire integration with OpenTelemetry (service defaults, tracing, metrics). `SimpleModule.AppHost` orchestrates services with `Aspire.Hosting.PostgreSQL`. `AddServiceDefaults()` and `MapDefaultEndpoints()` wired in Host.

### 3.2 Caching
**Problem**: Every request hits the database. No caching layer.
**Plan**: `IModuleCache` abstraction. In-memory default, Redis optional. Cache invalidation via event bus (when product updated, invalidate product cache).
**Files**: SimpleModule.Core (caching abstractions), module services.

### 3.3 Rate Limiting
**Problem**: No rate limiting on any endpoint. API abuse possible.
**Plan**: ASP.NET Core rate limiting middleware. Per-endpoint policies. Module-configurable limits.
**Files**: SimpleModule.Api (middleware registration), endpoint attributes.

### 3.4 API Versioning
**Problem**: No versioning strategy. Breaking changes would affect all consumers.
**Plan**: URL-based versioning (`/v1/products`). RoutePrefix on [Module] supports this. Version negotiation in Inertia responses.
**Files**: Module attributes, generator (version-aware routing).

### 3.5 Security Hardening
**Problem**: No CSRF on API endpoints (Inertia handles some), no Content-Security-Policy, no rate limiting on auth endpoints.
**Plan**: CSP headers, anti-forgery on state-changing endpoints, brute-force protection on login/token endpoints, security headers middleware.
**Files**: SimpleModule.Api (middleware), Users module (auth endpoints).

### 3.6 Background Jobs
**Problem**: No async processing. Long operations (email sending, report generation, data import) would block requests.
**Plan**: Lightweight job queue. Could use `IEventBus` pattern with persistent queue backend. Or integrate Hangfire/Quartz.
**Files**: SimpleModule.Core (job abstractions), new infrastructure project.

### 3.7 Deployment & Configuration
**Problem**: Docker exists but no Kubernetes manifests, no environment-specific configuration, no secrets management guidance.
**Plan**: Helm chart or K8s manifests. Document configuration hierarchy. Environment-specific appsettings. Secrets via environment variables or vault.
**Files**: Infrastructure directory, documentation.

---

## Phase 4: Developer Experience

These make the framework pleasant to build with.

### 4.1 CLI Enhancements (`sm` tool)
**Problem**: CLI exists but feature generation templates have TODO placeholders. Limited scaffolding.
**Plan**: Complete `sm add feature` command. Add `sm add endpoint`, `sm add event`, `sm add migration`. Interactive prompts.
**Files**: SimpleModule.Cli (FeatureTemplates.cs, commands).

### 4.2 Hot Reload for Module Pages
**Problem**: Module Vite builds require manual `npm run build` or `npm run watch`. No integrated dev experience.
**Plan**: `npm run dev` script that watches all modules. Vite HMR through Inertia. Single command development experience.
**Files**: Root package.json (scripts), module vite configs.

### 4.3 Documentation Site
**Problem**: CLAUDE.md and design docs exist but no user-facing documentation.
**Plan**: Doc site (Docusaurus or similar) covering: getting started, module creation guide, architecture overview, API reference.
**Files**: New docs/ directory.

### 4.4 Integration Test Harness
**Problem**: Tests exist but no shared test utilities. Each module reinvents WebApplicationFactory setup.
**Plan**: `SimpleModule.Testing` package with: test server builder, authenticated test client, database seeding helpers, Inertia response assertions.
**Files**: New project: src/SimpleModule.Testing/.

### ~~4.5 Source Generator Diagnostics~~ ✅ (Partial)
**Done**: Source generator refactored into focused `IEmitter` pattern (9 emitters: Diagnostic, Endpoint, JSON, Menu, Module, RazorComponent, TypeScript, ViewPages, HostDbContext). `DiagnosticEmitter` exists. Full analyzer diagnostics for common mistakes still pending.

---

## Known Problems (Full Catalog)

### Framework Core
| # | Problem | Severity | Phase | Status |
|---|---------|----------|-------|--------|
| F1 | No request validation pipeline | High | 1 | ✅ Done |
| F2 | No EF Core migrations | High | 1 | ✅ Done |
| F3 | No structured error responses (Problem Details) | High | 1 | ✅ Done |
| F4 | Event handlers not auto-discovered by generator | Medium | 2 | Open |
| F5 | No pagination/filtering abstractions | Medium | 2 | Open |
| F6 | No caching layer | Medium | 3 | Open |
| F7 | No background job system | Medium | 3 | Open |
| F8 | No file storage abstraction | Medium | 2 | Open |
| F9 | No rate limiting | Medium | 3 | Open |
| F10 | No API versioning | Low | 3 | Open |
| F11 | Menu URLs are untyped strings | Low | 4 | Open |
| F12 | No strongly-typed entity IDs | High | 2 | ✅ Done (Vogen) |

### Frontend
| # | Problem | Severity | Phase | Status |
|---|---------|----------|-------|--------|
| FE1 | Generated TS types unused (inline duplicates) | High | 1 | ✅ Done |
| FE2 | No client-side validation error display | High | 2 | Open |
| FE3 | No loading states during form submission | Medium | 2 | Open |
| FE4 | No toast/notification system | Medium | 2 | Open |
| FE5 | No shared React component library | Medium | 2 | Open |
| FE6 | QRCode module partially integrated | Low | 2 | Open |
| FE7 | Product Browse page has minimal styling vs others | Low | 2 | Open |
| FE8 | No Vite HMR integration for dev workflow | Medium | 4 | Open |

### Architecture
| # | Problem | Severity | Phase | Status |
|---|---------|----------|-------|--------|
| A1 | Mixed endpoint patterns (IEndpoint + ConfigureEndpoints) | High | 1 | ✅ Done |
| A2 | Manual form parsing in endpoints (ReadFormAsync) | High | 1 | ✅ Done |
| A3 | Seed data hard-coded in DbContext (not environment-aware) | Medium | 3 | Open |
| A4 | TypeScript extraction tool is fragile (depends on generator output format) | Medium | 4 | Open |
| A5 | No cross-module query pattern (only contracts interfaces) | Low | 3 | Open |
| A6 | CRUD endpoint boilerplate | Medium | 2 | ✅ Done (CrudEndpoints helper) |

### Production / Operations
| # | Problem | Severity | Phase | Status |
|---|---------|----------|-------|--------|
| P1 | No observability (metrics, tracing) | High | 3 | ✅ Done (Aspire + OpenTelemetry) |
| P2 | No security headers (CSP, HSTS beyond default) | Medium | 3 | Open |
| P3 | No brute-force protection on auth endpoints | Medium | 3 | Open |
| P4 | No K8s/production deployment manifests | Medium | 3 | Open |
| P5 | No secrets management guidance | Low | 3 | Open |

### Testing
| # | Problem | Severity | Phase | Status |
|---|---------|----------|-------|--------|
| T1 | No shared test utilities / test harness | Medium | 4 | Open |
| T2 | No frontend tests (Playwright setup exists, no tests) | Medium | 4 | ✅ Done (5 E2E test files) |
| T3 | Integration tests only for Products, not Orders/Users | Medium | 2 | Open |
| T4 | No load/performance testing | Low | 3 | Open |

### Developer Experience
| # | Problem | Severity | Phase | Status |
|---|---------|----------|-------|--------|
| DX1 | CLI feature templates have TODO placeholders | Medium | 4 | Open |
| DX2 | No user-facing documentation site | Medium | 4 | Open |
| DX3 | Source generator failures are hard to debug | Low | 4 | ✅ Partial (IEmitter refactor + DiagnosticEmitter) |
| DX4 | New module creation requires many manual steps | Medium | 4 | Open |

---

## Potential New Modules

| Module | Purpose | Dependencies | Priority |
|--------|---------|-------------|----------|
| **Dashboard** | Landing page with widgets from other modules | Products, Orders, Users contracts | High |
| **Notifications** | In-app + email notifications, subscribes to events | Event bus, Users | Medium |
| **Settings** | App-wide configuration UI (feature flags, system settings) | None | Medium |
| **Audit** | Track entity changes, user actions | Event bus, all modules | Medium |
| **FileStorage** | Centralized file/image management | Core abstractions | Medium |
| **Reports** | Exportable reports, scheduled generation | Products, Orders, background jobs | Low |

---

## Recent Completions (since 2026-03-13)

Work not originally in the roadmap that was completed:

| Item | Description |
|------|-------------|
| **Source generator refactor** | Monolithic generator split into 9 focused `IEmitter` classes with full test coverage |
| **NetEscapades.EnumGenerators** | AOT-safe enum extensions for `DatabaseProvider`, `MenuSection`, `CheckStatus` |
| **Vogen strongly-typed IDs** | `ProductId`, `OrderId`, `UserId` value objects in module Contracts projects |
| **ValidationBuilder** | Fluent validation helper in Core, wired into Products and Orders |
| **CrudEndpoints helper** | Static helper reducing CRUD endpoint boilerplate across modules |
| **GlobalExceptionHandler** | RFC 7807 ProblemDetails for all error types |
| **EF Core migration** | Initial migration with Vogen value converters |
| **Aspire + OpenTelemetry** | Service defaults, distributed tracing, PostgreSQL orchestration |
| **Playwright E2E tests** | 5 test suites covering Products, Orders, Dashboard flows |

## Verification

This is a planning document — verification means reviewing it periodically against actual project state:
1. Check off completed items as they're implemented
2. Re-prioritize based on what you're actually building
3. Update severity ratings as the project evolves
4. Remove items that become irrelevant
