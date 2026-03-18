# Codebase Structure

**Analysis Date:** 2026-03-18

## Directory Layout

```
SimpleModule/
├── framework/                          # Core abstractions and framework packages
│   ├── SimpleModule.Core/              # Interfaces, attributes, shared abstractions
│   ├── SimpleModule.Blazor/            # Blazor SSR integration (Inertia renderer)
│   ├── SimpleModule.Database/          # Multi-provider EF Core helpers
│   └── SimpleModule.Generator/         # Roslyn incremental source generator
├── modules/                            # Feature modules (isolated by domain)
│   ├── Products/                       # Product module
│   │   ├── src/
│   │   │   ├── Products.Contracts/     # Public API + DTOs
│   │   │   └── Products/               # Implementation
│   │   └── tests/
│   │       └── Products.Tests/         # xUnit tests
│   ├── Orders/                         # Order module
│   │   ├── src/
│   │   │   ├── Orders.Contracts/
│   │   │   └── Orders/
│   │   └── tests/
│   │       └── Orders.Tests/
│   ├── Users/                          # User/Auth module (OpenIddict, Identity)
│   │   ├── src/
│   │   │   ├── Users.Contracts/
│   │   │   └── Users/
│   │   └── tests/
│   │       └── Users.Tests/
│   └── Dashboard/                      # Dashboard module
│       ├── src/
│       │   └── Dashboard/
│       └── tests/ (if applicable)
├── template/
│   └── SimpleModule.Host/              # Host/shell application (PublishAot)
│       ├── Program.cs                  # DI wiring, middleware setup
│       ├── Components/                 # Blazor components (shell, layouts)
│       ├── ClientApp/                  # React + Inertia bootstrap
│       │   ├── app.tsx                 # Inertia app initialization
│       │   ├── vite.config.ts
│       │   └── package.json
│       └── wwwroot/                    # Static assets, vendored JS
├── packages/                           # Shared npm/npm workspaces
│   ├── @simplemodule/client            # Vite plugin, page resolution
│   ├── @simplemodule/ui                # Radix UI wrappers + Tailwind theme
│   └── @simplemodule/theme-default     # Tailwind CSS configuration
├── cli/
│   └── SimpleModule.Cli/               # CLI tool (sm command)
├── tests/                              # Framework-level tests
│   ├── SimpleModule.Core.Tests/
│   ├── SimpleModule.Cli.Tests/
│   └── SimpleModule.Tests.Shared/      # Shared test infrastructure
├── .aspire/                            # .NET Aspire orchestration
├── .planning/                          # GSD planning docs
└── docs/                               # Implementation plans, roadmaps
```

## Directory Purposes

