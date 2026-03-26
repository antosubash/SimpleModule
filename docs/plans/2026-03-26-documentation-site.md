# Documentation Site Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a VitePress-powered documentation site for SimpleModule framework users, hosted at docs.simplemodule.dev.

**Architecture:** VitePress docs site in `docs/site/` as an npm workspace. Markdown content organized into getting-started, guide, frontend, cli, testing, advanced, and reference sections. VitePress config handles nav, sidebar, search, and theme.

**Tech Stack:** VitePress 2.x, Node.js, Markdown

---

### Task 1: Scaffold VitePress project

**Files:**
- Create: `docs/site/package.json`
- Create: `docs/site/.vitepress/config.ts`
- Create: `docs/site/index.md`
- Modify: `package.json` (add workspace)
- Modify: `biome.json` (add docs to includes)

**Step 1: Create package.json for docs site**

Create `docs/site/package.json`:
```json
{
  "name": "docs",
  "private": true,
  "scripts": {
    "dev": "vitepress dev",
    "build": "vitepress build",
    "preview": "vitepress preview"
  },
  "devDependencies": {
    "vitepress": "^2.0.0"
  }
}
```

**Step 2: Add docs/site workspace to root package.json**

In root `package.json`, add `"docs/site"` to the `workspaces` array.

**Step 3: Add docs to biome.json includes**

In `biome.json`, add `"docs/**"` to the `files.includes` array.

**Step 4: Create VitePress config**

Create `docs/site/.vitepress/config.ts` with:
- Site title: "SimpleModule"
- Description: "Modular monolith framework for .NET"
- Navigation: Getting Started, Guide, Frontend, CLI, Testing, Advanced, Reference
- Sidebar: grouped by section with all pages
- Social links: GitHub repo
- Search: local (miniSearch)
- Edit link: GitHub source
- Dark/light mode (default)

**Step 5: Create landing page**

Create `docs/site/index.md` with VitePress hero layout:
- Title: SimpleModule
- Tagline: Modular monolith framework for .NET with compile-time module discovery
- Actions: Get Started, View on GitHub
- Features: Compile-time Discovery, React + Inertia.js, Module Isolation, CLI Tooling

**Step 6: Install dependencies**

Run: `npm install`

**Step 7: Verify dev server starts**

Run: `cd docs/site && npx vitepress dev --port 5173`
Expected: VitePress dev server running with hero landing page

---

### Task 2: Getting Started section

**Files:**
- Create: `docs/site/getting-started/introduction.md`
- Create: `docs/site/getting-started/quick-start.md`
- Create: `docs/site/getting-started/project-structure.md`

**Step 1: Write introduction.md**

Cover:
- What is SimpleModule (modular monolith framework for .NET)
- Why modular monoliths (vs microservices, vs traditional monolith)
- Key features: compile-time discovery, React+Inertia frontend, module isolation, CLI tooling
- How it works (high-level): Module attribute → source generator → auto-registration
- When to use SimpleModule

**Step 2: Write quick-start.md**

Cover:
- Prerequisites (.NET 10 SDK, Node.js)
- `sm new project MyApp` to scaffold
- `dotnet build && npm install`
- `npm run dev` to start development
- Creating first module: `sm new module Products`
- Creating first feature: `sm new feature Products/Browse`
- Verifying it works in browser at https://localhost:5001

**Step 3: Write project-structure.md**

Cover:
- Directory layout explanation (framework/, modules/, packages/, template/, cli/, tests/)
- What each framework project does (Core, Generator, Database, Blazor, Hosting)
- Module structure pattern (src/Name, src/Name.Contracts, tests/Name.Tests)
- Frontend packages (@simplemodule/client, @simplemodule/ui, @simplemodule/theme-default)
- Host app and how it wires everything together
- Solution file and build configuration

---

### Task 3: Guide section — Core concepts

**Files:**
- Create: `docs/site/guide/modules.md`
- Create: `docs/site/guide/endpoints.md`
- Create: `docs/site/guide/contracts.md`
- Create: `docs/site/guide/database.md`

**Step 1: Write modules.md**

Cover:
- What is a module (self-contained feature unit)
- The `IModule` interface and its virtual methods
- The `[Module]` attribute: Name, Version, RoutePrefix, ViewPrefix
- Module lifecycle: ConfigureServices → ConfigureMiddleware → ConfigureEndpoints → ConfigureMenu → ConfigurePermissions → ConfigureSettings
- How discovery works (source generator scans at compile time)
- Generated code: `AddModules()`, `MapModuleEndpoints()`, `CollectModuleMenuItems()`
- Creating a module manually vs `sm new module`
- Example: ProductsModule walkthrough

**Step 2: Write endpoints.md**

