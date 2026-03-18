# Architecture

**Analysis Date:** 2026-03-18

## Pattern Overview

**Overall:** Modular monolith with compile-time module discovery and static endpoint/menu registration via Roslyn source generators. No reflection. Frontend uses React 19 + Inertia.js for page rendering, with Blazor SSR as the HTML shell renderer.

**Key Characteristics:**
- Compile-time module discovery and AOT-compatible static initialization
- Each module is self-contained with independent DbContext, services, and permissions
- Schema isolation per module (table prefixes for SQLite, schemas for PostgreSQL/SQL Server)
- Minimal API endpoints with permission-based authorization
- Inertia.js protocol for page state management and component hydration
- View endpoints return Inertia responses with server-side props; API endpoints return JSON
- Event bus for inter-module communication with isolated handler failures

## Layers

**Framework (Core):**
- Purpose: Defines extension points and cross-cutting concerns
- Location: `framework/SimpleModule.Core`, `framework/SimpleModule.Blazor`, `framework/SimpleModule.Database`, `framework/SimpleModule.Generator`
- Contains: Base interfaces (`IModule`, `IEndpoint`, `IViewEndpoint`), attributes (`[Module]`, `[Dto]`, `[RequirePermission]`), Inertia integration, menu system, event bus, authorization/permissions, validation, exception handling, database utilities
- Depends on: Microsoft.AspNetCore.*, Microsoft.EntityFrameworkCore, System.Reflection.Metadata (for generators)
- Used by: All modules, Host application

**Modules:**
- Purpose: Isolated feature domains with complete autonomy over endpoints, database, services, and menus
- Location: `modules/<ModuleName>/src/<ModuleName>`
- Contains: Module class (`*Module.cs` implementing `IModule`), endpoints (API and View), services, DbContext, DTOs, permissions, React pages/views
- Depends on: SimpleModule.Core, SimpleModule.Database, own Contracts project
- Used by: Host application (via generated registration)

**Module Contracts:**
- Purpose: Public service interfaces and shared DTOs for cross-module communication
- Location: `modules/<ModuleName>/src/<ModuleName>.Contracts`
- Contains: `I<ModuleName>Contracts` interface, `[Dto]` types, value objects (e.g., `ProductId`)
- Depends on: SimpleModule.Core only
- Used by: Other modules, Host

**Frontend Packages:**
- Purpose: Shared UI components, utilities, and theme
- Location: `packages/SimpleModule.Client`, `packages/SimpleModule.UI`, `packages/SimpleModule.Theme.Default`
- Contains: Vite page resolver, Radix UI component wrappers, Tailwind theme
- Used by: All modules' React builds, Host ClientApp

**Host Application:**
- Purpose: Orchestrates module registration, Blazor/Inertia setup, and serves static assets
- Location: `template/SimpleModule.Host`
- Contains: `Program.cs` (calls generated `AddModules()`, `MapModuleEndpoints()`, `CollectModuleMenuItems()`), Blazor shell components, ClientApp bootstrap, middleware setup
- Depends on: All modules, SimpleModule.Core, SimpleModule.Blazor, OpenIddict, ASP.NET Core
- Used by: Runs as the only executable

**Code Generator:**
- Purpose: Compile-time discovery and code emission for AOT static initialization
- Location: `framework/SimpleModule.Generator`
- Contains: `ModuleDiscovererGenerator` (IIncrementalGenerator), Emitters (modules, endpoints, menus, TypeScript definitions, JSON resolvers, DbContext aggregation)
- Targets: netstandard2.0, runs during Host build
- Emits: Extension methods in generated files for Host to consume in `Program.cs`

## Data Flow

**Page Request (Inertia SSR):**

1. Browser requests `/products/browse` (GET, no X-Inertia header)
2. ASP.NET Core router matches `BrowseEndpoint.Map()` → `MapGet("/browse", ...)`
3. Endpoint calls `Inertia.Render("Products/Browse", props: { products: [...] })`
4. `InertiaResult.ExecuteAsync()` detects no X-Inertia header → renders full HTML
5. `InertiaPageRenderer` uses Blazor's `HtmlRenderer` to SSR the `<InertiaShell>` Blazor component
6. Shell component receives page JSON and renders it inline
7. HTML sent to browser with embedded React props in `<script id="inertia-page-data">`
8. Browser hydrates React from `ClientApp/app.tsx`:
   - Creates Inertia app with `resolvePage` resolver
   - Splits component name: `Products/Browse` → module `Products`
   - Dynamic import from `/_content/Products/Products.pages.js`
   - Gets `Browse` component from exported `pages` record
   - Renders with server-provided props

**Page Refresh (Inertia Client-Side Navigation):**

1. User clicks link → React Router/Inertia handles navigation
2. Sends GET request with `X-Inertia: true` header
3. Server middleware detects header → returns JSON only (no HTML)
4. Response contains: `{ component, props, url, version }`
5. React component updates, props hydrate
6. No full-page reload

**API Request (JSON):**

