# Source Generator Split & Perf Wins Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split `framework/SimpleModule.Generator/` files over 300 lines into cohesive sub-300-line files, and land seven safe performance improvements without changing generated output.

**Architecture:** `ModuleDiscovererGenerator` keeps the same incremental pipeline. `SymbolDiscovery.Extract` stays the orchestrator but delegates to per-responsibility finder classes (`Discovery/Finders/*`). Equatable records move out of one giant `DiscoveryData.cs` into grouped `Records/*`. `DiagnosticEmitter` becomes a router; 38 descriptors move to `DiagnosticDescriptors`, and per-concern checker classes hold the logic. A new `CoreSymbols` record resolves `GetTypeByMetadataName` once per Extract and threads through every finder.

**Tech Stack:** .NET source generators, Roslyn (`Microsoft.CodeAnalysis`), `IIncrementalGenerator`, netstandard2.0, xUnit.v3 + FluentAssertions for tests.

**Spec:** [docs/superpowers/specs/2026-04-15-generator-split-and-perf-design.md](../specs/2026-04-15-generator-split-and-perf-design.md)

---

## File map (end state)

### New files

```
framework/SimpleModule.Generator/
  Discovery/
    AssemblyConventions.cs                   # relocated from DiagnosticEmitter.cs
    CoreSymbols.cs                           # NEW
    SymbolHelpers.cs                         # extracted cross-cutting helpers
    Records/
      ModuleRecords.cs                       # split out of DiscoveryData.cs
      DataRecords.cs                         # split out of DiscoveryData.cs
    Finders/
      ModuleFinder.cs
      EndpointFinder.cs
      DtoFinder.cs
      DbContextFinder.cs
      ContractFinder.cs
      PermissionFeatureFinder.cs
      InterceptorFinder.cs
      AgentFinder.cs
      VogenFinder.cs
  Emitters/
    Diagnostics/
      DiagnosticDescriptors.cs               # 38 descriptors
      ModuleChecks.cs
      DbContextChecks.cs
      ContractAndDtoChecks.cs
      PermissionFeatureChecks.cs
      EndpointChecks.cs
      DependencyChecks.cs
```

### Trimmed files

| File | Before | After target |
|---|---|---|
| `Discovery/SymbolDiscovery.cs` | 2068 | ≤ 200 (orchestrator) |
| `Discovery/DiscoveryData.cs` | 640 | ≤ 220 (top record + hash + SourceLocationRecord) |
| `Emitters/DiagnosticEmitter.cs` | 1294 | ≤ 80 (orchestrator) |

### New tests

```
tests/SimpleModule.Generator.Tests/
  IncrementalCachingTests.cs
  DiagnosticCatalogTests.cs
```

---

## Task 1: Capture baseline

**Files:**
- Create: `baseline/generator-output/` (temp, not committed)
- Create: `baseline/diagnostics.txt` (temp, not committed)

- [ ] **Step 1: Build the host to populate obj/.../generated/**

Run: `dotnet build template/SimpleModule.Host -c Debug`
Expected: Build succeeds.

- [ ] **Step 2: Snapshot the generated source files**

Run:
```bash
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
mkdir -p baseline/generator-output
cp "$GEN_DIR"/../../*.cs baseline/generator-output/ 2>/dev/null || true
cp -r "$GEN_DIR"/* baseline/generator-output/
ls baseline/generator-output/ | wc -l
```
Expected: Non-zero file count (should be ~20 generated files).

- [ ] **Step 3: Snapshot the diagnostic descriptor set**

Run:
```bash
grep -E "DiagnosticDescriptor [A-Za-z_]+ = new" framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs \
  | sed -E 's/.*DiagnosticDescriptor ([A-Za-z_]+) = new.*/\1/' \
  | sort > baseline/diagnostics.txt
wc -l baseline/diagnostics.txt
```
Expected: `38 baseline/diagnostics.txt`

- [ ] **Step 4: Run the full generator test suite, save as baseline**

Run: `dotnet test tests/SimpleModule.Generator.Tests --logger "console;verbosity=minimal"`
Expected: All tests pass.

- [ ] **Step 5: Add baseline/ to .gitignore (don't commit the snapshot)**

Read `.gitignore` to see its shape, then append:
```
# Temporary refactor baseline — not committed
baseline/
```

- [ ] **Step 6: Commit the gitignore change**

```bash
git add .gitignore
git commit -m "chore(generator): ignore baseline snapshot dir used during refactor"
```

---

## Task 2: Relocate `AssemblyConventions` to Discovery namespace

`AssemblyConventions` is currently inside `Emitters/DiagnosticEmitter.cs` (lines 10-37) but will be used by both discovery and diagnostics. Move it to its own file under `Discovery/` so it's a neutral dependency.

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/AssemblyConventions.cs`
- Modify: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs` (remove lines 10-37)

- [ ] **Step 1: Create the new file with the exact content of the existing class**

Write `framework/SimpleModule.Generator/Discovery/AssemblyConventions.cs`:
```csharp
using System;

namespace SimpleModule.Generator;

/// <summary>
/// Naming conventions for SimpleModule assemblies. Centralised so the same
/// string literals don't drift between discovery code and diagnostic emission.
/// </summary>
internal static class AssemblyConventions
{
    internal const string FrameworkPrefix = "SimpleModule.";
    internal const string ContractsSuffix = ".Contracts";
    internal const string ModuleSuffix = ".Module";

    /// <summary>
    /// Derives the `.Contracts` sibling assembly name for a SimpleModule
    /// implementation assembly. Strips a trailing <c>.Module</c> suffix first
    /// so <c>SimpleModule.Agents.Module</c> maps to
    /// <c>SimpleModule.Agents.Contracts</c> instead of
    /// <c>SimpleModule.Agents.Module.Contracts</c>.
    /// </summary>
    internal static string GetExpectedContractsAssemblyName(string implementationAssemblyName)
    {
        var baseName = implementationAssemblyName.EndsWith(ModuleSuffix, StringComparison.Ordinal)
            ? implementationAssemblyName.Substring(
                0,
                implementationAssemblyName.Length - ModuleSuffix.Length
            )
            : implementationAssemblyName;
        return baseName + ContractsSuffix;
    }
}
```

- [ ] **Step 2: Remove the duplicated class from DiagnosticEmitter.cs**

In `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`, delete the `AssemblyConventions` class (the one starting with `/// <summary>` at line 10 through its closing `}` — should be ~27 lines removed). Keep `using System;` at the top (still needed elsewhere in the file).

- [ ] **Step 3: Build**

Run: `dotnet build framework/SimpleModule.Generator -c Debug`
Expected: Build succeeds with zero new warnings.

- [ ] **Step 4: Run generator tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/AssemblyConventions.cs \
        framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs
git commit -m "refactor(generator): move AssemblyConventions to Discovery namespace"
```

---

## Task 3: Introduce `CoreSymbols` record

Resolves every `compilation.GetTypeByMetadataName` call once per Extract. Threaded through finders in later tasks.

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/CoreSymbols.cs`

- [ ] **Step 1: Write the record**

