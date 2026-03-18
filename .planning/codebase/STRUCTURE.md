# Codebase Structure

**Analysis Date:** 2026-03-18

## Directory Layout

```
SimpleModule/
├── .claude/                        # Claude Code configuration and skills
├── .github/                        # GitHub workflows (CI/CD)
├── .planning/                      # Generated GSD planning documents
│   └── codebase/                   # Architecture analysis (STACK.md, TESTING.md, etc.)
├── cli/                            # CLI tool for scaffolding
│   └── SimpleModule.Cli/
├── docs/                           # Documentation
├── framework/                      # Core framework (non-negotiable, shared by all)
│   ├── SimpleModule.Core/          # Interfaces, attributes, utilities
│   ├── SimpleModule.Blazor/        # Inertia + Blazor SSR integration
│   ├── SimpleModule.Database/      # EF Core multi-tenancy helpers
│   └── SimpleModule.Generator/     # Roslyn source generator
├── modules/                        # Feature modules (independently deployable)
│   ├── Dashboard/
│   ├── Orders/
│   ├── Products/
│   └── Users/
├── packages/                       # Shared npm packages
│   ├── SimpleModule.Client/        # Page resolver, Vite plugin
│   ├── SimpleModule.UI/            # Radix UI component library
│   └── SimpleModule.Theme.Default/ # Tailwind theme
├── template/                       # Host application
│   └── SimpleModule.Host/          # Blazor SSR host, serves all modules
└── tools/                          # Build and utility scripts
```

## Directory Purposes

**framework/SimpleModule.Core:**
- Purpose: Core interfaces, attributes, and utilities that all code depends on
- Contains: `IModule`, `IEndpoint`, `IViewEndpoint`, `[Module]`, `[Dto]`, `[RequirePermission]` attributes, Inertia helpers, event bus, authorization, menu system, exception handling, validation
- Key files:
  - `IModule.cs` — module configuration contract
  - `IEndpoint.cs` — minimal API contract
  - `DtoAttribute.cs` — marks types for code generation
  - `Inertia/InertiaResult.cs` — page rendering
  - `Inertia/InertiaMiddleware.cs` — protocol version negotiation
  - `Events/EventBus.cs` — pub-sub with isolated failures
  - `Authorization/` — permission system, authorization handler

**framework/SimpleModule.Blazor:**
- Purpose: Bridge between Blazor SSR and Inertia.js, renders HTML shell
- Contains: `IInertiaPageRenderer` implementation using Blazor's `HtmlRenderer`
- Key files:
  - `Inertia/InertiaPageRenderer.cs` — SSR rendering via HtmlRenderer
  - `ServiceCollectionExtensions.cs` — DI registration

**framework/SimpleModule.Database:**
- Purpose: Entity Framework Core multi-tenancy utilities, database schema isolation
- Contains: Database provider detection (SQLite, PostgreSQL, SQL Server), ModuleDbContext registration, table prefix/schema mapping
- Key files:
  - `ModuleDbContextInfo.cs` — metadata for each module's DbContext
  - `DatabaseProviderDetector.cs` — determines which DB provider is used

**framework/SimpleModule.Generator:**
- Purpose: Compile-time Roslyn source generator for AOT initialization
- Contains: `IIncrementalGenerator` implementation that discovers modules, endpoints, menus, DTOs
- Key files:
  - `ModuleDiscovererGenerator.cs` — main generator entry point
  - `Discovery/SymbolDiscovery.cs` — reflection-free symbol scanning
  - `Emitters/` — code generation for each aspect (modules, endpoints, menus, TypeScript)
- Emits: Extension methods for Host's `Program.cs` to call
- Targets: netstandard2.0, runs during Host project build

**modules/{ModuleName}/src/{ModuleName}:**
- Purpose: Feature implementation (API, database, services, views)
- Contains:
  - `*Module.cs` — IModule implementation with configuration
  - `Endpoints/` — API route handlers implementing IEndpoint
  - `Views/` — Inertia route handlers implementing IViewEndpoint (maps to React pages)
  - `Entities/` — EF Core models and DbContext
  - `Services/` — Business logic implementing *Contracts interface
  - `Pages/index.ts` — React component exports (auto-generated, maps to Vite build output)
  - `package.json` — declares React/Inertia/Tailwind as peerDependencies
  - `vite.config.ts` — builds to library mode, outputs `{ModuleName}.pages.js`
  - `wwwroot/` — built assets, served at `/_content/{ModuleName}/`

**modules/{ModuleName}/src/{ModuleName}.Contracts:**
- Purpose: Public service interface and shared DTOs for cross-module use
- Contains:
  - `I<ModuleName>Contracts.cs` — public service interface
  - DTO types with `[Dto]` attribute
  - Value objects (strongly-typed IDs)
- Pattern: Other modules depend on Contracts project, never implementation

**modules/{ModuleName}/tests/{ModuleName}.Tests:**
- Purpose: Unit and integration tests
- Contains: xUnit test classes, test fixtures, integration test setup
- Database: SQLite in-memory for unit tests, PostgreSQL for CI integration tests