1. Client sends POST/PUT/DELETE with JSON body
2. Endpoint mapped as `IEndpoint` (not `IViewEndpoint`)
3. Returns `TypedResults.Ok()`, `TypedResults.Created()`, etc.
4. Response is JSON, no Inertia protocol involved
5. Used by dynamic forms, fetch() calls from React components

**Module Registration (Startup):**

1. Host's `Program.cs` calls generated `builder.Services.AddModules(config)`
2. Emitted code: instantiates each `[Module]` class, calls `ConfigureServices()`, `ConfigurePermissions()`
3. Each module registers: DbContext, services, menu items, permission claims
4. Generated `MapModuleEndpoints()` collects all `IEndpoint` and `IViewEndpoint` implementors
5. Host calls `app.MapModuleEndpoints()` → all endpoints mapped

**Event Publishing:**

1. Module publishes event via `IEventBus.PublishAsync<T>(event)`
2. Event bus retrieves all registered `IEventHandler<T>` instances
3. Invokes each handler, collects exceptions
4. If any handler fails, throws `AggregateException` with all failures
5. Handlers are isolated — one failure doesn't prevent other handlers from running

**State Management:**

- Server-side: `IProductContracts` service holds domain logic; DbContext manages EF Core state
- Client-side: React component state from Inertia props; localStorage for client-only state
- Shared: Strongly-typed IDs (`ProductId`, `UserId`) in Contracts; DTOs have JSON serializers auto-generated
- Cross-module: Event bus (`IEventBus`) for decoupled notifications

## Key Abstractions

**IModule:**
- Purpose: Module definition and configuration hook
- Location: `framework/SimpleModule.Core/IModule.cs`
- Pattern: Modules implement this interface and decorate class with `[Module]`
- Methods: `ConfigureServices()` (DI), `ConfigureEndpoints()` (custom routing), `ConfigureMenu()` (UI navigation), `ConfigurePermissions()` (auth claims)
- Used by: All feature modules; generated code calls these during startup

**IEndpoint:**
- Purpose: Minimal API route definition
- Location: `framework/SimpleModule.Core/IEndpoint.cs`
- Pattern: One class per route; Map() method defines the route handler
- Example: `GetAllEndpoint` maps GET `/`, calls service, returns JSON
- Auto-discovered by generator; no explicit registration needed

**IViewEndpoint:**
- Purpose: Inertia page route definition
- Location: `framework/SimpleModule.Core/IViewEndpoint.cs`
- Pattern: Similar to IEndpoint but returns `Inertia.Render(component, props)`
- Example: `BrowseEndpoint` maps GET `/browse`, fetches products, renders "Products/Browse" component
- Auto-discovered by generator; distinguishes view routes from API routes

**IEventBus and IEvent:**
- Purpose: Publish-subscribe for inter-module communication
- Location: `framework/SimpleModule.Core/Events/`
- Pattern: Module defines event class (implements `IEvent`), other modules implement `IEventHandler<T>`
- Example: User creation publishes `UserCreatedEvent`; Orders module listens and creates welcome order
- Benefits: Decoupled modules, isolated failures, no direct dependencies between modules

**IProductContracts (and other *Contracts interfaces):**
- Purpose: Public service API for inter-module use
- Location: `modules/<Name>/src/<Name>.Contracts/I<Name>Contracts.cs`
- Pattern: Services implement this interface; other modules depend on interface, not implementation
- Example: Orders module injects `IProductContracts` to fetch products for order creation
- Protects: Other modules see only contracts, never internal services

**Inertia.Render():**
- Purpose: Renders a React component with server-provided props
- Location: `framework/SimpleModule.Core/Inertia/InertiaResult.cs`
- Pattern: Returns `IResult` from endpoint handler
- Serialization: Props serialized to JSON with `JsonSerializerOptions { PropertyNamingPolicy = CamelCase }`
- Behavior: Full HTML on first request, JSON on subsequent client-side navigation

**[Dto] Attribute:**
- Purpose: Marks types for code generation (TypeScript interfaces, JSON serializers)
- Location: `framework/SimpleModule.Core/DtoAttribute.cs`
- Pattern: Applied to request/response types and shared data classes
- Generated: TypeScript `.d.ts` files in `ClientApp/types/`, AOT JSON serializers in Host

**[RequirePermission] Attribute:**
- Purpose: Declarative permission check on endpoints
- Location: `framework/SimpleModule.Core/Authorization/RequirePermissionAttribute.cs`
- Pattern: Applied to endpoint classes; generator extracts permissions, creates authorization requirements
- Example: `[RequirePermission(ProductsPermissions.Delete)]` on DeleteEndpoint
- Evaluation: `PermissionAuthorizationHandler` checks `User.HasClaim("permission", "products:delete")`

## Entry Points

**Host Application:**
- Location: `template/SimpleModule.Host/Program.cs`
- Triggers: `dotnet run --project template/SimpleModule.Host`
- Responsibilities:
  - Configures Blazor, Inertia, OpenIddict, Entity Framework
  - Calls `AddModules()` (generated) to register all modules
  - Calls `MapModuleEndpoints()` (generated) to map all routes
  - Calls `CollectModuleMenuItems()` (generated) to aggregate menu
  - Runs on HTTPS https://localhost:5001