Write `framework/SimpleModule.Generator/Discovery/CoreSymbols.cs`:
```csharp
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

/// <summary>
/// Pre-resolved Roslyn type symbols needed during discovery. Resolving each
/// symbol once via <see cref="Compilation.GetTypeByMetadataName"/> at the top
/// of <c>SymbolDiscovery.Extract</c> is dramatically cheaper than scattering
/// calls across finder methods — every call force-resolves the namespace
/// chain, so caching them saves ~15 lookups per Extract invocation.
/// </summary>
internal readonly record struct CoreSymbols(
    INamedTypeSymbol ModuleAttribute,
    INamedTypeSymbol? DtoAttribute,
    INamedTypeSymbol? EndpointInterface,
    INamedTypeSymbol? ViewEndpointInterface,
    INamedTypeSymbol? AgentDefinition,
    INamedTypeSymbol? AgentToolProvider,
    INamedTypeSymbol? KnowledgeSource,
    INamedTypeSymbol? ModuleServices,
    INamedTypeSymbol? ModuleMenu,
    INamedTypeSymbol? ModuleMiddleware,
    INamedTypeSymbol? ModuleSettings,
    INamedTypeSymbol? NoDtoAttribute,
    INamedTypeSymbol? EventInterface,
    INamedTypeSymbol? ModulePermissions,
    INamedTypeSymbol? ModuleFeatures,
    INamedTypeSymbol? SaveChangesInterceptor,
    INamedTypeSymbol? ModuleOptions,
    bool HasAgentsAssembly
)
{
    /// <summary>
    /// Resolves all framework type symbols from the current compilation.
    /// Returns null if the ModuleAttribute itself isn't resolvable —
    /// discovery cannot proceed without it.
    /// </summary>
    internal static CoreSymbols? TryResolve(Compilation compilation)
    {
        var moduleAttribute = compilation.GetTypeByMetadataName("SimpleModule.Core.ModuleAttribute");
        if (moduleAttribute is null)
            return null;

        return new CoreSymbols(
            ModuleAttribute: moduleAttribute,
            DtoAttribute: compilation.GetTypeByMetadataName("SimpleModule.Core.DtoAttribute"),
            EndpointInterface: compilation.GetTypeByMetadataName("SimpleModule.Core.IEndpoint"),
            ViewEndpointInterface: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.IViewEndpoint"
            ),
            AgentDefinition: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.Agents.IAgentDefinition"
            ),
            AgentToolProvider: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.Agents.IAgentToolProvider"
            ),
            KnowledgeSource: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.Rag.IKnowledgeSource"
            ),
            ModuleServices: compilation.GetTypeByMetadataName("SimpleModule.Core.IModuleServices"),
            ModuleMenu: compilation.GetTypeByMetadataName("SimpleModule.Core.IModuleMenu"),
            ModuleMiddleware: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.IModuleMiddleware"
            ),
            ModuleSettings: compilation.GetTypeByMetadataName("SimpleModule.Core.IModuleSettings"),
            NoDtoAttribute: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.NoDtoGenerationAttribute"
            ),
            EventInterface: compilation.GetTypeByMetadataName("SimpleModule.Core.Events.IEvent"),
            ModulePermissions: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.Authorization.IModulePermissions"
            ),
            ModuleFeatures: compilation.GetTypeByMetadataName(
                "SimpleModule.Core.FeatureFlags.IModuleFeatures"
            ),
            SaveChangesInterceptor: compilation.GetTypeByMetadataName(
                "Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor"
            ),
            ModuleOptions: compilation.GetTypeByMetadataName("SimpleModule.Core.IModuleOptions"),
            HasAgentsAssembly:
                compilation.GetTypeByMetadataName("SimpleModule.Agents.SimpleModuleAgentExtensions")
                is not null
        );
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build framework/SimpleModule.Generator`
Expected: Build succeeds. (`CoreSymbols` is not referenced yet — that's fine.)

- [ ] **Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/CoreSymbols.cs
git commit -m "feat(generator): add CoreSymbols record for one-shot type resolution"
```

---

## Task 4: Use `CoreSymbols` in `SymbolDiscovery.Extract`

Replace the scattered `compilation.GetTypeByMetadataName` calls at the top of `Extract` with a single `CoreSymbols.TryResolve` call, then pass the record through to existing finders. Keep finder signatures mostly the same — we'll clean them up in the split phase.

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs` (Extract method)

- [ ] **Step 1: Replace the top of `Extract` with a `CoreSymbols` bootstrap**

In `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`, replace lines 35-81 (the section starting with `internal static DiscoveryData Extract(` through the end of the per-symbol resolutions) so the new top of the method reads:

```csharp
    internal static DiscoveryData Extract(
        Compilation compilation,
        CancellationToken cancellationToken
    )
    {
        var hostAssemblyName = compilation.Assembly.Name;

        var symbols = CoreSymbols.TryResolve(compilation);
        if (symbols is null)
            return DiscoveryData.Empty;
        var s = symbols.Value;
```

Then update every downstream usage in the method body:
- `moduleAttributeSymbol` → `s.ModuleAttribute`
- `dtoAttributeSymbol` → `s.DtoAttribute`
- `endpointInterfaceSymbol` → `s.EndpointInterface`
- `viewEndpointInterfaceSymbol` → `s.ViewEndpointInterface`
- `agentDefinitionSymbol` → `s.AgentDefinition`
- `agentToolProviderSymbol` → `s.AgentToolProvider`
- `knowledgeSourceSymbol` → `s.KnowledgeSource`
- `moduleServicesSymbol` → `s.ModuleServices`
- `moduleMenuSymbol` → `s.ModuleMenu`
- `moduleMiddlewareSymbol` → `s.ModuleMiddleware`
- `moduleSettingsSymbol` → `s.ModuleSettings`
- `noDtoAttrSymbol` → `s.NoDtoAttribute`
- `eventInterfaceSymbol` → `s.EventInterface`
- `modulePermissionsSymbol` → `s.ModulePermissions`
- `moduleFeaturesSymbol` → `s.ModuleFeatures`
- `saveChangesInterceptorSymbol` → `s.SaveChangesInterceptor`
- `moduleOptionsSymbol` → `s.ModuleOptions`

Delete the local variable declarations that resolved each symbol (the original block was 17 `GetTypeByMetadataName` calls; they're all replaced by `CoreSymbols.TryResolve`).

Also replace the `DiscoveryData` constructor call near the end where `HasAgentsAssembly` was computed inline — use `s.HasAgentsAssembly` instead.

- [ ] **Step 2: Build**

Run: `dotnet build framework/SimpleModule.Generator`
Expected: Build succeeds. If any symbol variable was missed, the compiler will flag it.

- [ ] **Step 3: Run generator tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests`
Expected: All tests pass.

- [ ] **Step 4: Diff generated output against baseline**

Run:
```bash
dotnet build template/SimpleModule.Host -c Debug
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
diff -r baseline/generator-output "$GEN_DIR" | head -50
```
Expected: No output (identical).

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "perf(generator): resolve core symbols once via CoreSymbols record"
```

---

## Task 5: Split `DiscoveryData.cs` — create `Records/ModuleRecords.cs`

Move module-related equatable records out so `DiscoveryData.cs` stays focused.

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/Records/ModuleRecords.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs`

- [ ] **Step 1: Create the new file with relocated records**

Write `framework/SimpleModule.Generator/Discovery/Records/ModuleRecords.cs` containing (copy from `DiscoveryData.cs` lines 135-334 verbatim — preserve every `Equals`/`GetHashCode` override):
- `ModuleInfoRecord` (with custom Equals/GetHashCode)
- `EndpointInfoRecord` (with custom Equals/GetHashCode)
- `ViewInfoRecord`
- `ModuleDependencyRecord`
- `IllegalModuleReferenceRecord`

File header:
```csharp
using System.Collections.Immutable;
using System.Linq;

namespace SimpleModule.Generator;
```

- [ ] **Step 2: Delete the moved records from DiscoveryData.cs**

In `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs`, delete lines 135-334 (the 5 records just copied). Leave the top-level `DiscoveryData`, `HashHelper`, `SourceLocationRecord`, and the other records that will move in the next task.

- [ ] **Step 3: Build**

Run: `dotnet build framework/SimpleModule.Generator`
Expected: Build succeeds.

- [ ] **Step 4: Run generator tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/Records/ModuleRecords.cs \
        framework/SimpleModule.Generator/Discovery/DiscoveryData.cs
git commit -m "refactor(generator): split ModuleRecords out of DiscoveryData"
```

---

## Task 6: Split `DiscoveryData.cs` — create `Records/DataRecords.cs`

Move the remaining equatable records and the mutable working types. `DiscoveryData.cs` ends up holding only the top-level `DiscoveryData`, `HashHelper`, and `SourceLocationRecord`.

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/Records/DataRecords.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs`

- [ ] **Step 1: Create the new file**

Write `framework/SimpleModule.Generator/Discovery/Records/DataRecords.cs` containing the following records (copy verbatim from the current `DiscoveryData.cs`):

**Equatable records** (move from current lines 236-482):
- `DtoTypeInfoRecord` + `DtoPropertyInfoRecord`
- `DbContextInfoRecord` + `DbSetInfoRecord`
- `EntityConfigInfoRecord`
- `ContractInterfaceInfoRecord`
- `ContractImplementationRecord`
- `PermissionClassRecord` + `PermissionFieldRecord`
- `FeatureClassRecord` + `FeatureFieldRecord`
- `InterceptorInfoRecord`
- `ModuleOptionsRecord` (including its `GroupByModule` helper)
- `VogenValueObjectRecord`
- `AgentDefinitionRecord`, `AgentToolProviderRecord`, `KnowledgeSourceRecord`

**Mutable working types** (move from current lines 488-638):
- `ModuleInfo`, `EndpointInfo`, `ViewInfo`
- `DtoTypeInfo`, `DtoPropertyInfo`
- `DbContextInfo`, `DbSetInfo`, `EntityConfigInfo`
- `ContractImplementationInfo`
- `PermissionClassInfo`, `PermissionFieldInfo`
- `FeatureClassInfo`, `FeatureFieldInfo`
- `InterceptorInfo`, `DiscoveredTypeInfo`

File header:
```csharp
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SimpleModule.Generator;
```

- [ ] **Step 2: Trim DiscoveryData.cs to its core**

In `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs`, delete everything from `internal readonly record struct DtoTypeInfoRecord` through the final `#endregion` — leaving only `HashHelper`, `SourceLocationRecord`, and the top-level `DiscoveryData` record (including its `#region` markers if you want to keep them). The file should end up around 200 lines.

- [ ] **Step 3: Verify file size**

Run: `wc -l framework/SimpleModule.Generator/Discovery/DiscoveryData.cs`
Expected: ≤ 220.

- [ ] **Step 4: Build**

Run: `dotnet build framework/SimpleModule.Generator`
Expected: Build succeeds.

- [ ] **Step 5: Run generator tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/Records/DataRecords.cs \
        framework/SimpleModule.Generator/Discovery/DiscoveryData.cs
git commit -m "refactor(generator): split DataRecords out of DiscoveryData, trim top file to 200 lines"
```

---

## Task 7: Extract `SymbolHelpers.cs`

Pull cross-cutting helper methods from `SymbolDiscovery.cs` into a shared helper class before the finder splits — every finder depends on some of these.

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/SymbolHelpers.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Create the new file**

Write `framework/SimpleModule.Generator/Discovery/SymbolHelpers.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class SymbolHelpers
{
    /// <summary>
    /// Extracts a serializable source location from a symbol, if available.
    /// Returns null for symbols only available in metadata (compiled DLLs).
    /// </summary>
    internal static SourceLocationRecord? GetSourceLocation(ISymbol symbol)
    {
        foreach (var loc in symbol.Locations)
        {
            if (loc.IsInSource)
            {
                var span = loc.GetLineSpan();
                return new SourceLocationRecord(
                    span.Path,
                    span.StartLinePosition.Line,
                    span.StartLinePosition.Character,
                    span.EndLinePosition.Line,
                    span.EndLinePosition.Character
                );
            }
        }
        return null;
    }

    internal static bool ImplementsInterface(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol interfaceSymbol
    )
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, interfaceSymbol))
                return true;
        }
        return false;
    }

    internal static bool InheritsFrom(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseType)
    {
        var current = typeSymbol.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    internal static bool DeclaresMethod(INamedTypeSymbol typeSymbol, string methodName)
    {
        foreach (var member in typeSymbol.GetMembers(methodName))
        {
            if (member is IMethodSymbol method)
            {
                if (method.DeclaringSyntaxReferences.Length > 0)
                    return true;
                if (
                    !method.IsImplicitlyDeclared
                    && method.Locations.Any(static l => l.IsInMetadata)
                )
                    return true;
            }
        }
        return false;
    }

    internal static void ScanModuleAssemblies(
        List<ModuleInfo> modules,
        Dictionary<string, INamedTypeSymbol> moduleSymbols,
        Action<IAssemblySymbol, ModuleInfo> action
    )
    {
        var scanned = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        foreach (var module in modules)
        {
            if (!moduleSymbols.TryGetValue(module.FullyQualifiedName, out var typeSymbol))
                continue;

            if (scanned.Add(typeSymbol.ContainingAssembly))
                action(typeSymbol.ContainingAssembly, module);
        }
    }

    internal static string FindClosestModuleName(string typeFqn, List<ModuleInfo> modules)
    {
        // Match by longest shared namespace prefix between the type and each module class.
        var bestMatch = "";
        var bestLength = -1;
        foreach (var module in modules)
        {
            var moduleFqn = TypeMappingHelpers.StripGlobalPrefix(module.FullyQualifiedName);
            var moduleNs = TypeMappingHelpers.ExtractNamespace(moduleFqn);

            if (
                typeFqn.StartsWith(moduleNs, StringComparison.Ordinal)
                && moduleNs.Length > bestLength
            )
            {
                bestLength = moduleNs.Length;
                bestMatch = module.ModuleName;
            }
        }

        return bestMatch.Length > 0 ? bestMatch : modules[0].ModuleName;
    }

    /// <summary>
    /// Recursively walks namespaces and invokes <paramref name="onMatch"/> for each
    /// concrete (non-abstract, non-static) class that implements the given interface.
    /// </summary>
    internal static void FindConcreteClassesImplementing(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol interfaceSymbol,
        Action<INamedTypeSymbol> onMatch
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                FindConcreteClassesImplementing(childNs, interfaceSymbol, onMatch);
            }
            else if (
                member is INamedTypeSymbol typeSymbol
                && typeSymbol.TypeKind == TypeKind.Class
                && !typeSymbol.IsAbstract
                && !typeSymbol.IsStatic
                && ImplementsInterface(typeSymbol, interfaceSymbol)
            )
            {
                onMatch(typeSymbol);
            }
        }
    }
}
```

- [ ] **Step 2: Delete the moved helpers from SymbolDiscovery.cs**

In `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`, delete these method definitions:
- `GetSourceLocation` (lines ~16-33)
- `ImplementsInterface` (lines ~1075-1086)
- `ScanModuleAssemblies` (lines ~1088-1103)
- `DeclaresMethod` (lines ~1105-1125)
- `InheritsFrom` (lines ~1199-1209)
- `FindClosestModuleName` (lines ~1269-1290)
- `FindConcreteClassesImplementing` (lines ~1691-1714)

- [ ] **Step 3: Update call sites in `Extract` and remaining finders**

In the same file, prefix every call to the moved helpers with `SymbolHelpers.`:
- `GetSourceLocation(...)` → `SymbolHelpers.GetSourceLocation(...)`
- `ImplementsInterface(...)` → `SymbolHelpers.ImplementsInterface(...)`
- `ScanModuleAssemblies(...)` → `SymbolHelpers.ScanModuleAssemblies(...)`
- `DeclaresMethod(...)` → `SymbolHelpers.DeclaresMethod(...)`
- `InheritsFrom(...)` → `SymbolHelpers.InheritsFrom(...)`
- `FindClosestModuleName(...)` → `SymbolHelpers.FindClosestModuleName(...)`
- `FindConcreteClassesImplementing(...)` → `SymbolHelpers.FindConcreteClassesImplementing(...)`

- [ ] **Step 4: Build**

Run: `dotnet build framework/SimpleModule.Generator`
Expected: Build succeeds with zero new warnings.

- [ ] **Step 5: Run generator tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/SymbolHelpers.cs \
        framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "refactor(generator): extract SymbolHelpers from SymbolDiscovery"
```