**packages/SimpleModule.Client:**
- Purpose: Page resolution and Vite plugin for shared React dependencies
- Key files:
  - `resolve-page.ts` — dynamically imports `/_content/{ModuleName}/{ModuleName}.pages.js`
  - `vite-plugin-vendor.ts` — builds React/Inertia as shared vendor bundles

**packages/SimpleModule.UI:**
- Purpose: Radix UI-based component library with Tailwind styling
- Contains: Button, Dialog, Dropdown, Table, Form inputs, etc.
- Import pattern: `import { Button } from '@simplemodule/ui/components'`

**packages/SimpleModule.Theme.Default:**
- Purpose: Tailwind CSS base theme and typography
- Contains: Color palette, spacing scale, typography, Tailwind config

**template/SimpleModule.Host:**
- Purpose: Host application that orchestrates all modules
- Key files:
  - `Program.cs` — startup configuration, calls generated extension methods
  - `ClientApp/app.tsx` — React Inertia bootstrap
  - `Components/App.razor` — Blazor layout component
  - `Components/InertiaShell.razor` — HTML shell that embeds page JSON
  - `Styles/` — global CSS
  - `Migrations/` — EF Core migrations
  - `wwwroot/` — static assets, module assets served at `/_content/`

**cli/SimpleModule.Cli:**
- Purpose: CLI tools for scaffolding and validation
- Commands:
  - `sm new project` — scaffolds new SimpleModule solution
  - `sm new module <name>` — creates module with all required files
  - `sm new feature <name>` — adds feature to existing module
  - `sm doctor` — validates project structure

## Key File Locations

**Entry Points:**

- `template/SimpleModule.Host/Program.cs` — Host startup, DI configuration, endpoint mapping
- `modules/{Module}/{Module}Module.cs` — Module configuration (services, endpoints, menu, permissions)
- `modules/{Module}/src/{Module}/Endpoints/**/*Endpoint.cs` — API route handlers
- `modules/{Module}/src/{Module}/Views/**/*Endpoint.cs` — Inertia page handlers
- `template/SimpleModule.Host/ClientApp/app.tsx` — React app bootstrap

**Configuration:**

- `template/SimpleModule.Host/appsettings.json` — Host configuration (database, OpenIddict, etc.)
- `modules/{Module}/src/{Module}/package.json` — Module dependencies
- `framework/SimpleModule.Generator/ModuleDiscovererGenerator.cs` — Generator configuration
- `.editorconfig` — Coding style (enforced by Roslyn analyzers)
- `Directory.Build.props` — Global build settings (AOT, TreatWarningsAsErrors, etc.)

**Core Logic:**

- `modules/{Module}/src/{Module}/Services/*Service.cs` — Business logic implementing `I{Module}Contracts`
- `modules/{Module}/src/{Module}/Entities/*DbContext.cs` — EF Core models and DbContext
- `modules/{Module}/src/{Module}.Contracts/I{Module}Contracts.cs` — Public service interface
- `framework/SimpleModule.Core/Events/` — Event bus and event handlers

**Testing:**

- `modules/{Module}/tests/{Module}.Tests/` — Test suite for module
- `tests/SimpleModule.Tests.Shared/` — Test utilities, factories, WebApplicationFactory
- `modules/{Module}/**/*.Test.cs` or `*.Spec.cs` — Test files (co-located with source)

**Frontend:**

- `modules/{Module}/src/{Module}/Views/*.tsx` — React page components
- `modules/{Module}/src/{Module}/Pages/index.ts` — Component registry (auto-generated)
- `template/SimpleModule.Host/ClientApp/types/` — Generated TypeScript interfaces (from `[Dto]`)
- `packages/SimpleModule.UI/components/` — Reusable UI components

## Naming Conventions

**Files:**

- C# source: PascalCase (`ProductService.cs`, `CreateEndpoint.cs`)
- React components: PascalCase (`Browse.tsx`, `ProductForm.tsx`)
- Test files: Match source with `.Test.cs` or `.Spec.cs` suffix (e.g., `ProductService.Test.cs`)
- Config files: kebab-case (`vite.config.ts`, `tsconfig.json`)

**Directories:**

- Framework: PascalCase (`SimpleModule.Core`, `SimpleModule.Blazor`)
- Modules: PascalCase (`Products`, `Orders`)
- Functional groups: PascalCase (`Endpoints`, `Views`, `Services`, `Entities`, `Pages`)
- Feature-based endpoints: Group by entity (e.g., `Endpoints/Products/`, not `Endpoints/Get/`, `Endpoints/Post/`)

**Classes and Interfaces:**

- Interfaces: `IPrefixName` (e.g., `IProductContracts`, `IModule`, `IEndpoint`)
- Implementations: `Name` without prefix (e.g., `ProductService`, `ProductsModule`)
- Attributes: Suffix with `Attribute` (e.g., `[Module]`, `[Dto]`, `[RequirePermission]`)
- Enums/Constants: PascalCase (e.g., `ProductsConstants`, `ProductsPermissions`)

**Methods and Variables:**

