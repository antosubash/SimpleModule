# Technology Stack

**Analysis Date:** 2026-03-18

## Languages

**Primary:**
- C# 13 (net10.0) - Backend framework and host application
- TypeScript 5.8 - Frontend type safety and component development
- JavaScript (ES2022) - Build tooling and npm scripts

**Secondary:**
- CSS (via Tailwind CSS 4.2) - Styling with Tailwind directives enabled
- Razor (.cshtml) - Server-side template rendering for Blazor SSR

## Runtime

**Environment:**
- .NET 10.0 (net10.0)
- Node.js (for frontend build and tooling)
- AOT-Compatible: Host project publishes with `PublishAot` enabled

**Package Manager:**
- NPM (JavaScript/TypeScript) - Root monorepo configured with npm workspaces
- NuGet (.NET) - Package management via `*.csproj` files and `Directory.Build.props`
- Lockfile: `package-lock.json` present

## Frameworks

**Core Framework:**
- ASP.NET Core (net10.0) - Web API and routing via minimal APIs
- Blazor SSR - Server-side rendering with Razor components (`Microsoft.NET.Sdk.Razor`)
- Inertia.js - Server-driven React with JSON props over HTTP
- React 19.0 - Frontend UI framework
- React-DOM 19.0 - React rendering target

**Module System:**
- SimpleModule framework - Proprietary modular monolith with compile-time discovery
- Source generators - `IIncrementalGenerator` on netstandard2.0 targeting net10.0 code generation
- Entity Framework Core (EF Core) - ORM with multi-provider support

**CSS & Styling:**
- Tailwind CSS 4.2 - Utility-first CSS framework with @tailwindcss/vite integration
- Tailwind CLI (binary) - CSS bundling at build time via `tools/tailwindcss` executable
- @simplemodule/theme-default - Custom default theme package

**Component Libraries:**
- @simplemodule/ui - Radix UI component wrappers with Tailwind styling
- Radix UI - Headless UI primitives (20+ components: Dialog, Select, Tabs, Toast, etc.)

**Build & Dev:**
- Vite 6.2 - Frontend module bundler in library mode for module pages
- @vitejs/plugin-react 4.4 - React JSX transformation
- @tailwindcss/vite 4.2 - Tailwind CSS Vite integration for processing
- TypeScript 5.8 - TypeScript compiler and type checking
- Biome 2.4.6 - Linting and code formatting (replaces ESLint + Prettier)

**Testing:**
- xUnit.v3 - Unit test framework for .NET
- Playwright 1.52 - E2E testing framework (Chromium, Firefox, WebKit)
- Bogus - Fake data generation for unit tests
- FluentAssertions - Assertion library for readable test code

**Database:**
- Entity Framework Core 10.0
- Microsoft.EntityFrameworkCore.Sqlite - SQLite provider for development/testing
- Npgsql.EntityFrameworkCore.PostgreSQL - PostgreSQL provider for production
- Microsoft.EntityFrameworkCore.SqlServer - SQL Server provider (optional)

**Authentication & Authorization:**
- OpenIddict 3+ - OAuth2/OIDC provider and validation
- OpenIddict.AspNetCore - OpenIddict integration with ASP.NET Core
- OpenIddict.EntityFrameworkCore - Database persistence for OpenIddict
- Microsoft.AspNetCore.Identity - Identity framework for users and claims

**API Documentation:**
- Swashbuckle.AspNetCore - Swagger/OpenAPI documentation generation
- Microsoft.AspNetCore.OpenApi - Built-in OpenAPI endpoint support

**Development & Tooling:**
- Spectre.Console.Cli - CLI framework for `sm` command utility
- NetEscapades.EnumGenerators - Source generators for enum helpers
- Roslynator.Analyzers - C# code analyzers (added globally in Directory.Build.props)
- Microsoft.CodeAnalysis.CSharp - Roslyn API for code generation
- Microsoft.AspNetCore.Mvc.Testing - Testing utilities for integration tests

## Key Dependencies

**Critical (.NET):**
- Microsoft.AspNetCore.App (framework reference) - Core ASP.NET runtime
- SimpleModule.Core - Module interface, endpoint contracts, event bus, menu system
- SimpleModule.Database - EF Core context, multi-provider database setup
- SimpleModule.Blazor - Razor component root for SSR
- SimpleModule.Generator - Roslyn source generator for AOT static code generation

**Critical (Frontend):**
- @inertiajs/react 2.0 - React adapter for Inertia.js server-driven UI
- @simplemodule/client - Custom Vite plugin for vendor bundling and page resolution
- react 19.0 & react-dom 19.0 - React runtime and rendering

**Infrastructure (.NET):**
- Microsoft.EntityFrameworkCore.Design - EF Core CLI tools for migrations
- Microsoft.AspNetCore.Identity.EntityFrameworkCore - Identity EF Core integration
- Microsoft.NET.Test.Sdk - Test runner infrastructure
- xunit.runner.visualstudio - Visual Studio test explorer integration

## Configuration

**Environment:**
- `.env` files - NOT USED (no environment secrets visible in repo config)
- `appsettings.json` - Default configuration in `./template/SimpleModule.Host/`
- `appsettings.Development.json` - Development overrides with PostgreSQL connection string
- Database connection string in `Database:DefaultConnection` configuration key
- Aspire service defaults integration for OpenTelemetry and connection string bridging

**Build Configuration:**
- `Directory.Build.props` - Global MSBuild configuration for all projects
  - `TreatWarningsAsErrors: true` - Strict compilation
  - `AnalysisLevel: latest-all` & `AnalysisMode: All` - Latest static analysis
  - `Nullable: enable` - Nullable reference types enforced
  - `ImplicitUsings: enable` - Global using directives
- `.editorconfig` - Comprehensive C# and formatting rules (enforced via Biome for JS/TS)
- `biome.json` - Code formatting and linting for JavaScript/TypeScript
  - Single quotes, trailing commas, 2-space indent, 100-char line width
  - Tailwind CSS directives enabled
- `tsconfig.json` - TypeScript compiler options
  - Target: ES2022, Module: ESNext, moduleResolution: bundler
  - Strict mode enabled, JSX: react-jsx
- `.gitignore` - Standard .NET + Node.js exclusions

**Frontend Build:**
- Module builds use Vite in library mode outputting to `{ModuleName}.pages.js`
- React and React-DOM externalized and vendored separately
- Dynamic imports inlined in library builds
- Tailwind CSS bundled at build time via custom MSBuild target

## Platform Requirements

**Development:**
- .NET 10.0 SDK
- Node.js (LTS recommended) for npm workspaces
- PostgreSQL (optional, for Development appsettings)
- Tailwind CSS CLI binary (auto-downloaded via `tools/download-tailwind.sh` or `.ps1`)

**Production:**
- .NET 10.0 runtime
- SQLite or PostgreSQL database
- HTTPS endpoint (Inertia + Blazor SSR requires secure origin)
- Aspire orchestration support (optional)

**CI/CD:**
- GitHub Actions (inferred from playwright.config.ts `process.env.CI` support)
- Tests run against both SQLite (in-memory) and PostgreSQL in CI

---

*Stack analysis: 2026-03-18*