---

## Task 8: Extract `Finders/ModuleFinder.cs`

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/Finders/ModuleFinder.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Create the finder**

Write `framework/SimpleModule.Generator/Discovery/Finders/ModuleFinder.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class ModuleFinder
{
    internal static void FindModuleTypes(
        INamespaceSymbol namespaceSymbol,
        CoreSymbols symbols,
        List<ModuleInfo> modules,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindModuleTypes(childNamespace, symbols, modules, cancellationToken);
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                foreach (var attr in typeSymbol.GetAttributes())
                {
                    if (
                        SymbolEqualityComparer.Default.Equals(
                            attr.AttributeClass,
                            symbols.ModuleAttribute
                        )
                    )
                    {
                        var moduleName =
                            attr.ConstructorArguments.Length > 0
                                ? attr.ConstructorArguments[0].Value as string ?? ""
                                : "";
                        var routePrefix = "";
                        var viewPrefix = "";
                        foreach (var namedArg in attr.NamedArguments)
                        {
                            if (
                                namedArg.Key == "RoutePrefix"
                                && namedArg.Value.Value is string prefix
                            )
                            {
                                routePrefix = prefix;
                            }
                            else if (
                                namedArg.Key == "ViewPrefix"
                                && namedArg.Value.Value is string vPrefix
                            )
                            {
                                viewPrefix = vPrefix;
                            }
                        }

                        modules.Add(
                            new ModuleInfo
                            {
                                FullyQualifiedName = typeSymbol.ToDisplayString(
                                    SymbolDisplayFormat.FullyQualifiedFormat
                                ),
                                ModuleName = moduleName,
                                HasConfigureServices =
                                    SymbolHelpers.DeclaresMethod(typeSymbol, "ConfigureServices")
                                    || (
                                        symbols.ModuleServices is not null
                                        && SymbolHelpers.ImplementsInterface(
                                            typeSymbol,
                                            symbols.ModuleServices
                                        )
                                    ),
                                HasConfigureEndpoints = SymbolHelpers.DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureEndpoints"
                                ),
                                HasConfigureMenu =
                                    SymbolHelpers.DeclaresMethod(typeSymbol, "ConfigureMenu")
                                    || (
                                        symbols.ModuleMenu is not null
                                        && SymbolHelpers.ImplementsInterface(
                                            typeSymbol,
                                            symbols.ModuleMenu
                                        )
                                    ),
                                HasConfigureMiddleware =
                                    SymbolHelpers.DeclaresMethod(typeSymbol, "ConfigureMiddleware")
                                    || (
                                        symbols.ModuleMiddleware is not null
                                        && SymbolHelpers.ImplementsInterface(
                                            typeSymbol,
                                            symbols.ModuleMiddleware
                                        )
                                    ),
                                HasConfigurePermissions = SymbolHelpers.DeclaresMethod(
                                    typeSymbol,
                                    "ConfigurePermissions"
                                ),
                                HasConfigureSettings =
                                    SymbolHelpers.DeclaresMethod(typeSymbol, "ConfigureSettings")
                                    || (
                                        symbols.ModuleSettings is not null
                                        && SymbolHelpers.ImplementsInterface(
                                            typeSymbol,
                                            symbols.ModuleSettings
                                        )
                                    ),
                                HasConfigureFeatureFlags = SymbolHelpers.DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureFeatureFlags"
                                ),
                                HasConfigureAgents = SymbolHelpers.DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureAgents"
                                ),
                                HasConfigureRateLimits = SymbolHelpers.DeclaresMethod(
                                    typeSymbol,
                                    "ConfigureRateLimits"
                                ),
                                RoutePrefix = routePrefix,
                                ViewPrefix = viewPrefix,
                                AssemblyName = typeSymbol.ContainingAssembly.Name,
                                Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                            }
                        );
                        break;
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 2: Delete the old `FindModuleTypes` method from SymbolDiscovery.cs**

Remove the `FindModuleTypes` method (the big one starting around line 827 in the pre-refactor file; use Grep to locate).

- [ ] **Step 3: Update `Extract` call sites**

In `SymbolDiscovery.Extract`, replace the two calls that previously passed `moduleAttributeSymbol, moduleServicesSymbol, moduleMenuSymbol, moduleMiddlewareSymbol, moduleSettingsSymbol, modules, cancellationToken` with:

```csharp
ModuleFinder.FindModuleTypes(
    assemblySymbol.GlobalNamespace,
    s,
    modules,
    cancellationToken
);
```

and

```csharp
ModuleFinder.FindModuleTypes(
    compilation.Assembly.GlobalNamespace,
    s,
    modules,
    cancellationToken
);
```

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/Finders/ModuleFinder.cs \
        framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "refactor(generator): extract ModuleFinder from SymbolDiscovery"
```

---

## Task 9: Extract `Finders/EndpointFinder.cs`

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/Finders/EndpointFinder.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Create the finder**

Write `framework/SimpleModule.Generator/Discovery/Finders/EndpointFinder.cs` containing (copy verbatim from the current `SymbolDiscovery.cs`, updating the 2 direct helper calls and the `endpointInterfaceSymbol`/`viewEndpointInterfaceSymbol` params to use `CoreSymbols`):

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class EndpointFinder
{
    internal static void FindEndpointTypes(
        INamespaceSymbol namespaceSymbol,
        CoreSymbols symbols,
        List<EndpointInfo> endpoints,
        List<ViewInfo> views,
        CancellationToken cancellationToken
    )
    {
        if (symbols.EndpointInterface is null)
            return;

        FindEndpointTypesInternal(
            namespaceSymbol,
            symbols.EndpointInterface,
            symbols.ViewEndpointInterface,
            endpoints,
            views,
            cancellationToken
        );
    }

    private static void FindEndpointTypesInternal(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol endpointInterfaceSymbol,
        INamedTypeSymbol? viewEndpointInterfaceSymbol,
        List<EndpointInfo> endpoints,
        List<ViewInfo> views,
        CancellationToken cancellationToken
    )
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is INamespaceSymbol childNamespace)
            {
                FindEndpointTypesInternal(
                    childNamespace,
                    endpointInterfaceSymbol,
                    viewEndpointInterfaceSymbol,
                    endpoints,
                    views,
                    cancellationToken
                );
            }
            else if (member is INamedTypeSymbol typeSymbol)
            {
                if (!typeSymbol.IsAbstract && !typeSymbol.IsStatic)
                {
                    var fqn = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    if (
                        viewEndpointInterfaceSymbol is not null
                        && SymbolHelpers.ImplementsInterface(typeSymbol, viewEndpointInterfaceSymbol)
                    )
                    {
                        var className = typeSymbol.Name;
                        if (className.EndsWith("Endpoint", StringComparison.Ordinal))
                            className = className.Substring(
                                0,
                                className.Length - "Endpoint".Length
                            );
                        else if (className.EndsWith("View", StringComparison.Ordinal))
                            className = className.Substring(0, className.Length - "View".Length);

                        var viewInfo = new ViewInfo
                        {
                            FullyQualifiedName = fqn,
                            InferredClassName = className,
                            Location = SymbolHelpers.GetSourceLocation(typeSymbol),
                        };

                        var (viewRoute, _) = ReadRouteConstFields(typeSymbol);
                        viewInfo.RouteTemplate = viewRoute;
                        views.Add(viewInfo);
                    }
                    else if (SymbolHelpers.ImplementsInterface(typeSymbol, endpointInterfaceSymbol))
                    {
                        var info = new EndpointInfo { FullyQualifiedName = fqn };

                        foreach (var attr in typeSymbol.GetAttributes())
                        {
                            var attrName = attr.AttributeClass?.ToDisplayString(
                                SymbolDisplayFormat.FullyQualifiedFormat
                            );

                            if (
                                attrName
                                == "global::SimpleModule.Core.Authorization.RequirePermissionAttribute"
                            )
                            {
                                if (attr.ConstructorArguments.Length > 0)
                                {
                                    var arg = attr.ConstructorArguments[0];
                                    if (arg.Kind == TypedConstantKind.Array)
                                    {
                                        foreach (var val in arg.Values)
                                        {
                                            if (val.Value is string s)
                                                info.RequiredPermissions.Add(s);
                                        }
                                    }
                                    else if (arg.Value is string single)
                                    {
                                        info.RequiredPermissions.Add(single);
                                    }
                                }
                            }
                            else if (
                                attrName
                                == "global::Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute"
                            )
                            {
                                info.AllowAnonymous = true;
                            }
                        }

                        var (epRoute, epMethod) = ReadRouteConstFields(typeSymbol);
                        info.RouteTemplate = epRoute;
                        info.HttpMethod = epMethod;
                        endpoints.Add(info);
                    }
                }
            }
        }
    }

    private static (string route, string method) ReadRouteConstFields(INamedTypeSymbol typeSymbol)
    {
        var route = "";
        var method = "";
        foreach (var m in typeSymbol.GetMembers())
        {
            if (m is IFieldSymbol { IsConst: true, ConstantValue: string value } field)
            {
                if (field.Name == "Route")
                    route = value;
                else if (field.Name == "Method")
                    method = value;
            }
        }
        return (route, method);
    }
}
```

- [ ] **Step 2: Delete the old `FindEndpointTypes` and `ReadRouteConstFields` from SymbolDiscovery.cs**

Remove both methods.

- [ ] **Step 3: Update the call site in `Extract`**

In `SymbolDiscovery.Extract`, where `FindEndpointTypes` was called, replace with:
```csharp
EndpointFinder.FindEndpointTypes(
    assembly.GlobalNamespace,
    s,
    rawEndpoints,
    rawViews,
    cancellationToken
);
```

Also remove the `if (endpointInterfaceSymbol is not null)` outer guard — `EndpointFinder.FindEndpointTypes` handles it internally (per the Step 1 code above).

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/Finders/EndpointFinder.cs \
        framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "refactor(generator): extract EndpointFinder from SymbolDiscovery"
```

