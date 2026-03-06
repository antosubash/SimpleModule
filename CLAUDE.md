# SimpleModule

Modular monolith framework for .NET with compile-time module discovery via Roslyn source generators. Fully AOT-compatible — no reflection.

## Architecture

- **SimpleModule.Core** — `IModule` interface + `[Module]` attribute. References `Microsoft.AspNetCore.App` framework.
- **SimpleModule.Generator** — Incremental source generator (`netstandard2.0`) that discovers `[Module]`-decorated classes across referenced assemblies and generates `AddModules()` / `MapModuleEndpoints()` extension methods with direct `new` calls (no DI for module resolution).
- **SimpleModule.Api** — Host app (net10.0, PublishAot). References Core, Generator (as Analyzer), and all module projects.
- **src/modules/** — Each module is a class library (net10.0) referencing Core + `Microsoft.AspNetCore.App` framework.

## Key Constraints

- **No reflection** — everything must be AOT-compliant. The source generator emits static `new ModuleName()` calls.
- **Source generator must target netstandard2.0** with `LangVersion: latest` and `EnforceExtendedAnalyzerRules: true` (must use `IIncrementalGenerator`, not `ISourceGenerator`).
- **Module class libraries must NOT have `PublishAot`** — only the API project should. They need `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

## Build & Run

```bash
dotnet build
dotnet run --project src/SimpleModule.Api
```

## Adding a New Module

1. Create folder `src/modules/<Name>/`
2. Create `src/modules/<Name>/<Name>.Contracts/` with:
   - `<Name>.Contracts.csproj` (references Core only, uses `Microsoft.NET.Sdk`)
   - `I<Name>Contracts.cs` — public interface for cross-module use
   - Shared DTO types marked with `[Dto]`
3. Create `src/modules/<Name>/<Name>/` with:
   - `<Name>.csproj` (references Core + `<Name>.Contracts`; uses `Microsoft.NET.Sdk` with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`)
   - `<Name>Module.cs` — implements `IModule` with `[Module("Name")]`
   - `Features/<FeatureName>/` folders containing endpoint and handler classes
   - Register the contract interface against implementation in `ConfigureServices`
4. Add `ProjectReference` to `src/SimpleModule.Api/SimpleModule.Api.csproj` pointing to `<Name>/<Name>.csproj`
5. Add both projects to `SimpleModule.sln`
