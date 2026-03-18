# Architecture

**Analysis Date:** 2026-03-18

## Pattern Overview

**Overall:** Modular monolith with compile-time module discovery via Roslyn source generators. AOT-compatible (no reflection). Backend is .NET minimal APIs; frontend is React 19 + Inertia.js served via Blazor SSR.

**Key Characteristics:**
- Static module wiring at compile-time via `[Module]` attribute and Roslyn generators
- Contract-first module communication (never implementation-to-implementation dependencies)
- Permission system built on claims-based authorization with source generator integration
- Server-side rendered Blazor SSR shell bridges to client-side React via Inertia protocol
- Multi-provider database support (SQLite, PostgreSQL, SQL Server) with per-module DbContexts
- Fully AOT-compatible—no runtime reflection

## Layers

**Framework (Core):**
- Purpose: Defines interfaces, attributes, and shared abstractions that all modules implement against
- Location: `framework/SimpleModule.Core/`, `framework/SimpleModule.Blazor/`, `framework/SimpleModule.Database/`, `framework/SimpleModule.Generator/`
- Contains: Module interface (`IModule`), endpoint interface (`IEndpoint`), DTO marking (`[Dto]`), event bus, permission registry, menu system, Inertia integration
- Depends on: Microsoft.AspNetCore, Entity Framework Core, OpenIddict (Users module only in Core's dependency tree)
- Used by: All modules, Host application

**Module Contracts:**
- Purpose: Public API boundaries for cross-module communication; exports `IXxxContracts` interface and `[Dto]` types
- Location: `modules/{ModuleName}/src/{ModuleName}.Contracts/`
- Contains: Public service interfaces, shared DTOs, strongly-typed IDs
- Depends on: Core only
- Used by: Other modules, endpoints

**Module Implementation:**
- Purpose: Service logic, endpoints, views, database context, event handlers
- Location: `modules/{ModuleName}/src/{ModuleName}/`
- Contains: Module class, endpoint handlers (API + views), service implementations, DbContext, validators, permissions
- Depends on: Own contracts, Core, Database, other module contracts (never implementations)
- Used by: Host (via generator-emitted wiring)

**Host Application:**
- Purpose: Orchestrates all modules; wired by source generators; serves Blazor SSR + Inertia shell
- Location: `template/SimpleModule.Host/`
- Contains: `Program.cs` (DI setup), layouts, authentication middleware, global exception handling
- Depends on: All modules, all framework packages
- Used by: Entry point for requests

**Frontend (React + Inertia):**
- Purpose: Client-side rendering of pages served by Inertia, dynamic module discovery, component library
- Location: `template/SimpleModule.Host/ClientApp/`, `modules/{ModuleName}/src/{ModuleName}/Pages/`, `modules/{ModuleName}/src/{ModuleName}/Views/`
- Contains: Inertia bootstrap, module page bundles (Vite library mode), shared UI component library
- Depends on: React, Inertia, Tailwind, shared UI package
- Used by: Browser after Blazor SSR sends Inertia HTML shell

## Data Flow

**Request Lifecycle (View/Page):**

1. Browser requests `/products/browse` (or any view route)
2. ASP.NET routes to `BrowseEndpoint` (implements `IViewEndpoint`)
3. Endpoint calls `Inertia.Render("Products/Browse", { products: [...] })`
4. Inertia middleware serializes props to JSON
5. `SimpleModule.Blazor` renders Blazor SSR shell (`InertiaShell.razor`) with `<InertiaPage />` component
6. Shell emits HTML with `<div id="app">`, import map for React/Inertia, and `<script type="module">` bootstrapping
7. ClientApp (`app.tsx`) hydrates; `resolvePage` resolves `"Products/Browse"` → imports `Products.pages.js` → renders `Browse` component with server props
8. React takes over for client-side interactions

**Request Lifecycle (API):**

1. Browser/client requests `GET /api/products`
2. ASP.NET routes to `GetAllEndpoint` (implements `IEndpoint`)
3. Endpoint calls `IProductContracts.GetAllProductsAsync()`
4. Service fetches from `ProductsDbContext` (scoped to module)
5. Endpoint returns `200 OK` with JSON (serialized by generated `ModulesJsonResolver` for AOT)
6. Minimal API validation, permission checks applied via `.RequirePermission()` extension

**State Management:**

- **Server state:** Per-request scoped DI container; DbContexts are request-scoped
- **Client state:** Inertia props flow from server on each navigation; React manages local component state
- **Authentication state:** Bearer token (API) or Identity cookie (views); claims projected to `ClaimsPrincipal` (for permission claims)
- **Events:** `IEventBus` broadcasts to `IEventHandler<T>` implementations within same request scope; failures aggregated, not fatal

## Key Abstractions

**IModule:**
- Purpose: Defines module lifecycle; each module class implements this to wire services, endpoints, menu items, permissions
- Examples: `ProductsModule` (`modules/Products/src/Products/ProductsModule.cs`), `UsersModule`, `OrdersModule`
- Pattern: `[Module("Name", RoutePrefix = "api/products", ViewPrefix = "/products")]` attribute; generator discovers these and calls `ConfigureServices()`, `ConfigureEndpoints()`, `ConfigureMenu()`, `ConfigurePermissions()` methods

**IEndpoint:**
- Purpose: Maps a single minimal API endpoint; source generator discovers all implementations
- Examples: `GetAllEndpoint`, `CreateEndpoint` (`modules/Products/src/Products/Endpoints/Products/`)
- Pattern: `public void Map(IEndpointRouteBuilder app)` registers one route; permission attributes (`[RequirePermission(...)]`) and `.RequirePermission()` chaining enforce access control

**IViewEndpoint:**
- Purpose: Maps a view route that renders Inertia pages (extends `IEndpoint`)
- Examples: `BrowseEndpoint` (`modules/Products/src/Products/Views/BrowseEndpoint.cs`)
- Pattern: Calls `Inertia.Render(routeName, props)` to trigger server-side Inertia rendering

**Module DbContext:**
- Purpose: Entity Framework context scoped to module; auto-discovered by generator and registered in unified `HostDbContext`
- Examples: `ProductsDbContext` (`modules/Products/src/Products/ProductsDbContext.cs`)
- Pattern: Extends `DbContext`, applies entity configs, calls `modelBuilder.ApplyModuleSchema()` for multi-provider database support

**Permission System:**
- Purpose: Claims-based authorization; permissions enumerated per module and registered at startup
- Examples: `ProductsPermissions` (string constants), `OrdersPermissions`
- Pattern: Endpoint implements `[RequirePermission(ProductsPermissions.View)]` attribute and/or `.RequirePermission(...)` chain; generator wires permission registry and authorization handler

**Contracts (IXxxContracts):**
- Purpose: Cross-module API boundary; exported by module contract library, never implementations
- Examples: `IProductContracts` (`modules/Products/src/Products.Contracts/IProductContracts.cs`)
- Pattern: Interface defines async service methods; implementations are module-internal

## Entry Points

**Host Program.cs:**
- Location: `template/SimpleModule.Host/Program.cs`
- Triggers: Application startup
- Responsibilities: Configures DI (calls generated `AddModules()`), sets up authentication/authorization, registers Inertia middleware, maps all endpoints (calls generated `MapModuleEndpoints()`), registers health checks

**Module Class ([Module] attribute):**
- Location: `modules/{ModuleName}/src/{ModuleName}/{ModuleName}Module.cs`
- Triggers: Generator discovery at compile-time
- Responsibilities: Invokes `ConfigureServices()` to register DbContext, services, menu items, permissions

**Endpoint Classes (IEndpoint, IViewEndpoint):**
- Location: `modules/{ModuleName}/src/{ModuleName}/Endpoints/` and `modules/{ModuleName}/src/{ModuleName}/Views/`
- Triggers: HTTP request matching minimal API route
- Responsibilities: Validates input (via `[FromBody]`, `[FromRoute]`, etc.), calls service/contract, returns result or renders view

**React App Bootstrap:**
- Location: `template/SimpleModule.Host/ClientApp/app.tsx`
- Triggers: Page load (Inertia shell HTML sent by server)
- Responsibilities: Resolves page component from module bundles, hydrates with server props, attaches React event listeners

## Error Handling

**Strategy:** Centralized exception handler + Inertia invalid response handler

**Patterns:**

- **Backend:** Global `GlobalExceptionHandler` middleware (middleware registered in `Program.cs`) catches all exceptions, logs, returns ProblemDetails response
- **API responses:** Minimal APIs return `TypedResults` (e.g., `NotFound()`, `BadRequest()`)
- **Validation:** Optional validator classes (e.g., `CreateRequestValidator`) use FluentValidation; validators are invoked manually or via `ValidationResult`
- **Frontend:** Inertia router `on('invalid')` handler catches non-Inertia responses (404, 500) and shows toast notification instead of hard error
- **Event failures:** `IEventBus` collects handler exceptions into `AggregateException`; failures don't prevent other handlers from running

## Cross-Cutting Concerns

**Logging:** Microsoft.Extensions.Logging (injected as `ILogger<T>`); used in services and handlers; Health checks also log

**Validation:** Optional—validators (FluentValidation) live in endpoints; called manually or via request validation logic

**Authentication:** Smart auth policy (in `Program.cs`) routes Bearer tokens to OpenIddict validation, cookies to Identity; both set `ClaimsPrincipal`

**Authorization:** Claims-based via permission system; each endpoint declares `[RequirePermission(...)]` attribute; generator emits authorization policies; `PermissionAuthorizationHandler` checks `permission` claims

**Database Transactions:** Entity Framework handles within `DbContext` scope (request-scoped); no explicit transaction management in this architecture

---

*Architecture analysis: 2026-03-18*