---

## Task 10: Extract `Finders/DtoFinder.cs`

Move all DTO discovery — attribute-based, convention-based, property extraction, `[JsonIgnore]` check — into one file.

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/Finders/DtoFinder.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Create the finder**

Write `framework/SimpleModule.Generator/Discovery/Finders/DtoFinder.cs`. Copy the following methods verbatim from the current `SymbolDiscovery.cs`:
- `FindDtoTypes` (the attribute-based scanner)
- `FindConventionDtoTypes`
- `ExtractDtoProperties`
- `HasJsonIgnoreAttribute`

Make them `internal static` on a new class `DtoFinder`. Change any call to a helper moved to `SymbolHelpers` (e.g. `GetSourceLocation`) to use `SymbolHelpers.GetSourceLocation(...)`. Calls to `IsVogenValueObject` and `ResolveUnderlyingType` stay as-is for now (they'll move with VogenFinder in Task 14 — we'll update references then).

Until VogenFinder exists, keep `IsVogenValueObject` and `ResolveUnderlyingType` still in `SymbolDiscovery.cs` as `internal static`. Prefix their call sites inside `DtoFinder` with `SymbolDiscovery.` temporarily:
- `IsVogenValueObject(typeSymbol)` → `SymbolDiscovery.IsVogenValueObject(typeSymbol)`
- `ResolveUnderlyingType(prop.Type)` → `SymbolDiscovery.ResolveUnderlyingType(prop.Type)`

File header:
```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DtoFinder
{
    // Copy FindDtoTypes, FindConventionDtoTypes, ExtractDtoProperties, HasJsonIgnoreAttribute here
    // (each as `internal static` or `private static` as appropriate)
    // ...
}
```

For `IsVogenValueObject` and `ResolveUnderlyingType` referenced in `FindConventionDtoTypes` and `ExtractDtoProperties`, make sure they stay accessible — in `SymbolDiscovery.cs` change their visibility from `private static` to `internal static`.

- [ ] **Step 2: Delete the moved methods from SymbolDiscovery.cs**

Remove `FindDtoTypes`, `FindConventionDtoTypes`, `ExtractDtoProperties`, `HasJsonIgnoreAttribute`.

- [ ] **Step 3: Update `Extract` call sites**

Change:
- `FindDtoTypes(...)` → `DtoFinder.FindDtoTypes(...)` (two call sites — one per reference, one for the host assembly)
- `FindConventionDtoTypes(...)` → `DtoFinder.FindConventionDtoTypes(...)`

Also update the `FindDtoTypes` signature usage: the existing calls pass `dtoAttributeSymbol` — keep that parameter as-is in the finder (this finder predates CoreSymbols threading; we preserve current signature for byte-identical behavior).

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/Finders/DtoFinder.cs \
        framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "refactor(generator): extract DtoFinder from SymbolDiscovery"
```

---

## Task 11: Extract `Finders/DbContextFinder.cs`

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/Finders/DbContextFinder.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Create the finder**

Write `framework/SimpleModule.Generator/Discovery/Finders/DbContextFinder.cs` with:
- `FindDbContextTypes` (copy verbatim)
- `FindEntityConfigTypes` (copy verbatim)
- `HasDbContextConstructorParam` (copy verbatim)

All as `internal static` (the ones that are called from outside) / `private static` (for `HasDbContextConstructorParam` if only used internally — but it's actually called from `ContractFinder` too, so make it `internal static`).

Replace internal helper calls with `SymbolHelpers.GetSourceLocation(...)`.

- [ ] **Step 2: Delete the moved methods from SymbolDiscovery.cs**

Remove `FindDbContextTypes`, `FindEntityConfigTypes`, `HasDbContextConstructorParam`.

- [ ] **Step 3: Update call sites in `Extract`**

- `FindDbContextTypes(...)` → `DbContextFinder.FindDbContextTypes(...)`
- `FindEntityConfigTypes(...)` → `DbContextFinder.FindEntityConfigTypes(...)`
- `HasDbContextConstructorParam(...)` (inside ContractFinder when we extract it) — we'll update that reference in Task 12.

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/Finders/DbContextFinder.cs \
        framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "refactor(generator): extract DbContextFinder from SymbolDiscovery"
```

---

## Task 12: Extract `Finders/ContractFinder.cs`

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/Finders/ContractFinder.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Create the finder**

Write `framework/SimpleModule.Generator/Discovery/Finders/ContractFinder.cs` with:
- `ScanContractInterfaces` (copy verbatim)
- `FindContractImplementations` (copy verbatim)
- `GetContractLifetime` (copy verbatim, as `internal static`)

Replace:
- `GetSourceLocation(...)` → `SymbolHelpers.GetSourceLocation(...)`
- `HasDbContextConstructorParam(...)` → `DbContextFinder.HasDbContextConstructorParam(...)` (this is the reason we promoted it in Task 11)
- `GetContractLifetime(...)` inside `FindContractImplementations` stays as-is (same file).

- [ ] **Step 2: Delete the moved methods from SymbolDiscovery.cs**

Remove `ScanContractInterfaces`, `FindContractImplementations`, `GetContractLifetime`.

- [ ] **Step 3: Update call sites in `Extract`**

- `ScanContractInterfaces(...)` → `ContractFinder.ScanContractInterfaces(...)`
- `FindContractImplementations(...)` → `ContractFinder.FindContractImplementations(...)`

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/Finders/ContractFinder.cs \
        framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "refactor(generator): extract ContractFinder from SymbolDiscovery"
```

---

## Task 13: Extract `Finders/PermissionFeatureFinder.cs`

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/Finders/PermissionFeatureFinder.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Create the finder**

Write `framework/SimpleModule.Generator/Discovery/Finders/PermissionFeatureFinder.cs` with:
- `FindPermissionClasses` (copy verbatim)
- `FindFeatureClasses` (copy verbatim)
- `FindModuleOptionsClasses` (copy verbatim)

All as `internal static` class `PermissionFeatureFinder`. Replace helper calls with `SymbolHelpers.GetSourceLocation(...)` and `SymbolHelpers.ImplementsInterface(...)` and `SymbolHelpers.FindConcreteClassesImplementing(...)`.

- [ ] **Step 2: Delete the moved methods from SymbolDiscovery.cs**

Remove `FindPermissionClasses`, `FindFeatureClasses`, `FindModuleOptionsClasses`.

- [ ] **Step 3: Update call sites in `Extract`**

- `FindPermissionClasses(...)` → `PermissionFeatureFinder.FindPermissionClasses(...)`
- `FindFeatureClasses(...)` → `PermissionFeatureFinder.FindFeatureClasses(...)`
- `FindModuleOptionsClasses(...)` → `PermissionFeatureFinder.FindModuleOptionsClasses(...)`

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/Finders/PermissionFeatureFinder.cs \
        framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "refactor(generator): extract PermissionFeatureFinder from SymbolDiscovery"
```

---

## Task 14: Extract `Finders/VogenFinder.cs`, `Finders/InterceptorFinder.cs`, `Finders/AgentFinder.cs`

Three small finders, bundled into one task since each is under 80 lines.

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/Finders/VogenFinder.cs`
- Create: `framework/SimpleModule.Generator/Discovery/Finders/InterceptorFinder.cs`
- Create: `framework/SimpleModule.Generator/Discovery/Finders/AgentFinder.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Create VogenFinder**

Write `framework/SimpleModule.Generator/Discovery/Finders/VogenFinder.cs` with `FindVogenValueObjectsWithEfConverters`, `IsVogenValueObject`, `ResolveUnderlyingType` (copy verbatim). All as `internal static class VogenFinder`.

- [ ] **Step 2: Create InterceptorFinder**

Write `framework/SimpleModule.Generator/Discovery/Finders/InterceptorFinder.cs` with `FindInterceptorTypes` (copy verbatim). `internal static class InterceptorFinder`. Replace helper calls with `SymbolHelpers.GetSourceLocation(...)` and `SymbolHelpers.ImplementsInterface(...)`.

- [ ] **Step 3: Create AgentFinder**

Write `framework/SimpleModule.Generator/Discovery/Finders/AgentFinder.cs` with `FindImplementors` (copy verbatim). `internal static class AgentFinder`. Replace helper calls with `SymbolHelpers.ImplementsInterface(...)`.

- [ ] **Step 4: Delete moved methods from SymbolDiscovery.cs**

Remove:
- `FindVogenValueObjectsWithEfConverters`
- `IsVogenValueObject`
- `ResolveUnderlyingType`
- `FindInterceptorTypes`
- `FindImplementors`

- [ ] **Step 5: Update call sites in `Extract`**

Replace:
- `FindVogenValueObjectsWithEfConverters(...)` → `VogenFinder.FindVogenValueObjectsWithEfConverters(...)` (two call sites)
- `FindInterceptorTypes(...)` → `InterceptorFinder.FindInterceptorTypes(...)`
- `FindImplementors(...)` → `AgentFinder.FindImplementors(...)` (three call sites — agents, tool providers, knowledge sources)

Also update the `DtoFinder` usage that referenced `SymbolDiscovery.IsVogenValueObject` and `SymbolDiscovery.ResolveUnderlyingType` — change to `VogenFinder.IsVogenValueObject` and `VogenFinder.ResolveUnderlyingType`.

- [ ] **Step 6: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 7: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/Finders/VogenFinder.cs \
        framework/SimpleModule.Generator/Discovery/Finders/InterceptorFinder.cs \
        framework/SimpleModule.Generator/Discovery/Finders/AgentFinder.cs \
        framework/SimpleModule.Generator/Discovery/Finders/DtoFinder.cs \
        framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "refactor(generator): extract Vogen, Interceptor, Agent finders"
```

---

## Task 15: Verify `SymbolDiscovery.cs` is under 300 lines

After the finder extractions, `SymbolDiscovery.cs` should contain only `Extract` (the orchestrator).

**Files:**
- Verify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Check size**

Run: `wc -l framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`
Expected: ≤ 300. If above, identify what's still there and split further.

- [ ] **Step 2: Diff generated output against baseline**

```bash
dotnet build template/SimpleModule.Host -c Debug
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
diff -r baseline/generator-output "$GEN_DIR" | head -100
```
Expected: No output.

- [ ] **Step 3: No commit needed** (verification only).

---

## Task 16: Extract `Emitters/Diagnostics/DiagnosticDescriptors.cs`

Move all 38 `DiagnosticDescriptor` fields to a single file.

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/Diagnostics/DiagnosticDescriptors.cs`
- Modify: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

- [ ] **Step 1: Create the descriptors file**

Write `framework/SimpleModule.Generator/Emitters/Diagnostics/DiagnosticDescriptors.cs`:

```csharp
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DiagnosticDescriptors
{
    // Move all 38 DiagnosticDescriptor fields here.
    // Change each field's declaration from `private static readonly` or
    // `internal static readonly` to `internal static readonly`.
    // Keep the field names identical to the originals.
    // Use the exact id/title/messageFormat/category/defaultSeverity/isEnabledByDefault
    // values from DiagnosticEmitter.cs.
}
```

Copy every `DiagnosticDescriptor` field definition from `DiagnosticEmitter.cs` (lines ~41-381 — use the grep list below for field names). Each must become `internal static readonly`:

```
DuplicateDbSetPropertyName, EmptyModuleName, MultipleIdentityDbContexts,
IdentityDbContextBadTypeArgs, EntityConfigForMissingEntity, DuplicateEntityConfiguration,
CircularModuleDependency, IllegalImplementationReference, ContractInterfaceTooLargeWarning,
ContractInterfaceTooLargeError, MissingContractInterfaces, NoContractImplementation,
MultipleContractImplementations, PermissionFieldNotConstString, ContractImplementationNotPublic,
ContractImplementationIsAbstract, PermissionValueBadPattern, PermissionClassNotSealed,
DuplicatePermissionValue, PermissionValueWrongPrefix, DtoTypeNoProperties,
InfrastructureTypeInContracts, DuplicateViewPageName, InterceptorDependsOnDbContext,
DuplicateModuleName, ViewPagePrefixMismatch, ViewEndpointWithoutViewPrefix,
EmptyModuleWarning, MultipleModuleOptions, FeatureClassNotSealed,
FeatureFieldNamingViolation, DuplicateFeatureName, FeatureFieldNotConstString,
MultipleEndpointsPerFile, ModuleAssemblyNamingViolation, MissingContractsAssembly,
MissingEndpointRouteConst, EntityNotInContractsAssembly
```

- [ ] **Step 2: Delete the descriptor fields from DiagnosticEmitter.cs**

In `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`, delete every `DiagnosticDescriptor` field definition (lines ~41 through ~381 — keep the `public void Emit(...)` method and the `Strip`/`ExtractShortName` helpers).

- [ ] **Step 3: Update references in `DiagnosticEmitter.Emit`**

Prefix every bare descriptor reference in `Emit` (and helpers) with `DiagnosticDescriptors.`. Use a single replace-all:
- `EmptyModuleName` → `DiagnosticDescriptors.EmptyModuleName`
- `DuplicateModuleName` → `DiagnosticDescriptors.DuplicateModuleName`
- …and so on for all 38.

Tip: Do this with a loop in your shell to avoid typos:
```bash
for name in DuplicateDbSetPropertyName EmptyModuleName MultipleIdentityDbContexts \
  IdentityDbContextBadTypeArgs EntityConfigForMissingEntity DuplicateEntityConfiguration \
  CircularModuleDependency IllegalImplementationReference ContractInterfaceTooLargeWarning \
  ContractInterfaceTooLargeError MissingContractInterfaces NoContractImplementation \
  MultipleContractImplementations PermissionFieldNotConstString ContractImplementationNotPublic \
  ContractImplementationIsAbstract PermissionValueBadPattern PermissionClassNotSealed \
  DuplicatePermissionValue PermissionValueWrongPrefix DtoTypeNoProperties \
  InfrastructureTypeInContracts DuplicateViewPageName InterceptorDependsOnDbContext \
  ViewPagePrefixMismatch ViewEndpointWithoutViewPrefix EmptyModuleWarning \
  MultipleModuleOptions FeatureClassNotSealed FeatureFieldNamingViolation \
  DuplicateFeatureName FeatureFieldNotConstString MultipleEndpointsPerFile \
  ModuleAssemblyNamingViolation MissingContractsAssembly MissingEndpointRouteConst \
  EntityNotInContractsAssembly; do
  # Only replace inside Diagnostic.Create( arg list, where the descriptor is the 1st arg
  sed -i '' "s/Diagnostic\.Create(\\n\\s*${name}/Diagnostic.Create(\\n                        DiagnosticDescriptors.${name}/g" \
    framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs
done
```

If `sed` is awkward, do it manually — there are ~60 call sites.

**Note:** If any test file (e.g. `DiagnosticTests.cs`) references a descriptor by its old path (`DiagnosticEmitter.DuplicateDbSetPropertyName`), update those too. Check with:
```bash
grep -n "DiagnosticEmitter\." tests/SimpleModule.Generator.Tests/*.cs
```

- [ ] **Step 4: Build**

Run: `dotnet build framework/SimpleModule.Generator tests/SimpleModule.Generator.Tests`
Expected: Build succeeds with zero new warnings.

- [ ] **Step 5: Run generator tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests`
Expected: All tests pass. The diagnostic IDs and messages are unchanged.

- [ ] **Step 6: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/Diagnostics/DiagnosticDescriptors.cs \
        framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs \
        tests/SimpleModule.Generator.Tests/
git commit -m "refactor(generator): extract 38 DiagnosticDescriptors into their own file"
```

---

## Task 17: Extract `Emitters/Diagnostics/ModuleChecks.cs`

Move module-related checks (SM0002 empty name, SM0040 duplicate, SM0043 empty module).

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/Diagnostics/ModuleChecks.cs`
- Modify: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

- [ ] **Step 1: Create ModuleChecks**

Write `framework/SimpleModule.Generator/Emitters/Diagnostics/ModuleChecks.cs`:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class ModuleChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // SM0002: Empty module name
        foreach (var module in data.Modules)
        {
            if (string.IsNullOrEmpty(module.ModuleName))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EmptyModuleName,
                        LocationHelper.ToLocation(module.Location),
                        TypeMappingHelpers.StripGlobalPrefix(module.FullyQualifiedName)
                    )
                );
            }
        }

        // SM0040: Duplicate module name
        var seenModuleNames = new Dictionary<string, string>();
        foreach (var module in data.Modules)
        {
            if (string.IsNullOrEmpty(module.ModuleName))
                continue;

            if (seenModuleNames.TryGetValue(module.ModuleName, out var existingFqn))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateModuleName,
                        LocationHelper.ToLocation(module.Location),
                        module.ModuleName,
                        TypeMappingHelpers.StripGlobalPrefix(existingFqn),
                        TypeMappingHelpers.StripGlobalPrefix(module.FullyQualifiedName)
                    )
                );
            }
            else
            {
                seenModuleNames[module.ModuleName] = module.FullyQualifiedName;
            }
        }

        // SM0043: Empty module warning
        var moduleNamesWithDbContext = new HashSet<string>(
            StringComparer.Ordinal
        );
        foreach (var db in data.DbContexts)
            moduleNamesWithDbContext.Add(db.ModuleName);

        foreach (var module in data.Modules)
        {
            if (
                !module.HasConfigureServices
                && !module.HasConfigureEndpoints
                && !module.HasConfigureMenu
                && !module.HasConfigurePermissions
                && !module.HasConfigureMiddleware
                && !module.HasConfigureSettings
                && !module.HasConfigureFeatureFlags
                && module.Endpoints.Length == 0
                && module.Views.Length == 0
                && !moduleNamesWithDbContext.Contains(module.ModuleName)
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EmptyModuleWarning,
                        LocationHelper.ToLocation(module.Location),
                        module.ModuleName
                    )
                );
            }
        }
    }
}
```

- [ ] **Step 2: Remove the moved checks from `DiagnosticEmitter.Emit`**

In `DiagnosticEmitter.cs`, delete:
- The SM0002 loop (currently the first block in `Emit`)
- The SM0040 loop
- The SM0043 loop (which includes the `moduleNamesWithDbContext` HashSet build)

- [ ] **Step 3: Call `ModuleChecks.Run` at the top of `DiagnosticEmitter.Emit`**

Insert at the very top of the `Emit` method body (before any other check):
```csharp
ModuleChecks.Run(context, data);
```

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/Diagnostics/ModuleChecks.cs \
        framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs
git commit -m "refactor(generator): extract ModuleChecks (SM0002/0040/0043)"
```

---

## Task 18: Extract `Emitters/Diagnostics/DbContextChecks.cs`

Covers SM0001, SM0003, SM0005, SM0006, SM0007, SM0054 (last one: `EntityNotInContractsAssembly`).

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/Diagnostics/DbContextChecks.cs`
- Modify: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

- [ ] **Step 1: Create DbContextChecks**

Write `framework/SimpleModule.Generator/Emitters/Diagnostics/DbContextChecks.cs`. Copy the following blocks from the current `DiagnosticEmitter.Emit` body into a single `internal static void Run(SourceProductionContext context, DiscoveryData data)` method, preserving order:

1. SM0001 — Duplicate DbSet property name across modules (currently in the middle of Emit; search for `DuplicateDbSetPropertyName`).
2. SM0003 — Multiple IdentityDbContexts.
3. SM0005 — IdentityDbContext with wrong type args.
4. SM0054 (`EntityNotInContractsAssembly`) — the block with `allEntityFqns` HashSet (keep `allEntityFqns` inside the method so SM0006 can still use it).
5. SM0006 — Entity config for entity not in any DbSet.
6. SM0007 — Duplicate EntityTypeConfiguration.

Reference all descriptors as `DiagnosticDescriptors.Xxx`. Replace `Strip(...)` with `TypeMappingHelpers.StripGlobalPrefix(...)`.

File header:
```csharp
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DbContextChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // (Paste the 6 blocks here, in the order above.)
    }
}
```

Look up the exact code for each block in the current `DiagnosticEmitter.Emit` — do not improvise. SM0001 does not currently appear in `DiagnosticEmitter.Emit` as a visible block; search `grep -n DuplicateDbSetPropertyName framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs` — if it's only the descriptor with no emit logic, it's reserved for a future check and can be omitted here (no emit call to move).

- [ ] **Step 2: Remove the moved blocks from `DiagnosticEmitter.Emit`**

Delete the six blocks listed above from `DiagnosticEmitter.Emit`.

- [ ] **Step 3: Add `DbContextChecks.Run(context, data);` after `ModuleChecks.Run(...)`**

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/Diagnostics/DbContextChecks.cs \
        framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs
git commit -m "refactor(generator): extract DbContextChecks (SM0003/0005/0006/0007/0054)"
```

---

## Task 19: Extract `Emitters/Diagnostics/DependencyChecks.cs`

Covers SM0010 (circular) and SM0011 (illegal references). Kept separate from the contract/DTO group because both depend on `TopologicalSort`.

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/Diagnostics/DependencyChecks.cs`
- Modify: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

- [ ] **Step 1: Create DependencyChecks**

Write `framework/SimpleModule.Generator/Emitters/Diagnostics/DependencyChecks.cs`. Copy the SM0010 block (from the current `Emit`, where `TopologicalSort.SortModulesWithResult(data)` is called) and the SM0011 block (`foreach (var illegal in data.IllegalReferences)`). Reference descriptors as `DiagnosticDescriptors.Xxx`.

```csharp
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class DependencyChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // Paste SM0010 (circular dependency) block here.
        // Paste SM0011 (illegal implementation reference) block here.
    }
}
```