**Module Class (e.g., ProductsModule):**
- Location: `modules/Products/src/Products/ProductsModule.cs`
- Triggers: Discovered by generator during Host build
- Responsibilities:
  - Registers `ProductsDbContext`, `IProductContracts` service
  - Calls `ConfigurePermissions()` to define "products:view", "products:create", etc.
  - Calls `ConfigureMenu()` to register navbar items for Products

**Endpoints (e.g., GetAllEndpoint):**
- Location: `modules/Products/src/Products/Endpoints/Products/GetAllEndpoint.cs`
- Triggers: HTTP GET to `/products` (route prefix configured in Module)
- Responsibilities:
  - Receives injected `IProductContracts` service
  - Calls `GetAllProductsAsync()` from contracts
  - Uses `CrudEndpoints.GetAll()` helper to wrap response
  - Checks `[RequirePermission(ProductsPermissions.View)]`

**View Endpoints (e.g., BrowseEndpoint):**
- Location: `modules/Products/src/Products/Views/BrowseEndpoint.cs`
- Triggers: HTTP GET to `/products/browse` (ViewPrefix configured in Module)
- Responsibilities:
  - Fetches data from contracts
  - Renders Inertia component with props: `Inertia.Render("Products/Browse", new { products = [...] })`

**React Components (e.g., Browse.tsx):**
- Location: `modules/Products/src/Products/Views/Browse.tsx`
- Triggers: Inertia resolves "Products/Browse" → dynamically imports from `Products.pages.js`
- Responsibilities:
  - Accepts props from server (products list)
  - Renders UI using Radix + Tailwind
  - Handles user interactions, client-side state

**Blazor Shell (InertiaShell):**
- Location: `template/SimpleModule.Host/Components/InertiaShell.razor`
- Triggers: When rendering full HTML page (first request or refresh)
- Responsibilities:
  - Receives JSON page data from `InertiaPageRenderer`
  - Renders HTML structure, links, scripts
  - Embeds page JSON in `<script id="inertia-page-data">`
  - Loads React bootstrap script (`/js/shell.js`)

**Module Page Index (e.g., Products/index.ts):**
- Location: `modules/Products/src/Products/Pages/index.ts`
- Triggers: Generated during module Vite build
- Responsibilities:
  - Exports `pages` record mapping component names to React components
  - Example: `'Products/Browse': Browse, 'Products/Create': Create`

## Error Handling

**Strategy:** Fail-fast with typed exceptions; global handler converts to problem responses

**Patterns:**
- `ValidationException` (400 Bad Request): Field validation errors from validators or manual checks
- `NotFoundException` (404 Not Found): Entity not found in database
- `ConflictException` (409 Conflict): Business rule violation (e.g., duplicate entry)
- Custom exceptions inherit from `Exception` and are caught by `GlobalExceptionHandler`
- Handler converts exceptions to ProblemDetails with appropriate HTTP status

**Example:**
```csharp
// In endpoint handler
var validation = CreateRequestValidator.Validate(request);
if (!validation.IsValid)
    throw new ValidationException(validation.Errors);
```

## Cross-Cutting Concerns

**Logging:**
- Framework: Structured logging via `ILogger<T>` (Microsoft.Extensions.Logging)
- Pattern: Used in EventBus handler failures, service methods
- Example: `LogHandlerFailed()` in `EventBus.cs` logs handler exceptions

**Validation:**
- Framework: Custom `ValidationResult` + `ValidationBuilder` pattern; FluentValidation in test data generators
- Pattern: Validators in `Endpoints/` folders check request models before business logic
- Example: `CreateRequestValidator.Validate(request)` returns errors, endpoint throws `ValidationException`

**Authentication:**
- Framework: ASP.NET Core Identity + OpenIddict (OAuth2)
- Pattern: Smart auth scheme forwards Bearer tokens to OpenIddict, cookies to Identity
- Claims: User identity claims (name, email) + permission claims (e.g., "products:view")

**Authorization:**
- Framework: ASP.NET Core Authorization with custom `PermissionAuthorizationHandler`
- Pattern: `[RequirePermission]` attribute on endpoint classes; generator creates `PermissionRequirement` and policy
- Handler: Checks `User.HasClaim("permission", permission)` or `User.IsInRole("Admin")`
- Fallback: Admin role bypasses all permission checks

**Database Multi-Tenancy:**
- Framework: Schema isolation per module via `ModuleDbContextInfo`
- Pattern: SQLite uses table prefixes (e.g., `products_product`), PostgreSQL uses schemas (e.g., `products.product`)
- Configuration: Each module registers `AddModuleDbContext<DbContextType>(config, moduleName)`
- Host: `HostDbContext` aggregates all module contexts for migrations

---

*Architecture analysis: 2026-03-18*