Cover:
- `IEndpoint` interface for API endpoints
- `IViewEndpoint` interface for Inertia view endpoints
- The `Map(IEndpointRouteBuilder app)` method
- Auto-discovery: source generator finds all implementors
- Parameter binding rules (GET/HEAD/OPTIONS/DELETE vs POST/PUT/PATCH)
- Implicit vs explicit binding (`[FromForm]`, `[FromBody]`, `[FromQuery]`, etc.)
- Form binding limitations (List<string> workaround)
- Example: CRUD endpoints for Products

**Step 3: Write contracts.md**

Cover:
- Why contracts: module isolation, no direct dependencies
- Contracts project structure (Name.Contracts.csproj)
- `I<Name>Contracts` interface pattern
- `[Dto]` attribute for shared types
- TypeScript generation from DTOs
- Cross-module dependency: depend on contracts, never implementations
- Example: Orders depending on IProductContracts

**Step 4: Write database.md**

Cover:
- Multi-provider support: SQLite, PostgreSQL, SQL Server
- Schema isolation: table prefixes (SQLite) vs schemas (PostgreSQL/SQL Server)
- Module DbContext pattern: one DbContext per module
- `ModuleDbContextInfo` registration
- EF Core interceptor DI pattern (resolve at interception time, not constructor)
- Entity configurations
- Development: `EnsureCreated()` vs production migrations
- Example: ProductsDbContext

---

### Task 4: Guide section — Communication & cross-cutting

**Files:**
- Create: `docs/site/guide/events.md`
- Create: `docs/site/guide/permissions.md`
- Create: `docs/site/guide/menus.md`
- Create: `docs/site/guide/settings.md`
- Create: `docs/site/guide/inertia.md`

**Step 1: Write events.md**

Cover:
- Event bus overview: `IEventBus.PublishAsync<T>()`
- `IEvent` marker interface
- `IEventHandler<T>` implementation
- Partial success semantics: all handlers run, exceptions collected
- `AggregateException` behavior
- Handler best practices: stateless, independent, idempotent, no long-running work
- Exception handling pattern (try/catch in handlers for non-critical work)
- Testing partial failure scenarios

**Step 2: Write permissions.md**

Cover:
- Permission system overview
- `ConfigurePermissions(PermissionRegistryBuilder builder)`
- `[RequirePermission("name")]` attribute
- Policy-based authorization via claims
- Defining module permissions
- Checking permissions in endpoints

**Step 3: Write menus.md**

Cover:
- `IMenuRegistry` and `IMenuBuilder`
- `ConfigureMenu(IMenuBuilder menus)` on module
- Adding menu items with labels, icons, routes
- Menu ordering and grouping
- How the frontend renders menus

**Step 4: Write settings.md**

Cover:
- Module settings infrastructure
- `ConfigureSettings(ISettingsBuilder settings)` on module
- Defining and accessing settings
- Settings storage and retrieval

**Step 5: Write inertia.md**

Cover:
- What is Inertia.js and how SimpleModule uses it
- Request flow: ASP.NET → Blazor SSR → React hydration
- `Inertia.Render("Module/Page", props)` call
- How props are serialized and delivered
- Shared data and partial reloads
- Error handling (404, 500 responses)

---

### Task 5: Frontend section

**Files:**
- Create: `docs/site/frontend/overview.md`
- Create: `docs/site/frontend/pages.md`
- Create: `docs/site/frontend/components.md`
- Create: `docs/site/frontend/styling.md`
- Create: `docs/site/frontend/vite.md`

**Step 1: Write overview.md**

Cover:
- Architecture: React 19 + Inertia.js + Blazor SSR
- How module frontends work: each module builds pages.js via Vite library mode
- ClientApp as the Inertia bootstrap
- Dynamic page resolution by route name
- Type safety via generated TypeScript interfaces

**Step 2: Write pages.md**

Cover:
- Pages registry pattern: `Pages/index.ts`
- The `pages` record mapping route names to lazy imports
- Why this is critical: missing entry = silent 404
- `validate-pages` script and CI integration
- Adding new pages: match `Inertia.Render("Name/Page")` key
- Example: Products module pages

**Step 3: Write components.md**

Cover:
- @simplemodule/ui package
- Radix UI + Tailwind wrappers
- Importing: `@simplemodule/ui/components`, `@simplemodule/ui/lib/utils`
- Available components
- Adding new components: `npm run ui:add`
- Component registry

**Step 4: Write styling.md**

Cover:
- Tailwind CSS 4.x configuration
- @simplemodule/theme-default package
- Theme customization
- Dark mode support
- Module-specific styles
- Global styles in template/SimpleModule.Host/Styles/

**Step 5: Write vite.md**

Cover:
- Vite library mode for modules
- Module vite.config.ts pattern
- Externalizing React, React-DOM, @inertiajs/react
- Development workflow: `npm run dev` orchestrator
- Build modes: dev (unminified) vs prod (minified)
- Build orchestrator and watch mode
- Source maps in development

---