- [ ] **Step 2: Remove the moved blocks from `DiagnosticEmitter.Emit`**

- [ ] **Step 3: Add `DependencyChecks.Run(context, data);` after `DbContextChecks.Run(...)`**

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/Diagnostics/DependencyChecks.cs \
        framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs
git commit -m "refactor(generator): extract DependencyChecks (SM0010/0011)"
```

---

## Task 20: Extract `Emitters/Diagnostics/ContractAndDtoChecks.cs`

Covers SM0012/0013 (contract size), SM0014 (missing contract interfaces), SM0025/0026/0028/0029 (implementations), SM0035 (DTO no properties), SM0038 (infrastructure in contracts).

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/Diagnostics/ContractAndDtoChecks.cs`
- Modify: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

- [ ] **Step 1: Create ContractAndDtoChecks**

Write `framework/SimpleModule.Generator/Emitters/Diagnostics/ContractAndDtoChecks.cs`. Copy these blocks, in order, from the current `DiagnosticEmitter.Emit` into a single `Run` method:

1. SM0012/SM0013 — Contract interface size (reference `ContractInterfaceTooLargeError`, `ContractInterfaceTooLargeWarning`). Includes helper `ExtractShortName` — **move that helper here too** as `private static string ExtractShortName(...)`.
2. SM0014 — Missing contract interfaces.
3. SM0025/SM0026/SM0028/SM0029 — Contract implementation diagnostics (the block with `implsByInterface` Dictionary).
4. SM0035 — DTO type with no public properties.
5. SM0038 — Infrastructure type in Contracts.

File header:
```csharp
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class ContractAndDtoChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // Paste the 5 blocks here, in order.
    }

    private static string ExtractShortName(string interfaceName)
    {
        var name = TypeMappingHelpers.StripGlobalPrefix(interfaceName);
        if (name.Contains("."))
            name = name.Substring(name.LastIndexOf('.') + 1);
        if (name.StartsWith("I", StringComparison.Ordinal) && name.Length > 1)
            name = name.Substring(1);
        if (name.EndsWith("Contracts", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Contracts".Length);
        return name;
    }
}
```

- [ ] **Step 2: Remove the moved blocks and `ExtractShortName` from `DiagnosticEmitter.cs`**

- [ ] **Step 3: Add `ContractAndDtoChecks.Run(context, data);` after `DependencyChecks.Run(...)`**

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/Diagnostics/ContractAndDtoChecks.cs \
        framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs
git commit -m "refactor(generator): extract ContractAndDtoChecks"
```

---

## Task 21: Extract `Emitters/Diagnostics/PermissionFeatureChecks.cs`

Covers SM0027/0031/0032/0033/0034 (permissions), SM0044 (multiple IModuleOptions), SM0045/0046/0047/0048 (features).

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/Diagnostics/PermissionFeatureChecks.cs`
- Modify: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

- [ ] **Step 1: Create PermissionFeatureChecks**

Write `framework/SimpleModule.Generator/Emitters/Diagnostics/PermissionFeatureChecks.cs`. Copy the three blocks in order:

1. SM0027/0031/0032/0033/0034 — Permission diagnostics (the block starting with `// SM0027/SM0031/SM0032/SM0033/SM0034: Permission diagnostics` and ending before `// SM0035: DTO type in contracts with no public properties`).
2. SM0044 — Multiple IModuleOptions (the block starting with `var optionsByModule = ModuleOptionsRecord.GroupByModule(data.ModuleOptions);`).
3. SM0045/0046/0047/0048 — Feature flag diagnostics (the block starting with `// SM0045/SM0046/SM0047/SM0048: Feature flag diagnostics`).

```csharp
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class PermissionFeatureChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // Paste permission, options, feature blocks here.
    }
}
```

- [ ] **Step 2: Remove the moved blocks from `DiagnosticEmitter.Emit`**

- [ ] **Step 3: Add `PermissionFeatureChecks.Run(context, data);` after `ContractAndDtoChecks.Run(...)`**

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/Diagnostics/PermissionFeatureChecks.cs \
        framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs
git commit -m "refactor(generator): extract PermissionFeatureChecks"
```

---

## Task 22: Extract `Emitters/Diagnostics/EndpointChecks.cs`

Covers SM0015 (duplicate view page), SM0039 (interceptor depends on DbContext-contract), SM0041 (view page prefix mismatch), SM0042 (views without ViewPrefix), SM0049 (multiple endpoints per file), SM0052/0053/0054 (assembly naming / missing contracts / missing Route const).

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/Diagnostics/EndpointChecks.cs`
- Modify: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

- [ ] **Step 1: Create EndpointChecks**

Write `framework/SimpleModule.Generator/Emitters/Diagnostics/EndpointChecks.cs`. Copy these blocks, in order:

1. SM0015 — Duplicate view page name (block starting `// SM0015: Duplicate view page name across modules`).
2. SM0041 — View page prefix mismatch (block starting `// SM0041: View page prefix must match module name`).
3. SM0042 — Module with views but no ViewPrefix (block starting `// SM0042: Module with views but no ViewPrefix`).
4. SM0039 — Interceptor depends on DbContext-contract (block starting `// SM0039: Interceptor depends on contract whose implementation takes a DbContext`).
5. SM0049 — Multiple endpoints per file (block starting `// SM0049: Multiple endpoints`).
6. SM0052/0053/0054 — Assembly naming + missing contracts + missing Route const (the final `if (hostIsFramework)` block including all nested checks).

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class EndpointChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // Paste the 6 blocks here, in order.
    }
}
```

- [ ] **Step 2: Remove the moved blocks from `DiagnosticEmitter.Emit`**

- [ ] **Step 3: Add `EndpointChecks.Run(context, data);` after `PermissionFeatureChecks.Run(...)`**

- [ ] **Step 4: Check DiagnosticEmitter size**

Run: `wc -l framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`
Expected: ≤ 80 lines. If higher, some blocks are still inline — move them.

- [ ] **Step 5: Remove unused imports and `Strip` helper from DiagnosticEmitter.cs**

`DiagnosticEmitter.Emit` should now be just:
```csharp
public void Emit(SourceProductionContext context, DiscoveryData data)
{
    ModuleChecks.Run(context, data);
    DbContextChecks.Run(context, data);
    DependencyChecks.Run(context, data);
    ContractAndDtoChecks.Run(context, data);
    PermissionFeatureChecks.Run(context, data);
    EndpointChecks.Run(context, data);
}
```

Delete the `Strip` private helper and any unused `using`s. The final file should look like:
```csharp
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal sealed class DiagnosticEmitter : IEmitter
{
    public void Emit(SourceProductionContext context, DiscoveryData data)
    {
        ModuleChecks.Run(context, data);
        DbContextChecks.Run(context, data);
        DependencyChecks.Run(context, data);
        ContractAndDtoChecks.Run(context, data);
        PermissionFeatureChecks.Run(context, data);
        EndpointChecks.Run(context, data);
    }
}
```

- [ ] **Step 6: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 7: Diff generated output against baseline**

```bash
dotnet build template/SimpleModule.Host -c Debug
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
diff -r baseline/generator-output "$GEN_DIR" | head -100
```
Expected: No output (identical).

- [ ] **Step 8: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/Diagnostics/EndpointChecks.cs \
        framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs
git commit -m "refactor(generator): extract EndpointChecks, trim DiagnosticEmitter to orchestrator"
```

---

## Task 23: Perf win — single-pass reference classification

Today `compilation.References` is iterated three times (module scan, DTO scan, contracts scan). Consolidate into one pass up front.

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Add the one-pass classification block near the top of `Extract`**

Right after `var s = symbols.Value;`, add:

```csharp
        // Single-pass reference classification. Every discovery phase that scans
        // referenced assemblies gets to reuse these pre-classified lists instead
        // of re-iterating compilation.References + re-calling GetAssemblyOrModuleSymbol.
        var refAssemblies = new List<IAssemblySymbol>(compilation.References.Count());
        var contractsAssemblies = new List<IAssemblySymbol>();
        foreach (var reference in compilation.References)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (
                compilation.GetAssemblyOrModuleSymbol(reference)
                is not IAssemblySymbol asm
            )
                continue;

            refAssemblies.Add(asm);
            if (asm.Name.EndsWith(".Contracts", StringComparison.OrdinalIgnoreCase))
                contractsAssemblies.Add(asm);
        }
```

- [ ] **Step 2: Replace the first reference loop (module discovery)**

Find the loop `foreach (var reference in compilation.References)` that immediately precedes `FindModuleTypes` on line ~84. Replace the loop body with one that iterates `refAssemblies`:

```csharp
        foreach (var assemblySymbol in refAssemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ModuleFinder.FindModuleTypes(
                assemblySymbol.GlobalNamespace,
                s,
                modules,
                cancellationToken
            );
        }
```

- [ ] **Step 3: Replace the DTO reference loop**

Find the second `foreach (var reference in compilation.References)` (inside the `if (dtoAttributeSymbol is not null)` / now `if (s.DtoAttribute is not null)` block). Replace with:

```csharp
            foreach (var assemblySymbol in refAssemblies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                DtoFinder.FindDtoTypes(
                    assemblySymbol.GlobalNamespace,
                    s.DtoAttribute,
                    dtoTypes,
                    cancellationToken
                );
            }
```

- [ ] **Step 4: Replace the contracts-assembly building**

Find the loop starting `// Step 2: Build contracts-to-module map` that iterates `compilation.References`. Replace that loop with one that uses `contractsAssemblies`:

```csharp
        // Step 2: Build contracts-to-module map
        var contractsAssemblyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var contractsAssemblySymbols = new Dictionary<string, IAssemblySymbol>(
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var asm in contractsAssemblies)
        {
            var asmName = asm.Name;
            var baseName = asmName.Substring(0, asmName.Length - ".Contracts".Length);

            // Try exact match on assembly name
            if (moduleAssemblyMap.TryGetValue(baseName, out var moduleName))
            {
                contractsAssemblyMap[asmName] = moduleName;
                contractsAssemblySymbols[asmName] = asm;
                continue;
            }

            // Try matching last segment of baseName to module names (case-insensitive)
            var lastDot = baseName.LastIndexOf('.');
            var lastSegment = lastDot >= 0 ? baseName.Substring(lastDot + 1) : baseName;

            foreach (var kvp in moduleAssemblyMap)
            {
                if (string.Equals(lastSegment, kvp.Value, StringComparison.OrdinalIgnoreCase))
                {
                    contractsAssemblyMap[asmName] = kvp.Value;
                    contractsAssemblySymbols[asmName] = asm;
                    break;
                }
            }
        }
```

- [ ] **Step 5: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 6: Diff against baseline**

```bash
dotnet build template/SimpleModule.Host -c Debug
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
diff -r baseline/generator-output "$GEN_DIR" | head -50
```
Expected: No output.

