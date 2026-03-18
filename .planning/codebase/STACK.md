# Technology Stack

**Analysis Date:** 2026-03-18

## Languages

**Primary:**
- C# 13 (implicit usings, nullable reference types enabled) - Backend APIs, modules, framework, CLI
- TypeScript 5.8 - Frontend React components, UI utilities, build tooling
- JavaScript ES2022 - Build scripts, Vite configuration

**Secondary:**
- HTML/CSS (Tailwind) - Styling via Tailwind CSS 4.2.1
- SQL - Entity Framework Core for PostgreSQL, SQLite, SQL Server

## Runtime

**Environment:**
- .NET 10.0 - Target framework for all projects (`net10.0`)
- Node.js - For npm workspaces and Vite build tooling

**Package Manager:**
- npm (v9+) - JavaScript dependencies and workspaces
- NuGet - C# dependencies

## Frameworks

**Core (Backend):**
- ASP.NET Core 10.0 - Web framework, minimal APIs, Blazor SSR
- Entity Framework Core 10.0 - ORM with multi-provider support (SQLite, PostgreSQL, SQL Server)
- OpenIddict 5.x - OAuth 2.0/OIDC provider for authentication
- Microsoft.AspNetCore.Identity - User authentication and role management
- Roslyn (`Microsoft.CodeAnalysis.CSharp`) - Source generator for module discovery and type generation

**Frontend:**
- React 19.0 - UI framework
- Inertia.js 2.0 - Server-driven UI adapter between ASP.NET Core and React
- Vite 6.2.0 - Module bundler for frontend development and library builds
- Tailwind CSS 4.2.1 - Utility-first CSS framework

**Testing:**
- xUnit.v3 - .NET test framework
- Bogus - Fake data generation for testing
- FluentAssertions - Assertion library for readable test code
- Microsoft.AspNetCore.Mvc.Testing - Integration testing utilities
- Vitest - JavaScript test runner (referenced in e2e tests)

**Build & Dev Tools:**
- Biome 2.4.6 - JavaScript linter and formatter
- Tailwind CSS CLI - CSS processing
- Roslynator.Analyzers - C# code quality analyzers
- NetEscapades.EnumGenerators - Enum source generator

## Key Dependencies

**Critical (Framework):**
- `Microsoft.AspNetCore.OpenApi` - OpenAPI/Swagger support
- `Microsoft.EntityFrameworkCore.Design` - EF Core tooling
- `OpenIddict.AspNetCore` - OAuth 2.0/OIDC authentication provider
- `Swashbuckle.AspNetCore` - Swagger/OpenAPI documentation

**Database Providers:**
- `Microsoft.EntityFrameworkCore.Sqlite` - SQLite support for development/testing
- `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL support for production
- `Microsoft.EntityFrameworkCore.SqlServer` - SQL Server support

**Frontend UI Components:**
- `@radix-ui/react-*` (accordion, dialog, dropdown, select, etc.) - Headless UI primitives
- `class-variance-authority` - Type-safe component variants
- `clsx` - Class name utility
- `cmdk` - Command/search component
- `tailwind-merge` - Tailwind class merging utility

**Infrastructure:**
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` - Distributed tracing (OTEL)
- `OpenTelemetry.Instrumentation.AspNetCore` - ASP.NET Core instrumentation
- `OpenTelemetry.Instrumentation.Http` - HTTP client instrumentation
- `Microsoft.Extensions.Http.Resilience` - HTTP resilience policies
- `Microsoft.Extensions.ServiceDiscovery` - Service discovery for distributed deployments

## Configuration

**Environment:**
- Configured via `appsettings.json` and `appsettings.{Environment}.json`
- Database connection string: `Database:DefaultConnection`
- OpenIddict certificate paths: `OpenIddict:EncryptionCertPath`, `OpenIddict:SigningCertPath`
- OpenTelemetry/Aspire configuration via service defaults

**Build:**
- `.csproj` files use centralized package management via implicit SDK versions
- `Directory.Build.props` enforces code style (`Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`)
- Tailwind CLI binary downloaded to `tools/tailwindcss.exe` (Windows) or `tools/tailwindcss` (Unix)
- Vite configuration per module in `vite.config.ts` with library mode for module pages and component exports

**JavaScript:**
- `biome.json` at repo root enforces formatting (2-space indent, 100-char line width, single quotes, trailing commas)
- `tsconfig.json` targets ES2022, strict mode enabled, React JSX transform

## Platform Requirements

**Development:**
- .NET 10.0 SDK (rollForward: latestMajor, allowPrerelease)
- Node.js (npm 9+)
- Tailwind CSS CLI (downloaded via `tools/download-tailwind.sh` or `.ps1`)

**Production:**
- Docker container: `mcr.microsoft.com/dotnet/aspnet:10.0`
- ASP.NET Core runs on HTTPS (port 5001 in development via launch profile)
- Database: PostgreSQL 16 (compose) or SQLite (development)
- .NET 10.0 runtime (AOT-compatible)

## Source Generator Architecture

**SimpleModule.Generator** (`framework/SimpleModule.Generator/`):
- Targets `netstandard2.0` as `IIncrementalGenerator`
- Scans referenced assemblies for `[Module]` attribute on classes
- Discovers `IEndpoint` implementations
- Discovers `[Dto]` type definitions
- **Generates**:
  - `AddModules()` extension method - registers all module services
  - `MapModuleEndpoints()` extension method - maps all endpoint routes
  - `CollectModuleMenuItems()` extension method - gathers menu items
  - AOT-compatible JSON serializers for all `[Dto]` types
  - TypeScript interface definitions (consumed by `tools/extract-ts-types.mjs`)

## .NET Analyzer & Code Quality

**Global Settings (`Directory.Build.props`):**
- `TreatWarningsAsErrors=true`
- `EnforceCodeStyleInBuild=true`
- `AnalysisLevel=latest-all`
- `AnalysisMode=All`

**Suppressions:**
- Listed in `.editorconfig` (CA2234, xUnit1051, etc.)

---

*Stack analysis: 2026-03-18*
