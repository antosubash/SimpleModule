# Split Generator into Focused Emitters — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Refactor the monolithic `ModuleDiscovererGenerator` into a shared discovery layer + focused emitter classes, keeping generated output byte-identical.

**Architecture:** Single `[Generator]` entry point calls `SymbolDiscovery.Extract(compilation)` for shared discovery, then iterates an `IEmitter[]` array to dispatch to focused emitter classes. All model types move to `Discovery/`, all emitters to `Emitters/`, shared helpers to `Helpers/`.

**Tech Stack:** C# / Roslyn `IIncrementalGenerator` / netstandard2.0

---

### Task 1: Run existing tests (green baseline)

**Files:** None (verification only)

**Step 1: Run all generator tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 2: Commit** — no commit needed, baseline only

---

### Task 2: Extract model types to `Discovery/DiscoveryData.cs`

Move all record types and mutable working types from `ModuleDiscovererGenerator.Models.cs` into a new standalone file. Change visibility from `private` (nested in partial class) to `internal` (top-level in namespace).

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs`
- Delete: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Models.cs`

**Step 1: Create `Discovery/DiscoveryData.cs`**

Copy all types from `ModuleDiscovererGenerator.Models.cs` into new file. Key changes:
- Remove the `partial class ModuleDiscovererGenerator` wrapper
- Change all `private` access modifiers to `internal`
- Add `namespace SimpleModule.Generator;` (file-scoped)
- Keep all `readonly record struct` types and mutable `sealed class` types

```csharp
// Discovery/DiscoveryData.cs
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SimpleModule.Generator;

#region Equatable data model for incremental caching

// These record types implement value equality so the incremental generator
// pipeline can detect when the extracted data hasn't changed and skip
// re-generating source files.

internal readonly record struct DiscoveryData(
    ImmutableArray<ModuleInfoRecord> Modules,
    ImmutableArray<DtoTypeInfoRecord> DtoTypes,
    ImmutableArray<DbContextInfoRecord> DbContexts,
    ImmutableArray<EntityConfigInfoRecord> EntityConfigs
)
{
    public static readonly DiscoveryData Empty = new(
        ImmutableArray<ModuleInfoRecord>.Empty,
        ImmutableArray<DtoTypeInfoRecord>.Empty,
        ImmutableArray<DbContextInfoRecord>.Empty,
        ImmutableArray<EntityConfigInfoRecord>.Empty
    );

    public bool Equals(DiscoveryData other)
    {
        return Modules.SequenceEqual(other.Modules)
            && DtoTypes.SequenceEqual(other.DtoTypes)
            && DbContexts.SequenceEqual(other.DbContexts)
            && EntityConfigs.SequenceEqual(other.EntityConfigs);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var m in Modules)
                hash = hash * 31 + m.GetHashCode();
            foreach (var d in DtoTypes)
                hash = hash * 31 + d.GetHashCode();
            foreach (var c in DbContexts)
                hash = hash * 31 + c.GetHashCode();
            foreach (var e in EntityConfigs)
                hash = hash * 31 + e.GetHashCode();
            return hash;
        }
    }
}

// ... all other record structs and mutable classes with `internal` visibility
#endregion
```

**Step 2: Delete `ModuleDiscovererGenerator.Models.cs`**

**Step 3: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 4: Commit**

```bash
git add -A framework/SimpleModule.Generator/Discovery/ framework/SimpleModule.Generator/ModuleDiscovererGenerator.Models.cs
git commit -m "refactor: extract generator model types to Discovery/DiscoveryData.cs"
```

---

### Task 3: Extract shared helpers to `Helpers/TypeMappingHelpers.cs`

Move `GetModuleFieldName`, `MapCSharpTypeToTypeScript`, and `GetModuleNameFromFqn` out of the emitters partial class into a standalone static helper class.

**Files:**
- Create: `framework/SimpleModule.Generator/Helpers/TypeMappingHelpers.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` — remove the three methods

**Step 1: Create `Helpers/TypeMappingHelpers.cs`**

```csharp
using System;
using System.Collections.Generic;

namespace SimpleModule.Generator;

internal static class TypeMappingHelpers
{
    internal static string GetModuleFieldName(string fullyQualifiedName)
    {
        var name = fullyQualifiedName.Replace("global::", "").Replace(".", "_");
        return $"s_{name}";
    }

    internal static string GetModuleNameFromFqn(string fqn)
    {
        var name = fqn.Replace("global::", "");
        var parts = name.Split('.');
        return parts.Length >= 3 ? parts[1] : parts[0];
    }

    internal static string MapCSharpTypeToTypeScript(
        string typeFqn,
        Dictionary<string, string>? knownDtoTypes = null
    )
    {
        // ... exact copy of existing method body
    }
}
```