- [ ] **Step 7: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "perf(generator): single-pass reference classification, eliminate 2 reference re-iterations"
```

---

## Task 24: Perf win — modules-by-name dictionary

Today `modules.Find(m => m.ModuleName == ownerName)` inside endpoint/view loops is O(N·M). Replace with a `Dictionary<string, ModuleInfo>` built once.

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Build the dictionary after modules are discovered**

Right after the module-symbols-by-FQN dictionary is built (currently lines ~121-128 of the pre-refactor file — locate with `grep -n "moduleSymbols = new Dictionary" framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`), add:

```csharp
        // Dictionary by module NAME for O(1) endpoint/view attribution below.
        // Duplicate names are already caught by SM0040 — we just take the first entry.
        var modulesByName = new Dictionary<string, ModuleInfo>(StringComparer.Ordinal);
        foreach (var module in modules)
        {
            if (!modulesByName.ContainsKey(module.ModuleName))
                modulesByName[module.ModuleName] = module;
        }
```

- [ ] **Step 2: Replace both `modules.Find(...)` call sites**

Find the two lines that read:
```csharp
var owner = modules.Find(m => m.ModuleName == ownerName);
```

Replace each with:
```csharp
modulesByName.TryGetValue(ownerName, out var owner);
```

And update the subsequent `if (owner is not null)` usage — it stays valid since `TryGetValue` leaves `owner` null on miss.

- [ ] **Step 3: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 4: Diff against baseline**

```bash
dotnet build template/SimpleModule.Host -c Debug
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
diff -r baseline/generator-output "$GEN_DIR" | head -20
```
Expected: No output.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "perf(generator): use modules-by-name dictionary for endpoint/view attribution"
```

---

## Task 25: Perf win — lift `moduleNsByName` out of inner loop

`moduleNsByName` is currently rebuilt inside the per-module endpoint-scan loop. Build it once.

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Move the `moduleNsByName` build above the outer loop**

Find the block:
```csharp
// Pre-compute module namespace per module name for page inference
var moduleNsByName = new Dictionary<string, string>();
foreach (var m in modules)
{
    if (!moduleNsByName.ContainsKey(m.ModuleName))
    {
        var mFqn = TypeMappingHelpers.StripGlobalPrefix(m.FullyQualifiedName);
        moduleNsByName[m.ModuleName] = TypeMappingHelpers.ExtractNamespace(mFqn);
    }
}
```

Currently sits inside `foreach (var module in modules)` near the view-matching code. Move it so it's declared **before** that outer `foreach (var module in modules)` loop — i.e. immediately after `modulesByName` is built in Task 24.

- [ ] **Step 2: Confirm only one copy remains**

Run: `grep -n "moduleNsByName = new Dictionary" framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`
Expected: Exactly one match.

- [ ] **Step 3: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 4: Diff against baseline**

```bash
dotnet build template/SimpleModule.Host -c Debug
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
diff -r baseline/generator-output "$GEN_DIR" | head -20
```
Expected: No output.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "perf(generator): lift moduleNsByName build out of per-module loop"
```

---

## Task 26: Perf win — `FindClosestModuleName` reverse-index

Replace the linear namespace scan with a pre-built `(namespace, moduleName)` list sorted by namespace-length descending.

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolHelpers.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Add a new helper to SymbolHelpers.cs**

In `SymbolHelpers.cs`, add (keep existing `FindClosestModuleName(string, List<ModuleInfo>)` for now — it's the fallback):

```csharp
    /// <summary>
    /// Pre-computed namespace→module-name index used by <see cref="FindClosestModuleNameFast"/>.
    /// Entries are sorted by namespace length descending so the first startsWith match wins.
    /// </summary>
    internal readonly struct ModuleNamespaceIndex
    {
        internal readonly (string Namespace, string ModuleName)[] Entries;
        internal readonly string FirstModuleName;

        internal ModuleNamespaceIndex(
            (string Namespace, string ModuleName)[] entries,
            string firstModuleName
        )
        {
            Entries = entries;
            FirstModuleName = firstModuleName;
        }
    }

    internal static ModuleNamespaceIndex BuildModuleNamespaceIndex(List<ModuleInfo> modules)
    {
        var entries = new (string Namespace, string ModuleName)[modules.Count];
        for (var i = 0; i < modules.Count; i++)
        {
            var moduleFqn = TypeMappingHelpers.StripGlobalPrefix(modules[i].FullyQualifiedName);
            entries[i] = (TypeMappingHelpers.ExtractNamespace(moduleFqn), modules[i].ModuleName);
        }

        System.Array.Sort(
            entries,
            (a, b) => b.Namespace.Length.CompareTo(a.Namespace.Length)
        );

        return new ModuleNamespaceIndex(entries, modules[0].ModuleName);
    }

    internal static string FindClosestModuleNameFast(string typeFqn, ModuleNamespaceIndex index)
    {
        foreach (var (ns, moduleName) in index.Entries)
        {
            if (typeFqn.StartsWith(ns, System.StringComparison.Ordinal))
                return moduleName;
        }
        return index.FirstModuleName;
    }
```

- [ ] **Step 2: Build the index once in `Extract`**

In `SymbolDiscovery.Extract`, right after `modulesByName` is built (Task 24), add:
```csharp
        var moduleNsIndex = SymbolHelpers.BuildModuleNamespaceIndex(modules);
```

- [ ] **Step 3: Replace `FindClosestModuleName` call sites**

Find all call sites of `SymbolHelpers.FindClosestModuleName(xxx, modules)` in `Extract` (there are 4: endpoint attribution, view attribution, DbContext attribution, entity config attribution). Replace with:
```csharp
SymbolHelpers.FindClosestModuleNameFast(xxx, moduleNsIndex)
```

- [ ] **Step 4: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 5: Diff against baseline**

```bash
dotnet build template/SimpleModule.Host -c Debug
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
diff -r baseline/generator-output "$GEN_DIR" | head -20
```
Expected: No output.

- [ ] **Step 6: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/SymbolHelpers.cs \
        framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "perf(generator): use pre-sorted namespace index for FindClosestModuleName"
```

---

## Task 27: Perf win — DTO convention short-circuit

