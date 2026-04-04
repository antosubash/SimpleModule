---
outline: deep
---

# Source Generator

SimpleModule uses a Roslyn **incremental source generator** (`IIncrementalGenerator`) to discover modules, endpoints, DTOs, and other framework constructs at compile time. This eliminates runtime reflection entirely -- everything is wired up in generated code before your application starts.

## Why IIncrementalGenerator?

The Roslyn SDK offers two generator APIs:

| API | Caching | Performance |
|-----|---------|-------------|
| `ISourceGenerator` | None -- runs on every keystroke | Slow in large solutions |
| `IIncrementalGenerator` | Built-in incremental caching | Only re-generates when inputs change |

SimpleModule uses `IIncrementalGenerator` because modular monoliths reference many assemblies. Without incremental caching, the generator would re-scan every referenced assembly on every keystroke, causing noticeable IDE lag.

The generator extracts an **equatable data model** (`DiscoveryData`) from the compilation. The incremental pipeline compares the new data model against the cached one and skips re-generation when nothing has changed.

## The netstandard2.0 Constraint

Roslyn analyzers and source generators **must** target `netstandard2.0`. This is a hard requirement from the Roslyn compiler infrastructure -- the generator runs inside the compiler process, which loads analyzers as netstandard2.0 assemblies.

The generator's project file reflects this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

::: warning
If you add dependencies to the generator project, they must also be netstandard2.0 compatible. Modern .NET APIs like `Span<T>` or `ImmutableArray<T>` are available through the `Microsoft.CodeAnalysis.CSharp` package.
:::

## What It Discovers

The generator scans both referenced assemblies and the current compilation for:

| Construct | How It's Found |
|-----------|----------------|
| **Modules** | Classes decorated with `[Module]` attribute |
| **API Endpoints** | Classes implementing `IEndpoint` |
| **View Endpoints** | Classes implementing `IViewEndpoint` |
| **DTO Types** | Public types in `*.Contracts` assemblies (opt-out with `[NoDtoGeneration]`) |
| **DbContexts** | Classes inheriting from `DbContext` in module assemblies |
| **Entity Configurations** | Classes implementing `IEntityTypeConfiguration<T>` |
| **Contract Interfaces** | Interfaces in `*.Contracts` assemblies with implementations |
| **Permission Classes** | Sealed classes implementing `IModulePermissions` |
| **Interceptors** | Classes implementing `ISaveChangesInterceptor` |
| **Vogen Value Objects** | Types with Vogen value object markers |

All discovered data is collected into a `DiscoveryData` record:

```csharp
internal readonly record struct DiscoveryData(
    ImmutableArray<ModuleInfoRecord> Modules,
    ImmutableArray<DtoTypeInfoRecord> DtoTypes,
    ImmutableArray<DbContextInfoRecord> DbContexts,
    ImmutableArray<EntityConfigInfoRecord> EntityConfigs,
    ImmutableArray<ModuleDependencyRecord> Dependencies,
    ImmutableArray<IllegalModuleReferenceRecord> IllegalReferences,
    ImmutableArray<ContractInterfaceInfoRecord> ContractInterfaces,
    ImmutableArray<ContractImplementationRecord> ContractImplementations,
    ImmutableArray<PermissionClassRecord> PermissionClasses,
    ImmutableArray<InterceptorInfoRecord> Interceptors,
    ImmutableArray<VogenValueObjectRecord> VogenValueObjects
);
```

## What It Generates

The generator feeds `DiscoveryData` through a pipeline of **emitters**, each responsible for generating a specific source file:

### Core Extension Methods

| Emitter | Generated File | Purpose |
|---------|---------------|---------|
| `ModuleExtensionsEmitter` | `ModuleExtensions.g.cs` | `AddModules()` -- registers all module services, contract implementations, permissions, and JSON serializers |
| `EndpointExtensionsEmitter` | `EndpointExtensions.g.cs` | `MapModuleEndpoints()` -- maps all `IEndpoint` and `IViewEndpoint` implementations with route groups and authorization |
| `MenuExtensionsEmitter` | `MenuExtensions.g.cs` | `CollectModuleMenuItems()` -- collects menu items from all modules that implement `ConfigureMenu` |
| `SettingsExtensionsEmitter` | `SettingsExtensions.g.cs` | Collects settings definitions from modules that implement `ConfigureSettings` |
| `HostingExtensionsEmitter` | `HostingExtensions.g.cs` | Top-level `AddSimpleModule()` that orchestrates all registrations |

### Frontend Integration

| Emitter | Generated File | Purpose |
|---------|---------------|---------|
| `TypeScriptDefinitionsEmitter` | `DtoTypeScript_{Module}.g.cs` | Embeds TypeScript interface definitions as comments in C# files, one per module |
| `ViewPagesEmitter` | `ViewPages.g.cs` | Page constants for all `IViewEndpoint` views |
| `PageRegistryEmitter` | `PageRegistry.g.cs` | Registry mapping page names to module assemblies |