**Step 2: Update all call sites** in `ModuleDiscovererGenerator.Emitters.cs` to use `TypeMappingHelpers.GetModuleFieldName(...)`, `TypeMappingHelpers.MapCSharpTypeToTypeScript(...)`, `TypeMappingHelpers.GetModuleNameFromFqn(...)`. Remove the three methods from the partial class.

**Step 3: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 4: Commit**

```bash
git add framework/SimpleModule.Generator/Helpers/ framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs
git commit -m "refactor: extract shared helpers to TypeMappingHelpers"
```

---

### Task 4: Extract symbol discovery to `Discovery/SymbolDiscovery.cs`

Move `ExtractDiscoveryData` and all `Find*` methods + helper methods (`ImplementsInterface`, `DeclaresMethod`, `InheritsFrom`, `HasComponentBaseDescendant`, `FindClosestModuleName`) from `ModuleDiscovererGenerator.cs` into a standalone static class.

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.cs` — remove all methods except `Initialize`, update call to `SymbolDiscovery.Extract`

**Step 1: Create `Discovery/SymbolDiscovery.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class SymbolDiscovery
{
    internal static DiscoveryData Extract(Compilation compilation)
    {
        // ... exact copy of ExtractDiscoveryData body
    }

    // ... all Find* methods, ImplementsInterface, DeclaresMethod,
    //     InheritsFrom, HasComponentBaseDescendant, FindClosestModuleName
    //     all as `internal static` or `private static`
}
```

**Step 2: Update `ModuleDiscovererGenerator.Initialize`**

Change: `static (compilation, _) => ExtractDiscoveryData(compilation)`
To: `static (compilation, _) => SymbolDiscovery.Extract(compilation)`

**Step 3: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 4: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs framework/SimpleModule.Generator/ModuleDiscovererGenerator.cs
git commit -m "refactor: extract symbol discovery to SymbolDiscovery static class"
```

---

### Task 5: Create `IEmitter` interface

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/IEmitter.cs`

**Step 1: Create the interface**

```csharp
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal interface IEmitter
{
    void Emit(SourceProductionContext context, DiscoveryData data);
}
```

**Step 2: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/IEmitter.cs
git commit -m "refactor: add IEmitter interface for generator dispatch"
```

---

### Task 6: Extract `DiagnosticEmitter`

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` — remove `ReportDiscoveryDiagnostics` and all 7 `DiagnosticDescriptor` fields

**Step 1: Create `Emitters/DiagnosticEmitter.cs`**

Move `ReportDiscoveryDiagnostics` as the `Emit` method. Move all 7 `DiagnosticDescriptor` static fields. Note: `DuplicateDbSetPropertyName` (SM0001) is used by `HostDbContextEmitter` — make it `internal static` so `HostDbContextEmitter` can reference it.

```csharp
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal sealed class DiagnosticEmitter : IEmitter
{
    internal static readonly DiagnosticDescriptor DuplicateDbSetPropertyName = new(...);
    private static readonly DiagnosticDescriptor EmptyModuleName = new(...);
    // ... other descriptors

    public void Emit(SourceProductionContext context, DiscoveryData data)
    {
        // ... exact copy of ReportDiscoveryDiagnostics body
    }
}
```

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs
git commit -m "refactor: extract DiagnosticEmitter"
```

---

### Task 7: Extract `ModuleExtensionsEmitter`

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` — remove `GenerateModuleExtensions`

**Step 1: Create the emitter**

```csharp
internal sealed class ModuleExtensionsEmitter : IEmitter
{
    public void Emit(SourceProductionContext context, DiscoveryData data)
    {
        // Call existing logic with data.Modules, data.DtoTypes.Length > 0
        // Use TypeMappingHelpers.GetModuleFieldName(...)
    }
}
```

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs
git commit -m "refactor: extract ModuleExtensionsEmitter"
```

---

### Task 8: Extract `EndpointExtensionsEmitter`

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/EndpointExtensionsEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` — remove `GenerateEndpointExtensions`

**Step 1: Create the emitter** — move `GenerateEndpointExtensions` logic into `Emit`. Uses `TypeMappingHelpers.GetModuleFieldName`.

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/EndpointExtensionsEmitter.cs framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs
git commit -m "refactor: extract EndpointExtensionsEmitter"
```

---

### Task 9: Extract `MenuExtensionsEmitter`

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/MenuExtensionsEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` — remove `GenerateMenuExtensions`

**Step 1: Create the emitter**

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/MenuExtensionsEmitter.cs framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs
git commit -m "refactor: extract MenuExtensionsEmitter"
```

---