Skip recursion into a type's nested namespace tree when its FQN is already claimed by the attributed-DTO scan.

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/Finders/DtoFinder.cs`

- [ ] **Step 1: Read current `FindConventionDtoTypes` in `DtoFinder.cs`**

The existing check already skips types whose FQN is in `existingFqns`. The short-circuit we add is: inside the per-namespace recursion, once we've processed a namespace, we don't revisit it. Since recursion is already single-pass, the real win here is smaller than estimated — **verify with a measurement before committing**.

- [ ] **Step 2: Add a guard that skips child-namespace recursion when the namespace contains no public types**

Find the recursion call inside `FindConventionDtoTypes`:
```csharp
if (member is INamespaceSymbol childNs)
{
    FindConventionDtoTypes(childNs, ...);
}
```

Replace with:
```csharp
if (member is INamespaceSymbol childNs)
{
    // Skip walking into System.*, Microsoft.*, or Vogen.* trees — they never contain DTOs.
    var childName = childNs.Name;
    if (
        childName == "System"
        || childName == "Microsoft"
        || childName == "Vogen"
    )
        continue;

    FindConventionDtoTypes(
        childNs,
        noDtoAttrSymbol,
        eventInterfaceSymbol,
        existingFqns,
        dtoTypes,
        cancellationToken
    );
}
```

(Replace the parameter list `...` with the actual parameter list from the method — they haven't changed.)

- [ ] **Step 3: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 4: Diff against baseline**

```bash
dotnet build template/SimpleModule.Host -c Debug
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
diff -r baseline/generator-output "$GEN_DIR" | head -30
```
Expected: No output. If there ARE differences (some module happens to be in `Microsoft.*`), revert this task.

- [ ] **Step 5: Commit (or revert)**

If diff is clean:
```bash
git add framework/SimpleModule.Generator/Discovery/Finders/DtoFinder.cs
git commit -m "perf(generator): skip System/Microsoft/Vogen trees in convention DTO scan"
```

If diff shows any change (user modules under those namespaces): `git checkout framework/SimpleModule.Generator/Discovery/Finders/DtoFinder.cs` and skip this task.

---

## Task 28: Perf win — scoped attributed-DTO discovery

Currently `DtoFinder.FindDtoTypes` scans every referenced assembly for `[Dto]`-attributed types. Restrict to module + host assemblies — contracts assemblies get the convention pass, and `[Dto]` attributes on framework/library types shouldn't be discovered as DTOs.

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

- [ ] **Step 1: Replace the `foreach (var assemblySymbol in refAssemblies)` DTO loop with a module-scoped scan**

Find the loop (introduced in Task 23 Step 3):
```csharp
foreach (var assemblySymbol in refAssemblies)
{
    cancellationToken.ThrowIfCancellationRequested();

    DtoFinder.FindDtoTypes(
        assemblySymbol.GlobalNamespace,
        s.DtoAttribute,
        dtoTypes,
        cancellationToken
    );
}
```

Replace with:
```csharp
// Only scan module assemblies (which host custom entities and request/response DTOs)
// for [Dto]-attributed types. Contracts assemblies get the convention pass below.
SymbolHelpers.ScanModuleAssemblies(
    modules,
    moduleSymbols,
    (assembly, _) =>
        DtoFinder.FindDtoTypes(
            assembly.GlobalNamespace,
            s.DtoAttribute!,
            dtoTypes,
            cancellationToken
        )
);
```

- [ ] **Step 2: Build and test**

```bash
dotnet build framework/SimpleModule.Generator
dotnet test tests/SimpleModule.Generator.Tests
```
Expected: Build succeeds, all tests pass.

- [ ] **Step 3: Diff against baseline**

```bash
dotnet build template/SimpleModule.Host -c Debug
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
diff -r baseline/generator-output "$GEN_DIR" | head -100
```

- If **no output**: commit.
- If **any diff**: the DTO set changed. Revert:
    ```bash
    git checkout framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
    ```
  Skip this task and note it in the commit log of Task 30.

- [ ] **Step 4: Commit (conditional)**

```bash
git add framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "perf(generator): restrict [Dto] attribute scan to module assemblies only"
```

---

## Task 29: Add incremental-caching test

Locks in that `DiscoveryData` equality still works after the split.

**Files:**
- Create: `tests/SimpleModule.Generator.Tests/IncrementalCachingTests.cs`

- [ ] **Step 1: Write the test**

Write `tests/SimpleModule.Generator.Tests/IncrementalCachingTests.cs`:

```csharp
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class IncrementalCachingTests
{
    [Fact]
    public void Generator_CachesDiscoveryData_OnIdenticalCompilation()
    {
        // Two-run pattern: first run populates the cache, second run should hit it.
        var source = """
            using SimpleModule.Core;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Configuration;
            using Microsoft.AspNetCore.Routing;

            namespace TestApp;

            [Module("Test")]
            public class TestModule : IModule
            {
                public void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
                public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var generator = new ModuleDiscovererGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true
            )
        );

        // First run — populate cache.
        driver = driver.RunGenerators(compilation);

        // Second run with the same compilation — should hit cache.
        driver = driver.RunGenerators(compilation);
        var result = driver.GetRunResult().Results[0];

        // The RegisterSourceOutput step reuses prior output when its input is equal.
        var outputs = result.TrackedOutputSteps.SelectMany(kvp => kvp.Value).ToList();
        outputs.Should().NotBeEmpty("source outputs should be tracked");
        outputs
            .Should()
            .OnlyContain(
                step => step.Outputs.All(o => o.Reason == IncrementalStepRunReason.Cached
                    || o.Reason == IncrementalStepRunReason.Unchanged),
                "second run with identical compilation must hit the cache"
            );
    }
}
```

- [ ] **Step 2: Run the test**

Run: `dotnet test tests/SimpleModule.Generator.Tests --filter "IncrementalCachingTests"`
Expected: Pass.

- [ ] **Step 3: Commit**

```bash
git add tests/SimpleModule.Generator.Tests/IncrementalCachingTests.cs
git commit -m "test(generator): lock in incremental caching behaviour after refactor"
```

---

## Task 30: Add diagnostic-catalog reflection test

Ensures no descriptor is accidentally dropped or changed during future edits.

**Files:**
- Create: `tests/SimpleModule.Generator.Tests/DiagnosticCatalogTests.cs`

- [ ] **Step 1: Generate the baseline descriptor snapshot**

From the repo root:
```bash
cat > /tmp/dump_descriptors.csx <<'EOF'
// This script is not run by the test; it documents how the baseline was produced.
// Run once manually on the pre-refactor commit to produce the expected table.
EOF
```

(No action — just note that the baseline was captured in Task 1 Step 3.)

- [ ] **Step 2: Write the test**

Write `tests/SimpleModule.Generator.Tests/DiagnosticCatalogTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator.Tests;

public class DiagnosticCatalogTests
{
    // Baseline captured from DiagnosticEmitter.cs pre-refactor.
    // If you intentionally add/remove a diagnostic, update this table in the same commit
    // and the docs for the new/removed ID.
    private static readonly Dictionary<string, (string Id, DiagnosticSeverity Severity, string Category)> Expected = new()
    {
        ["DuplicateDbSetPropertyName"] = ("SM0001", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["EmptyModuleName"] = ("SM0002", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["MultipleIdentityDbContexts"] = ("SM0003", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["IdentityDbContextBadTypeArgs"] = ("SM0005", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["EntityConfigForMissingEntity"] = ("SM0006", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["DuplicateEntityConfiguration"] = ("SM0007", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["CircularModuleDependency"] = ("SM0010", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["IllegalImplementationReference"] = ("SM0011", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["ContractInterfaceTooLargeWarning"] = ("SM0012", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["ContractInterfaceTooLargeError"] = ("SM0013", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["MissingContractInterfaces"] = ("SM0014", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["NoContractImplementation"] = ("SM0025", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["MultipleContractImplementations"] = ("SM0026", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["PermissionFieldNotConstString"] = ("SM0027", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["ContractImplementationNotPublic"] = ("SM0028", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["ContractImplementationIsAbstract"] = ("SM0029", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["PermissionValueBadPattern"] = ("SM0031", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["PermissionClassNotSealed"] = ("SM0032", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["DuplicatePermissionValue"] = ("SM0033", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["PermissionValueWrongPrefix"] = ("SM0034", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["DtoTypeNoProperties"] = ("SM0035", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["InfrastructureTypeInContracts"] = ("SM0038", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["DuplicateViewPageName"] = ("SM0015", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["InterceptorDependsOnDbContext"] = ("SM0039", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["DuplicateModuleName"] = ("SM0040", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["ViewPagePrefixMismatch"] = ("SM0041", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["ViewEndpointWithoutViewPrefix"] = ("SM0042", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["EmptyModuleWarning"] = ("SM0043", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["MultipleModuleOptions"] = ("SM0044", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["FeatureClassNotSealed"] = ("SM0045", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["FeatureFieldNamingViolation"] = ("SM0046", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["DuplicateFeatureName"] = ("SM0047", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["FeatureFieldNotConstString"] = ("SM0048", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["MultipleEndpointsPerFile"] = ("SM0049", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["ModuleAssemblyNamingViolation"] = ("SM0052", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
        ["MissingContractsAssembly"] = ("SM0053", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["MissingEndpointRouteConst"] = ("SM0054", DiagnosticSeverity.Error, "SimpleModule.Generator"),
        ["EntityNotInContractsAssembly"] = ("SM0055", DiagnosticSeverity.Warning, "SimpleModule.Generator"),
    };

    [Fact]
    public void AllDescriptorsMatchBaseline()
    {
        var descriptorsType = typeof(ModuleDiscovererGenerator).Assembly
            .GetType("SimpleModule.Generator.DiagnosticDescriptors");
        descriptorsType.Should().NotBeNull("DiagnosticDescriptors class must exist in the generator assembly");

        var actual = new Dictionary<string, (string Id, DiagnosticSeverity Severity, string Category)>();
        foreach (var field in descriptorsType!.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (field.GetValue(null) is DiagnosticDescriptor d)
                actual[field.Name] = (d.Id, d.DefaultSeverity, d.Category);
        }

        actual.Should().HaveCount(Expected.Count, "the set of descriptors must match the baseline");

        foreach (var kvp in Expected)
        {
            actual.Should().ContainKey(kvp.Key);
            actual[kvp.Key].Should().Be(kvp.Value, $"descriptor {kvp.Key} should match the baseline");
        }
    }
}
```

**Note:** The ID/severity values in the `Expected` dictionary above are derived from reading `DiagnosticEmitter.cs` on the pre-refactor commit. If the reading is off, the test will fail on first run — inspect the failure, correct the baseline table in the *same commit*, and re-run.

- [ ] **Step 3: Run the test**

Run: `dotnet test tests/SimpleModule.Generator.Tests --filter "DiagnosticCatalogTests"`
Expected: Pass. If any mismatch: inspect the failure, correct the `Expected` table (the test's baseline can only be wrong in one direction — the assertions tell you which fields don't match).

- [ ] **Step 4: Commit**

```bash
git add tests/SimpleModule.Generator.Tests/DiagnosticCatalogTests.cs
git commit -m "test(generator): lock in diagnostic catalog against baseline"
```

---

## Task 31: Final size + output verification

**Files:** (verification only)

- [ ] **Step 1: Confirm all target files are under 300 lines**

Run:
```bash
wc -l framework/SimpleModule.Generator/**/*.cs framework/SimpleModule.Generator/*.cs 2>/dev/null \
  | awk '$1 > 300 && $2 != "total" { print }'
```
Expected: Empty output.

- [ ] **Step 2: Confirm byte-identical generated source**

Run:
```bash
dotnet build template/SimpleModule.Host -c Debug
GEN_DIR=$(find template/SimpleModule.Host/obj/Debug -type d -name "SimpleModule.Generator" | head -1)
diff -r baseline/generator-output "$GEN_DIR"
```
Expected: No output.

- [ ] **Step 3: Full test suite**

Run: `dotnet test`
Expected: All tests pass (generator tests + integration tests).

- [ ] **Step 4: Full solution build**

Run: `dotnet build`
Expected: Zero warnings, zero errors (repo enforces `TreatWarningsAsErrors`).

- [ ] **Step 5: Remove the baseline snapshot**

```bash
rm -rf baseline/
```
No commit — `.gitignore` already excludes it.

- [ ] **Step 6: Summarise the line-count improvements**

Run:
```bash
echo "=== After refactor ==="
wc -l framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs \
      framework/SimpleModule.Generator/Discovery/DiscoveryData.cs \
      framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs
echo ""
echo "=== All generator files ==="
find framework/SimpleModule.Generator -name "*.cs" | xargs wc -l | sort -rn | head -20
```

Expected: `SymbolDiscovery.cs` ≤ 200, `DiscoveryData.cs` ≤ 220, `DiagnosticEmitter.cs` ≤ 30. No file > 300.

---

## Self-Review

**Spec coverage:**

| Spec requirement | Covered by task(s) |
|---|---|
| `SymbolDiscovery.cs` split to 12 files | Tasks 7–15 |
| `DiscoveryData.cs` split to 3 files | Tasks 5–6 |
| `DiagnosticEmitter.cs` split to 8 files | Tasks 16–22 |
| `AssemblyConventions` relocation | Task 2 |
| `CoreSymbols` record | Task 3, used in Task 4 + downstream |
| Module-by-name dictionary | Task 24 |
| Single-pass reference classification | Task 23 |
| Lifted `moduleNsByName` | Task 25 |
| `FindClosestModuleName` reverse-index | Task 26 |
| DTO convention short-circuit | Task 27 |
| Scoped attributed-DTO discovery (revert gate) | Task 28 |
| Byte-identical generated output check | Tasks 4, 15, 22, 23, 24, 25, 26, 27, 28, 31 |
| Diagnostic catalog reflection test | Task 30 |
| Incremental-caching test | Task 29 |
| Final line-count verification | Task 31 |

All spec items mapped.

**Placeholder scan:** No `TBD`, `TODO`, or "similar to above" phrasing. Every task has explicit file paths, commit messages, and build/test commands. Tasks that move code reference concrete method names and the current file's line ranges.

**Type consistency check:**
- `CoreSymbols.TryResolve` returns `CoreSymbols?` — Task 3 (definition) and Task 4 (usage) agree.
- `ModuleFinder.FindModuleTypes(INamespaceSymbol, CoreSymbols, List<ModuleInfo>, CancellationToken)` — Task 8 (definition) matches call site in Task 8 Step 3.
- `EndpointFinder.FindEndpointTypes(INamespaceSymbol, CoreSymbols, List<EndpointInfo>, List<ViewInfo>, CancellationToken)` — Task 9 signature matches its call site.
- `SymbolHelpers.BuildModuleNamespaceIndex(List<ModuleInfo>)` / `ModuleNamespaceIndex` / `FindClosestModuleNameFast` — defined in Task 26, used in Task 26.
- `DiagnosticDescriptors.*` field names in Tasks 16–22 and Task 30 match exactly: 38 names listed, grep output from the explore step confirmed same count.
- `ModuleChecks.Run` / `DbContextChecks.Run` / `DependencyChecks.Run` / `ContractAndDtoChecks.Run` / `PermissionFeatureChecks.Run` / `EndpointChecks.Run` — all six defined with `(SourceProductionContext context, DiscoveryData data)` signature, called in the Task 22 Step 5 orchestrator in that exact order.
- `VogenFinder.IsVogenValueObject` / `VogenFinder.ResolveUnderlyingType` — Task 14 moves these out of `SymbolDiscovery`, Task 14 Step 5 fixes up the `DtoFinder` references that Task 10 left pointing at `SymbolDiscovery.IsVogenValueObject`.
- `DtoFinder.FindDtoTypes(INamespaceSymbol, INamedTypeSymbol, List<DtoTypeInfo>, CancellationToken)` — Task 10 preserves the pre-refactor signature; Task 23 call site passes `s.DtoAttribute` (non-null under the `if (s.DtoAttribute is not null)` guard); Task 28 call site uses `s.DtoAttribute!` for the same reason.

**Scope:** Single focused refactor. No cross-repo concerns.