### Database

| Emitter | Generated File | Purpose |
|---------|---------------|---------|
| `HostDbContextEmitter` | `HostDbContext.g.cs` | Generates the host DbContext that aggregates module DbSets |
| `DbContextRegistryEmitter` | `DbContextRegistry.g.cs` | Registers `ModuleDbContextInfo` for each module's DbContext |
| `ValueConverterConventionsEmitter` | `ValueConverterConventions.g.cs` | Applies EF Core value converters for Vogen value objects |

### Infrastructure

| Emitter | Generated File | Purpose |
|---------|---------------|---------|
| `JsonResolverEmitter` | `ModulesJsonResolver.g.cs` | AOT-compatible JSON type info resolver for all DTO types |
| `ViewPagesEmitter` | `ViewPages.g.cs` | Maps view endpoints to React component names |
| `DiagnosticEmitter` | _(diagnostics only)_ | Reports compiler warnings/errors for illegal module references and other issues |

## The Discovery-Emit Pipeline

The generator follows a two-phase architecture:

```
Compilation
    │
    ▼
SymbolDiscovery.Extract(compilation)
    │  Scans all referenced assemblies
    │  Finds [Module] classes, IEndpoint types, [Dto] types, etc.
    │  Returns equatable DiscoveryData
    │
    ▼
Incremental Cache Check
    │  Compares new DiscoveryData with cached version
    │  If equal → skip generation (no IDE lag)
    │  If changed → proceed to emission
    │
    ▼
Emitters[].Emit(context, data)
    │  Each emitter generates one source file
    │  ModuleExtensionsEmitter → ModuleExtensions.g.cs
    │  EndpointExtensionsEmitter → EndpointExtensions.g.cs
    │  TypeScriptDefinitionsEmitter → DtoTypeScript_*.g.cs
    │  ... (14 emitters total)
    │
    ▼
Generated source added to compilation
```

The main generator class is minimal -- it delegates all work to `SymbolDiscovery` and the emitter array:

```csharp
[Generator]
public class ModuleDiscovererGenerator : IIncrementalGenerator
{
    private static readonly IEmitter[] Emitters =
    [
        new DiagnosticEmitter(),
        new ModuleExtensionsEmitter(),
        new EndpointExtensionsEmitter(),
        new MenuExtensionsEmitter(),
        new SettingsExtensionsEmitter(),
        new ViewPagesEmitter(),
        new PageRegistryEmitter(),
        new JsonResolverEmitter(),
        new TypeScriptDefinitionsEmitter(),
        new HostingExtensionsEmitter(),
        new HostDbContextEmitter(),
        new ValueConverterConventionsEmitter(),
        new DbContextRegistryEmitter(),
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var dataProvider = context.CompilationProvider.Select(
            static (compilation, _) => SymbolDiscovery.Extract(compilation)
        );

        context.RegisterSourceOutput(
            dataProvider,
            static (spc, data) =>
            {
                if (data.Modules.Length == 0)
                    return;

                foreach (var emitter in Emitters)
                {
                    emitter.Emit(spc, data);
                }
            }
        );
    }
}
```

## Module Dependency Ordering

The generator performs **topological sorting** of modules based on their contract dependencies. This ensures that when `AddModules()` calls `ConfigureServices` on each module, dependencies are initialized before dependents. The generated code includes phase comments:

```csharp
// Phase 1: No dependencies
((IModule)s_Dashboard_DashboardModule).ConfigureServices(services, configuration);

// Phase 2: Depends on Products
((IModule)s_Orders_OrdersModule).ConfigureServices(services, configuration);
```

## Debugging Tips

### Inspecting Generated Files

Generated source files are written to the `obj/` directory during build. To find them:

```bash
# Find all generated files for the host project
find template/SimpleModule.Host/obj -name "*.g.cs" | head -20
```

Or in Visual Studio, expand **Dependencies > Analyzers > SimpleModule.Generator** in Solution Explorer to see all generated files.

### Common Issues

::: tip Generator Not Running
If you don't see generated files, verify that the host project references the generator correctly:
```xml
<ProjectReference Include="...\SimpleModule.Generator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```
:::

::: tip Module Not Discovered
The generator scans referenced assemblies for `[Module]` classes. Ensure your module project is referenced by the host project and the class has the `[Module]` attribute.
:::

::: tip Stale Generated Code
If the generator seems to produce outdated code, clean and rebuild:
```bash
dotnet clean
dotnet build
```
The incremental cache occasionally needs a full rebuild to reset.
:::

## Next Steps

- [Type Generation](/advanced/type-generation) -- how DTOs become TypeScript interfaces
- [Modules](/guide/modules) -- how modules are defined and registered
- [API Reference](/reference/api) -- all generated extension methods
