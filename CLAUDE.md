# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

Modular monolith framework for .NET with compile-time module discovery via Roslyn source generators. Fully AOT-compatible — no reflection. Frontend uses React 19 + Inertia.js served via Blazor SSR.

## Build & Run

```bash
dotnet build
dotnet run --project template/SimpleModule.Host     # runs on https://localhost:5001
```

## Frontend (npm workspaces)

```bash
npm install                          # install all workspace dependencies
npm run check                        # biome lint + format check
npm run check:fix                    # auto-fix lint + formatting
npm run lint                         # lint only
npm run format                       # format only (with write)
```

Workspaces: `modules/*/src/*`, `packages/*`, and `template/SimpleModule.Host/ClientApp`.

## Testing

```bash
dotnet test                                            # all tests
dotnet test --filter "FullyQualifiedName~ClassName"    # single test class
dotnet test --filter "FullyQualifiedName~MethodName"   # single test method
```

Test stack: xUnit.v3, FluentAssertions, Bogus, Microsoft.AspNetCore.Mvc.Testing. SQLite in-memory for unit tests, PostgreSQL for integration tests in CI.

## Architecture

### .NET Backend

- **SimpleModule.Core** — `IModule` interface, `[Module]` attribute, `IEndpoint` interface, `[Dto]` attribute, menu system (`IMenuRegistry`), event bus (`IEventBus`), Inertia integration.
- **SimpleModule.Generator** — Roslyn `IIncrementalGenerator` (netstandard2.0). Scans referenced assemblies for `[Module]` classes, `IEndpoint` implementors, and `[Dto]` types. Generates: `AddModules()`, `MapModuleEndpoints()`, `CollectModuleMenuItems()`, AOT JSON serializers, TypeScript interface definitions, Razor component assembly discovery.
- **SimpleModule.Host** — Host app (net10.0, PublishAot). Calls generated extension methods in `Program.cs`. Custom Inertia middleware bridges Blazor SSR → React.

### Frontend (React + Inertia.js)

- **ClientApp** (`template/SimpleModule.Host/ClientApp/app.tsx`) — Inertia bootstrap. Resolves pages by splitting route name (e.g., `Products/Browse` → imports `/_content/Products/Products.pages.js`).
- **Module pages** — Each module builds its React pages via Vite in library mode → `{ModuleName}.pages.js` in module's `wwwroot/`. Entry point: `Pages/index.ts` exporting a `pages` record mapping route names to components.
- **Type generation** — `[Dto]` types → source generator embeds TS interfaces → `tools/extract-ts-types.mjs` writes `.ts` files to `ClientApp/types/`.

### Request Flow

1. ASP.NET route handler calls `Inertia.Render("Products/Browse", props)`
2. Inertia middleware renders Blazor SSR shell with JSON props
3. React ClientApp dynamically imports module's `pages.js` bundle
4. Component hydrates with server-provided props

## Key Constraints

- **No reflection** — source generator emits static `new ModuleName()` calls for AOT.
- **Source generator must target netstandard2.0** with `IIncrementalGenerator` (not `ISourceGenerator`).
- **Module class libraries must NOT have `PublishAot`** — only the API project. Modules need `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
- **Module Vite builds use library mode** — externalize React, React-DOM, @inertiajs/react. Inline dynamic imports.

## Adding a New Module

1. Create `modules/<Name>/`
2. Create `modules/<Name>/src/<Name>.Contracts/` with:
   - `<Name>.Contracts.csproj` (references Core only, `Microsoft.NET.Sdk`)
   - `I<Name>Contracts.cs` — public interface for cross-module use
   - Shared DTO types marked with `[Dto]`
3. Create `modules/<Name>/src/<Name>/` with:
   - `<Name>.csproj` (references Core + Contracts; `Microsoft.NET.Sdk` with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`)
   - `<Name>Module.cs` — implements `IModule` with `[Module("Name", RoutePrefix = "...")]`
   - `Endpoints/<Name>/` — endpoint classes implementing `IEndpoint` (auto-discovered)
   - `Pages/index.ts` — exports `pages` record mapping route names to React components
   - `vite.config.ts` — library mode build targeting `Pages/index.ts`
   - `package.json` — declare React/Inertia as peerDependencies
   - Register contract interface in `ConfigureServices`
   - **Escape hatch**: For non-standard routes, implement `ConfigureEndpoints` on the module class
4. Create `modules/<Name>/tests/<Name>.Tests/` with xUnit test project
5. Add `ProjectReference` to `template/SimpleModule.Host/SimpleModule.Host.csproj`
6. Add all projects to `SimpleModule.slnx`

## Linting & Formatting

Biome is configured at repo root (`biome.json`). Covers `modules/**`, `packages/**`, `template/**` except `**/wwwroot/**`. Settings: single quotes, semicolons always, 2-space indent, trailing commas, 100-char line width. Tailwind CSS directives enabled.