### Task 10: Extract `RazorComponentExtensionsEmitter`

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/RazorComponentExtensionsEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` — remove `GenerateRazorComponentExtensions`

**Step 1: Create the emitter**

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/RazorComponentExtensionsEmitter.cs framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs
git commit -m "refactor: extract RazorComponentExtensionsEmitter"
```

---

### Task 11: Extract `ViewPagesEmitter`

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/ViewPagesEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` — remove `GenerateViewPages`

**Step 1: Create the emitter**

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/ViewPagesEmitter.cs framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs
git commit -m "refactor: extract ViewPagesEmitter"
```

---

### Task 12: Extract `JsonResolverEmitter`

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/JsonResolverEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` — remove `GenerateJsonResolver`

**Step 1: Create the emitter** — conditionally emits only when `data.DtoTypes.Length > 0`

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/JsonResolverEmitter.cs framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs
git commit -m "refactor: extract JsonResolverEmitter"
```

---

### Task 13: Extract `TypeScriptDefinitionsEmitter`

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/TypeScriptDefinitionsEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` — remove `GenerateTypeScriptDefinitions`

**Step 1: Create the emitter** — conditionally emits only when `data.DtoTypes.Length > 0`. Uses `TypeMappingHelpers.MapCSharpTypeToTypeScript` and `TypeMappingHelpers.GetModuleNameFromFqn`.

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/TypeScriptDefinitionsEmitter.cs framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs
git commit -m "refactor: extract TypeScriptDefinitionsEmitter"
```

---

### Task 14: Extract `HostDbContextEmitter`

**Files:**
- Create: `framework/SimpleModule.Generator/Emitters/HostDbContextEmitter.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs` — remove `EmitHostDbContext` (this should now be the last method in the file)

**Step 1: Create the emitter** — conditionally emits only when `data.DbContexts.Length > 0`. References `DiagnosticEmitter.DuplicateDbSetPropertyName` for SM0001. Keep `#pragma warning disable CA1308`.

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/HostDbContextEmitter.cs framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs
git commit -m "refactor: extract HostDbContextEmitter"
```

---

### Task 15: Delete the old emitters partial file and wire up `ModuleDiscoveryGenerator.cs`

At this point `ModuleDiscovererGenerator.Emitters.cs` should be empty (no methods left). Delete it and update the main generator to use the new emitter array.

**Files:**
- Delete: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.Emitters.cs`
- Modify: `framework/SimpleModule.Generator/ModuleDiscovererGenerator.cs` — replace inline dispatch with emitter array

**Step 1: Update `ModuleDiscovererGenerator.cs`**

The class should no longer be `partial`. Replace the `RegisterSourceOutput` callback:

```csharp
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

[Generator]
public class ModuleDiscovererGenerator : IIncrementalGenerator
{
    private static readonly IEmitter[] Emitters =
    [
        new DiagnosticEmitter(),
        new ModuleExtensionsEmitter(),
        new EndpointExtensionsEmitter(),
        new MenuExtensionsEmitter(),
        new RazorComponentExtensionsEmitter(),
        new ViewPagesEmitter(),
        new JsonResolverEmitter(),
        new TypeScriptDefinitionsEmitter(),
        new HostDbContextEmitter(),
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

**Step 2: Delete `ModuleDiscovererGenerator.Emitters.cs`**

**Step 3: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests --no-restore -v quiet`
Expected: All tests pass

**Step 4: Commit**

```bash
git add framework/SimpleModule.Generator/
git commit -m "refactor: wire up emitter dispatch and delete old partial files"
```

---

### Task 16: Full build and test verification

**Step 1: Clean build**

Run: `dotnet build`
Expected: Build succeeds with no errors

**Step 2: Run all tests (not just generator tests)**

Run: `dotnet test`
Expected: All tests pass

**Step 3: Verify final file structure**

```
framework/SimpleModule.Generator/
├── ModuleDiscovererGenerator.cs
├── Discovery/
│   ├── DiscoveryData.cs
│   └── SymbolDiscovery.cs
├── Emitters/
│   ├── IEmitter.cs
│   ├── DiagnosticEmitter.cs
│   ├── ModuleExtensionsEmitter.cs
│   ├── EndpointExtensionsEmitter.cs
│   ├── MenuExtensionsEmitter.cs
│   ├── RazorComponentExtensionsEmitter.cs
│   ├── ViewPagesEmitter.cs
│   ├── JsonResolverEmitter.cs
│   ├── TypeScriptDefinitionsEmitter.cs
│   └── HostDbContextEmitter.cs
├── Helpers/
│   └── TypeMappingHelpers.cs
└── IsExternalInit.cs
```

**Step 4: Commit** — no additional commit needed if all prior commits are clean