**framework/SimpleModule.Core/**
- Purpose: Core interfaces and attributes for all modules
- Contains: `IModule`, `IEndpoint`, `IViewEndpoint`, `[Module]` and `[Dto]` attributes, event bus, authorization, menu system, Inertia integration
- Key files: `IModule.cs`, `IEndpoint.cs`, `ModuleAttribute.cs`, `DtoAttribute.cs`, `Events/`, `Authorization/`, `Menu/`, `Inertia/`

**framework/SimpleModule.Blazor/**
- Purpose: Blazor SSR integration; renders Inertia HTML shells
- Contains: `InertiaShell.razor` (layout template), `InertiaPage.razor` (page component), `InertiaPageRenderer.cs` (service that renders Blazor → HTML)
- Key files: `Components/InertiaShell.razor`, `Inertia/InertiaPageRenderer.cs`, `ServiceCollectionExtensions.cs`

**framework/SimpleModule.Database/**
- Purpose: Entity Framework Core multi-provider helpers
- Contains: `DbContextOptions`, provider detection (SQLite/PostgreSQL/SQL Server), module schema isolation, health checks
- Key files: `ModuleDbContextInfo.cs`, `DatabaseOptions.cs`, `DatabaseProvider.cs`, `ModuleModelBuilderExtensions.cs`

**framework/SimpleModule.Generator/**
- Purpose: Roslyn IIncrementalGenerator that discovers modules, endpoints, DTOs at compile-time
- Contains: Discovery logic (finds `[Module]`, `IEndpoint`, `[Dto]` types), emitters (generates extension methods, TypeScript definitions, page routing)
- Key files: `Discovery/SymbolDiscovery.cs`, `Emitters/ModuleExtensionsEmitter.cs`, `Emitters/EndpointExtensionsEmitter.cs`, `Emitters/TypeScriptDefinitionsEmitter.cs`, `Emitters/ViewPagesEmitter.cs`

**modules/{ModuleName}/src/{ModuleName}.Contracts/**
- Purpose: Public API boundary; contracts are always referenced, never implementations
- Contains: `I{ModuleName}Contracts` interface, `[Dto]`-marked types, strongly-typed value objects (e.g., `ProductId`)
- Key files: `I{ModuleName}Contracts.cs`, DTO classes with `[Dto]`, value object types

**modules/{ModuleName}/src/{ModuleName}/**
- Purpose: Module implementation; endpoints, services, database, event handlers, permissions
- Contains: `{ModuleName}Module.cs`, `Endpoints/`, `Views/`, `Services/`, `{ModuleName}DbContext.cs`, `{ModuleName}Constants.cs`, `{ModuleName}Permissions.cs`, `Pages/index.ts` (generated from view endpoints)
- Key subdirectories:
  - `Endpoints/` — API endpoints (implement `IEndpoint`), organized by entity/feature
  - `Views/` — View endpoints (implement `IViewEndpoint`), return Inertia pages
  - `Services/` — Business logic; often implement contracts from `.Contracts`
  - `Pages/` — React components (TSX/TSX)
  - `EntityConfigurations/` — EF Core `IEntityTypeConfiguration<T>` implementations
  - `Validators/` — FluentValidation validators for requests
  - `Handlers/` — Event handlers (implement `IEventHandler<T>`)

**modules/{ModuleName}/tests/{ModuleName}.Tests/**
- Purpose: xUnit tests for module
- Contains: Endpoint tests, service tests, integration tests
- Uses: `SimpleModule.Tests.Shared` for `SimpleModuleWebApplicationFactory`, test claims

**template/SimpleModule.Host/**
- Purpose: Host application; orchestrates all modules; served as `PublishAot` binary
- Contains: `Program.cs` (DI wiring, middleware), Blazor layouts, exception handlers, authentication setup
- Key files: `Program.cs`, `Components/Layout/MainLayout.razor`, `Components/InertiaShell.razor` (shell template), `ClientApp/` (React bootstrap)

**template/SimpleModule.Host/ClientApp/**
- Purpose: React + Inertia.js bootstrap; dynamically resolves pages from module bundles
- Contains: `app.tsx` (Inertia initialization), error handling, Vite configuration
- Key files: `app.tsx`, `vite.config.ts`

**packages/@simplemodule/client/**
- Purpose: npm package with Inertia utilities and Vite plugin for vendor bundling
- Contains: `resolvePage()` function (dynamic imports module pages), Vite plugin config
- Used by: `app.tsx` for page resolution, Host `vite.config.ts` for vendoring

**packages/@simplemodule/ui/**
- Purpose: Shared UI component library (Radix UI wrappers with Tailwind styling)
- Contains: `components/` (Card, Button, Input, etc.), `lib/utils.ts` (cn, mergeClasses)
- Used by: Module React components via `import { Card } from '@simplemodule/ui'`

**packages/@simplemodule/theme-default/**
- Purpose: Tailwind CSS configuration, design tokens
- Contains: Tailwind config, theme colors, typography
- Used by: All modules and ClientApp via `tailwind.config.ts` extends

**cli/SimpleModule.Cli/**
- Purpose: CLI tool (`sm` command) for scaffolding projects and modules
- Contains: Commands (`new project`, `new module`, `new feature`, `doctor`), templates for generating module structure
- Key directories: `Commands/`, `Templates/`

**tests/SimpleModule.Tests.Shared/**
- Purpose: Shared test infrastructure for all module tests
- Contains: `SimpleModuleWebApplicationFactory` (in-memory SQLite, test auth claims), `FakeDataGenerators` (Bogus fakers)
- Key files: `SimpleModuleWebApplicationFactory.cs`, `FakeDataGenerators.cs`

## Key File Locations

**Entry Points:**
- `template/SimpleModule.Host/Program.cs` — Host startup; calls generated `AddModules()` and `MapModuleEndpoints()`
- `modules/{ModuleName}/src/{ModuleName}/{ModuleName}Module.cs` — Module class marked with `[Module(...)]`; discovered by generator
- `template/SimpleModule.Host/ClientApp/app.tsx` — React/Inertia bootstrap; runs in browser

**Configuration:**
- `template/SimpleModule.Host/Program.cs` — DI setup, authentication, middleware ordering
- `.editorconfig` — C# coding style rules (file-scoped namespaces, naming conventions, etc.)
- `biome.json` — JavaScript/TypeScript linting and formatting
- `Directory.Build.props` — Global MSBuild properties (warnings as errors, analysis level)

**Core Logic:**
- `framework/SimpleModule.Core/IModule.cs` — Module interface contract
- `framework/SimpleModule.Core/IEndpoint.cs` — Endpoint interface contract
- `framework/SimpleModule.Core/Events/EventBus.cs` — Event publication
- `framework/SimpleModule.Core/Authorization/PermissionRegistry.cs` — Permission storage and lookup
- `modules/{ModuleName}/src/{ModuleName}/Services/` — Business logic

**Testing:**
- `modules/{ModuleName}/tests/{ModuleName}.Tests/` — All module tests
- `tests/SimpleModule.Tests.Shared/SimpleModuleWebApplicationFactory.cs` — Test infrastructure
- `tests/SimpleModule.Core.Tests/` — Framework-level tests

## Naming Conventions

**Files:**
- `{EntityName}Module.cs` — Module class (singular, PascalCase)
- `{ActionName}Endpoint.cs` — Endpoint classes (verb-noun, e.g., `GetAllEndpoint`)
- `{EntityName}DbContext.cs` — Entity Framework context
- `{EntityName}Service.cs` — Service implementing `I{EntityName}Contracts`
- `I{EntityName}Contracts.cs` — Public contract interface in `.Contracts` projects
- `{EntityName}Tests.cs` — xUnit test class
- `{EntityName}.tsx` — React component (PascalCase)
- `{name}.ts` or `{name}.tsx` — Generalized TypeScript/TSX files

**Directories:**
- `Endpoints/{EntityName}/` — Group endpoints by entity (e.g., `Endpoints/Products/`)
- `Views/` — View/page endpoints (separate from API endpoints)
- `Pages/` — React component files; also contains generated `index.ts` mapping
- `Services/` — Service implementations
- `EntityConfigurations/` — EF Core entity configs
- `Handlers/` — Event handlers

**Code Symbols:**
- `Interfaces` — `IXxxX` (e.g., `IProductContracts`, `IEventBus`)
- `Classes/Records` — `PascalCase` (e.g., `Product`, `ProductsModule`)
- `Methods/Properties` — `PascalCase` (e.g., `GetAllProductsAsync`, `CreateProductAsync`)
- `Private fields` — `_camelCase` (e.g., `_logger`, `_productService`)
- `Local variables/parameters` — `camelCase` (e.g., `productId`, `request`)
- `Constants` — `PascalCase` (e.g., `ModuleName`, `RoutePrefix`)

## Where to Add New Code

**New Feature (within existing module):**
- Endpoint handler: `modules/{ModuleName}/src/{ModuleName}/Endpoints/{EntityName}/{ActionName}Endpoint.cs`
- Service method: Add to `modules/{ModuleName}/src/{ModuleName}/Services/{EntityName}Service.cs`
- API DTO: Add to `modules/{ModuleName}/src/{ModuleName}.Contracts/` (mark with `[Dto]`)
- Test: `modules/{ModuleName}/tests/{ModuleName}.Tests/{ActionName}Tests.cs`

**New Module:**
- Use CLI: `sm new module <Name>`
- Or manually create:
  - `modules/{Name}/src/{Name}.Contracts/` — I{Name}Contracts.cs, DTOs
  - `modules/{Name}/src/{Name}/` — {Name}Module.cs, Endpoints/, Views/, Services/, DbContext
  - `modules/{Name}/tests/{Name}.Tests/` — xUnit test project
  - Add project references to Host and `.slnx`

**New React Component:**
- Shared component: `packages/@simplemodule/ui/src/components/{ComponentName}.tsx`
- Module-specific page: `modules/{ModuleName}/src/{ModuleName}/Views/{PageName}.tsx`
- Mount in `Pages/index.ts` export mapping

**New Utility/Helper:**
- C# shared logic (used by multiple modules): `framework/SimpleModule.Core/{Area}/` (e.g., `Validation/`)
- C# module-internal helper: `modules/{ModuleName}/src/{ModuleName}/{HelperName}.cs`
- TypeScript shared utility: `packages/@simplemodule/client/src/lib/` or `packages/@simplemodule/ui/src/lib/`

## Special Directories

**framework/SimpleModule.Generator/**
- Purpose: Source generator for compile-time module discovery and code emission
- Generated: No—this is the generator itself
- Committed: Yes
- Generated outputs appear in `obj/Generated/` at build time (extension methods like `AddModules.g.cs`, `MapModuleEndpoints.g.cs`)

**modules/{ModuleName}/src/{ModuleName}/Pages/index.ts**
- Purpose: Maps view endpoint route names to React components
- Generated: Yes—emitted by `ViewPagesEmitter` in source generator
- Committed: No (generated at build time from view endpoints)
- Pattern: `export const pages: Record<string, any> = { "ModuleName/ViewName": Component, ... }`

**template/SimpleModule.Host/wwwroot/**
- Purpose: Static assets; vendored JavaScript libraries (React, Inertia, etc.)
- Generated: Partially (vendor JS copied by Host build, Vite)
- Committed: No (`/js/vendor/` is gitignored; built at runtime)

**obj/ and bin/ directories**
- Purpose: Build outputs and generated code
- Generated: Yes
- Committed: No (gitignored)

---

*Structure analysis: 2026-03-18*