- Public methods: PascalCase (`GetAllProductsAsync()`)
- Private fields: camelCase prefixed with underscore (`_productService`)
- Local variables: camelCase (`products`, `request`)
- Async methods: Suffix with `Async` (`GetProductByIdAsync()`)

**Database:**

- Tables: Snake_case or module-prefixed
  - SQLite: `products_product`, `orders_order`
  - PostgreSQL: Schema `products.product`, `orders.order`
- Columns: snake_case (`product_id`, `created_at`)

**Frontend:**

- React components: PascalCase (`Browse.tsx`)
- Hooks: camelCase prefixed with `use` (`useProductForm()`)
- Utility functions: camelCase (`formatDate()`)
- Constants: UPPER_SNAKE_CASE (`API_BASE_URL`)
- CSS classes: kebab-case (via Tailwind utility classes)

## Where to Add New Code

**New Feature (Endpoint + View):**

1. API endpoint:
   - Location: `modules/{Module}/src/{Module}/Endpoints/{Entity}/{Action}Endpoint.cs`
   - Implement `IEndpoint`
   - Add `[RequirePermission(...)]` attribute if needed
   - Example: `modules/Products/src/Products/Endpoints/Products/GetByIdEndpoint.cs`

2. View endpoint:
   - Location: `modules/{Module}/src/{Module}/Views/{Action}Endpoint.cs`
   - Implement `IViewEndpoint`
   - Return `Inertia.Render("Products/ComponentName", props)`
   - Example: `modules/Products/src/Products/Views/BrowseEndpoint.cs`

3. React component:
   - Location: `modules/{Module}/src/{Module}/Views/{ComponentName}.tsx`
   - Accept props from server
   - Register in `Pages/index.ts` (auto-generated)
   - Example: `modules/Products/src/Products/Views/Browse.tsx`

4. Service method:
   - Location: `modules/{Module}/src/{Module}/Services/{Module}Service.cs`
   - Add method to `I{Module}Contracts` interface
   - Implement in service class

**New Module:**

1. Use CLI: `sm new module ProductReviews`
2. Or manually create:
   - `modules/ProductReviews/src/ProductReviews.Contracts/` (public interface + DTOs)
   - `modules/ProductReviews/src/ProductReviews/` (implementation)
   - `modules/ProductReviews/src/ProductReviews/{ProductReviews}Module.cs` (IModule)
   - `modules/ProductReviews/tests/ProductReviews.Tests/` (tests)

**New Shared Component:**

1. Location: `packages/SimpleModule.UI/components/{ComponentName}.tsx`
2. Export from `packages/SimpleModule.UI/components/index.ts`
3. Import in modules: `import { ComponentName } from '@simplemodule/ui/components'`

**New Utility or Hook:**

- Shared React hook: `packages/SimpleModule.Client/src/hooks/{hookName}.ts`
- Shared util function: `packages/SimpleModule.Client/src/utils/{utilName}.ts`
- Module-specific helper: `modules/{Module}/src/{Module}/lib/{helperName}.ts`

**New Permission:**

1. Define in `modules/{Module}/src/{Module}/{Module}Permissions.cs`:
   ```csharp
   public static class ProductsPermissions
   {
       public const string View = "products:view";
       public const string Create = "products:create";
   }
   ```

2. Register in module's `ConfigurePermissions()`:
   ```csharp
   builder.AddPermissions<ProductsPermissions>();
   ```

3. Apply to endpoint:
   ```csharp
   [RequirePermission(ProductsPermissions.Create)]
   public class CreateEndpoint : IEndpoint { ... }
   ```

**New Database Migration:**

1. Add/modify models in module's `Entities/` folder
2. Module manages its own `DbContext`
3. Use EF Core migrations per module for production
4. Tests use `Database.EnsureCreatedAsync()`

## Special Directories

**wwwroot/ (Module Level):**
- Purpose: Static assets served at `/_content/{ModuleName}/`
- Generated: Module's Vite build outputs `{ModuleName}.pages.js` here
- Committed: No; built during module Vite build
- Cached: Via `?v={cache-buster}` query param, immutable for 1 year

**wwwroot/ (Host Level):**
- Purpose: Global static assets, vendor bundles, module assets
- Contains: `css/app.css`, `js/vendor/` (React, Inertia, etc.), `js/shell.js`
- Committed: `shell.js` and theme CSS yes; vendor bundles generated during build
- Structure: `_content/` serves module assets, `/css/` and `/js/` serve host assets

**obj/ and bin/:**
- Purpose: Build artifacts
- Generated: Yes
- Committed: No (in `.gitignore`)

**\.planning/codebase/:**
- Purpose: GSD-generated architecture documentation
- Contains: `ARCHITECTURE.md`, `STRUCTURE.md`, `STACK.md`, `TESTING.md`, `CONVENTIONS.md`, `CONCERNS.md`
- Generated: Yes (by `/gsd:map-codebase`)
- Committed: Yes (tracked in git)

**migrations/ and Migrations/:**
- Purpose: EF Core migration files
- Generated: Via `dotnet ef migrations add`
- Committed: Yes, for production deployments
- Per-module: Each module can have its own migrations directory

---

*Structure analysis: 2026-03-18*