### Task 6: CLI section

**Files:**
- Create: `docs/site/cli/overview.md`
- Create: `docs/site/cli/new-project.md`
- Create: `docs/site/cli/new-module.md`
- Create: `docs/site/cli/new-feature.md`
- Create: `docs/site/cli/doctor.md`

**Step 1: Write overview.md**

Cover:
- The `sm` command: what it is, how to install
- Available commands summary table
- Global options

**Step 2: Write new-project.md**

Cover: `sm new project` — what it scaffolds, options, example output

**Step 3: Write new-module.md**

Cover: `sm new module <name>` — what it creates, project structure, options, example

**Step 4: Write new-feature.md**

Cover: `sm new feature <name>` — what it adds, options, example

**Step 5: Write doctor.md**

Cover: `sm doctor [--fix]` — what it validates, auto-fix behavior, common issues

---

### Task 7: Testing section

**Files:**
- Create: `docs/site/testing/overview.md`
- Create: `docs/site/testing/unit-tests.md`
- Create: `docs/site/testing/integration-tests.md`
- Create: `docs/site/testing/e2e-tests.md`

**Step 1: Write overview.md**

Cover:
- Test stack: xUnit.v3, FluentAssertions, Bogus, NSubstitute
- Test project conventions: `tests/<Name>.Tests/`
- Running tests: `dotnet test` commands
- CI strategy: SQLite + PostgreSQL
- Test naming: `Method_Scenario_Expected`

**Step 2: Write unit-tests.md**

Cover:
- Unit test patterns
- FakeDataGenerators (Bogus)
- Mocking with NSubstitute
- Testing event handlers
- Testing services in isolation

**Step 3: Write integration-tests.md**

Cover:
- `SimpleModuleWebApplicationFactory<THost>`
- In-memory SQLite by default
- Test authentication: `CreateAuthenticatedClient(params Claim[] claims)`
- Claims via `X-Test-Claims` header
- Testing endpoints end-to-end
- PostgreSQL integration tests in CI

**Step 4: Write e2e-tests.md**

Cover:
- Playwright setup and configuration
- Running E2E tests: `npm run test:e2e`, `npm run test:e2e:ui`
- Writing browser tests
- CI integration

---

### Task 8: Advanced section

**Files:**
- Create: `docs/site/advanced/source-generator.md`
- Create: `docs/site/advanced/type-generation.md`
- Create: `docs/site/advanced/interceptors.md`
- Create: `docs/site/advanced/deployment.md`

**Step 1: Write source-generator.md**

Cover:
- How the Roslyn IIncrementalGenerator works
- What it discovers: [Module] classes, IEndpoint/IViewEndpoint implementors, [Dto] types
- What it generates: AddModules(), MapModuleEndpoints(), CollectModuleMenuItems(), JSON serializers, TypeScript interfaces, Razor assembly discovery
- netstandard2.0 constraint and why
- Debugging the generator

**Step 2: Write type-generation.md**

Cover:
- [Dto] attribute → source generator → embedded TypeScript
- `tools/extract-ts-types.mjs` pipeline
- `npm run generate:types` command
- Output: `ClientApp/types/` directory
- Using generated types in React components
- [NoDtoGeneration] escape hatch

**Step 3: Write interceptors.md**

Cover:
- EF Core SaveChangesInterceptor pattern
- Circular dependency problem with DI
- Solution: inject IServiceProvider, resolve at interception time
- Correct vs incorrect patterns (with code examples)
- When to use interceptors vs other approaches

**Step 4: Write deployment.md**

Cover:
- Docker and docker-compose setup
- CI/CD pipeline overview (build → test-sqlite → test-postgresql → publish)
- Production database setup (PostgreSQL recommended)
- Environment configuration
- Health checks

---

### Task 9: Reference section

**Files:**
- Create: `docs/site/reference/configuration.md`
- Create: `docs/site/reference/api.md`

**Step 1: Write configuration.md**

Cover:
- appsettings.json structure
- Database provider configuration
- Module-specific settings
- Environment variables
- Development vs production configuration

**Step 2: Write api.md**

Cover:
- Core interfaces: IModule, IEndpoint, IViewEndpoint, IEventBus, IEventHandler<T>, IEvent, IMenuRegistry
- Core attributes: [Module], [Dto], [NoDtoGeneration], [RequirePermission]
- Core types: PagedResult<T>, ModuleDbContextInfo
- Extension methods: AddModules(), MapModuleEndpoints()

---

### Task 10: Final verification

**Step 1: Run VitePress build**

Run: `cd docs/site && npx vitepress build`
Expected: Build succeeds with no errors, all pages rendered

**Step 2: Preview the built site**

Run: `cd docs/site && npx vitepress preview --port 5173`
Expected: All navigation works, sidebar links resolve, search works

**Step 3: Verify all internal links**

Check that all markdown cross-references resolve correctly.
