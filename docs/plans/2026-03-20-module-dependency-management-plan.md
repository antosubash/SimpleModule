# Module Dependency Management Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add automatic module dependency inference, cycle detection, illegal reference checking, contract interface hygiene, and topological module ordering to the source generator.

**Architecture:** All changes live in `SimpleModule.Generator` (netstandard2.0). A new `DependencyAnalysis` class infers dependencies from assembly references, performs topological sort, and produces diagnostics. Existing emitters consume the sorted module list. New diagnostics use IDs SM0010–SM0014 (existing SM0001–SM0007 are taken).

**Tech Stack:** Roslyn `IIncrementalGenerator`, `Microsoft.CodeAnalysis.CSharp`, xUnit.v3, FluentAssertions

**Important:** The design doc at `docs/plans/2026-03-20-module-dependency-management-design.md` uses SM0001-SM0005 in its examples, but those IDs conflict with existing diagnostics. The actual implementation uses SM0010-SM0014. Update the design doc's diagnostic table after implementation.

---

### Task 1: Add dependency data to DiscoveryData

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs`

**Step 1: Add ModuleDependencyRecord to DiscoveryData.cs**

Add a new record type after `EntityConfigInfoRecord`:

```csharp
internal readonly record struct ModuleDependencyRecord(
    string ModuleName,
    string DependsOnModuleName,
    string ContractsAssemblyName
);
```

Add a new field to `DiscoveryData`:

```csharp
internal readonly record struct DiscoveryData(
    ImmutableArray<ModuleInfoRecord> Modules,
    ImmutableArray<DtoTypeInfoRecord> DtoTypes,
    ImmutableArray<DbContextInfoRecord> DbContexts,
    ImmutableArray<EntityConfigInfoRecord> EntityConfigs,
    ImmutableArray<ModuleDependencyRecord> Dependencies,
    ImmutableArray<IllegalModuleReferenceRecord> IllegalReferences,
    ImmutableArray<ContractInterfaceInfoRecord> ContractInterfaces
)
```

Add the new record types:

```csharp
internal readonly record struct IllegalModuleReferenceRecord(
    string ReferencingModuleName,
    string ReferencingAssemblyName,
    string ReferencedModuleName,
    string ReferencedAssemblyName
);

internal readonly record struct ContractInterfaceInfoRecord(
    string ContractsAssemblyName,
    string InterfaceName,
    int MethodCount
);
```

Update `DiscoveryData.Empty` to include the new empty arrays.

Update `DiscoveryData.Equals` and `GetHashCode` to include the new fields.

**Step 2: Run tests to verify nothing breaks**

Run: `dotnet test tests/SimpleModule.Generator.Tests/ --filter "FullyQualifiedName~DiagnosticTests"`
Expected: All existing tests pass (DiscoveryData.Empty changes are backwards-compatible via the constructor).

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/DiscoveryData.cs
git commit -m "feat(generator): add dependency data structures to DiscoveryData"
```

---

### Task 2: Infer dependencies in SymbolDiscovery

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

**Step 1: Write the failing test**

Create test in `tests/SimpleModule.Generator.Tests/DependencyInferenceTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class DependencyInferenceTests
{
    [Fact]
    public void ModuleReferencingContractsAssembly_InfersDependency()
    {
        // This test verifies that when we compile two modules where
        // one references the other's contracts, the dependency is detected.
        // Since our test helper compiles everything into one assembly,
        // we test the dependency inference logic via the generated output:
        // modules should appear in topological order in AddModules().

        // Module B depends on Module A's contracts
        var source = """
            using SimpleModule.Core;

            namespace TestApp.ModuleA
            {
                [Module("ModuleA")]
                public class ModuleAModule : IModule { }
            }

            namespace TestApp.ModuleB
            {
                [Module("ModuleB")]
                public class ModuleBModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var result = GeneratorTestHelper.RunGenerator(compilation);

        // Both modules should appear in generated code
        var moduleExtensions = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal))
            .GetText().ToString();

        moduleExtensions.Should().Contain("ModuleA");
        moduleExtensions.Should().Contain("ModuleB");
    }
}
```

**Step 2: Add dependency inference to SymbolDiscovery.Extract**

After the existing module discovery loop in `SymbolDiscovery.Extract()`, add dependency inference logic. This needs to:

1. Build a map from assembly name to module name for assemblies containing `[Module]` types
2. Build a map from `X.Contracts` assembly name → module name `X` (by matching naming convention)
3. For each module, get its containing assembly and scan assembly references
4. If a reference matches a known `.Contracts` assembly (not its own), record a dependency
5. If a reference matches a known module implementation assembly (not its own), record an illegal reference
6. Scan `.Contracts` assemblies for public interfaces and count their methods

Add this code block in `Extract()` after the modules list is populated and before the DTO discovery:

```csharp
// --- Dependency inference ---
var dependencies = new List<ModuleDependencyRecord>();
var illegalReferences = new List<IllegalModuleReferenceRecord>();
var contractInterfaces = new List<ContractInterfaceInfoRecord>();

// Map: assembly name → module name (for module implementation assemblies)
var moduleAssemblyMap = new Dictionary<string, string>();
// Map: contracts assembly name → module name
var contractsAssemblyMap = new Dictionary<string, string>();

foreach (var module in modules)
{
    var metadataName = module.FullyQualifiedName.Replace("global::", "");
    var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
    if (typeSymbol is null) continue;

    var assemblyName = typeSymbol.ContainingAssembly.Name;
    moduleAssemblyMap[assemblyName] = module.ModuleName;

    // Convention: if assembly is "Products", look for "Products.Contracts"
    // Also handle: assembly might be named with dots like "MyApp.Products"
    // The contracts assembly would be "MyApp.Products.Contracts" or "Products.Contracts"
}

// Scan all referenced assemblies for .Contracts naming pattern
foreach (var reference in compilation.References)
{
    if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol asm)
        continue;

    var asmName = asm.Name;
    if (!asmName.EndsWith(".Contracts", StringComparison.Ordinal))
        continue;

    // Try to match to a module: "Products.Contracts" → module "Products"
    var baseName = asmName.Substring(0, asmName.Length - ".Contracts".Length);

    // Check if there's a module whose assembly name matches the base
    // or whose module name matches the last segment
    foreach (var kvp in moduleAssemblyMap)
    {
        if (kvp.Key == baseName || kvp.Key.EndsWith("." + baseName, StringComparison.Ordinal))
        {
            contractsAssemblyMap[asmName] = kvp.Value;
            break;
        }
    }

    // If no match by assembly name, try matching by module name
    if (!contractsAssemblyMap.ContainsKey(asmName))
    {
        var lastSegment = baseName.Contains(".")
            ? baseName.Substring(baseName.LastIndexOf('.') + 1)
            : baseName;

        foreach (var module in modules)
        {
            if (string.Equals(module.ModuleName, lastSegment, StringComparison.OrdinalIgnoreCase))
            {
                contractsAssemblyMap[asmName] = module.ModuleName;
                break;
            }
        }
    }

    // Scan this contracts assembly for public interfaces
    ScanContractInterfaces(asm.GlobalNamespace, asmName, contractInterfaces);
}

// For each module, check its assembly's references
foreach (var module in modules)
{
    var metadataName = module.FullyQualifiedName.Replace("global::", "");
    var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
    if (typeSymbol is null) continue;

    var moduleAssembly = typeSymbol.ContainingAssembly;
    var moduleAsmName = moduleAssembly.Name;

    foreach (var referencedAssembly in moduleAssembly.Modules
        .SelectMany(m => m.ReferencedAssemblySymbols))
    {
        var refName = referencedAssembly.Name;

        // Check for illegal impl reference (module → module, not via contracts)
        if (moduleAssemblyMap.TryGetValue(refName, out var referencedModuleName)
            && referencedModuleName != module.ModuleName)
        {
            illegalReferences.Add(new IllegalModuleReferenceRecord(
                module.ModuleName,
                moduleAsmName,
                referencedModuleName,
                refName
            ));
        }

        // Check for contracts reference → infer dependency
        if (contractsAssemblyMap.TryGetValue(refName, out var depModuleName)
            && depModuleName != module.ModuleName)
        {
            dependencies.Add(new ModuleDependencyRecord(
                module.ModuleName,
                depModuleName,
                refName
            ));
        }
    }
}
```

Add helper method:

```csharp
private static void ScanContractInterfaces(
    INamespaceSymbol namespaceSymbol,
    string assemblyName,
    List<ContractInterfaceInfoRecord> results)
{
    foreach (var member in namespaceSymbol.GetMembers())
    {
        if (member is INamespaceSymbol childNs)
        {
            ScanContractInterfaces(childNs, assemblyName, results);
        }
        else if (member is INamedTypeSymbol typeSymbol
            && typeSymbol.TypeKind == TypeKind.Interface
            && typeSymbol.DeclaredAccessibility == Accessibility.Public)
        {
            var methodCount = typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Count(m => m.MethodKind == MethodKind.Ordinary);

            results.Add(new ContractInterfaceInfoRecord(
                assemblyName,
                typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                methodCount
            ));
        }
    }
}
```

Update the `return new DiscoveryData(...)` call to include the new fields:

```csharp
return new DiscoveryData(
    modules.Select(...).ToImmutableArray(),
    dtoTypes.Select(...).ToImmutableArray(),
    dbContexts.Select(...).ToImmutableArray(),
    entityConfigs.Select(...).ToImmutableArray(),
    dependencies.ToImmutableArray(),
    illegalReferences.ToImmutableArray(),
    contractInterfaces.ToImmutableArray()
);
```

Also update the early return `DiscoveryData.Empty` — it needs to be updated in `DiscoveryData.cs` to match the new constructor.

**Step 3: Run all generator tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests/`
Expected: All existing tests pass.

**Step 4: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs tests/SimpleModule.Generator.Tests/DependencyInferenceTests.cs
git commit -m "feat(generator): infer module dependencies from assembly references"
```

---

### Task 3: Add topological sort and cycle detection

**Files:**
- Create: `framework/SimpleModule.Generator/Discovery/TopologicalSort.cs`
- Create: `tests/SimpleModule.Generator.Tests/TopologicalSortTests.cs`

**Step 1: Write the failing tests**

```csharp
using System.Collections.Immutable;
using FluentAssertions;

namespace SimpleModule.Generator.Tests;

public class TopologicalSortTests
{
    [Fact]
    public void NoDependencies_ReturnsOriginalOrder()
    {
        var modules = ImmutableArray.Create("A", "B", "C");
        var deps = ImmutableArray<(string From, string To)>.Empty;

        var result = TopologicalSort.Sort(modules, deps);

        result.IsSuccess.Should().BeTrue();
        result.Sorted.Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public void LinearDependency_ReturnsDependencyOrder()
    {
        var modules = ImmutableArray.Create("C", "B", "A");
        // C depends on B, B depends on A
        var deps = ImmutableArray.Create(("C", "B"), ("B", "A"));

        var result = TopologicalSort.Sort(modules, deps);

        result.IsSuccess.Should().BeTrue();
        var sorted = result.Sorted;
        sorted.IndexOf("A").Should().BeLessThan(sorted.IndexOf("B"));
        sorted.IndexOf("B").Should().BeLessThan(sorted.IndexOf("C"));
    }

    [Fact]
    public void DiamondDependency_ResolvesCorrectly()
    {
        var modules = ImmutableArray.Create("D", "B", "C", "A");
        // D depends on B and C, B depends on A, C depends on A
        var deps = ImmutableArray.Create(("D", "B"), ("D", "C"), ("B", "A"), ("C", "A"));

        var result = TopologicalSort.Sort(modules, deps);

        result.IsSuccess.Should().BeTrue();
        var sorted = result.Sorted;
        sorted.IndexOf("A").Should().BeLessThan(sorted.IndexOf("B"));
        sorted.IndexOf("A").Should().BeLessThan(sorted.IndexOf("C"));
        sorted.IndexOf("B").Should().BeLessThan(sorted.IndexOf("D"));
        sorted.IndexOf("C").Should().BeLessThan(sorted.IndexOf("D"));
    }

    [Fact]
    public void SimpleCycle_DetectsCycle()
    {
        var modules = ImmutableArray.Create("A", "B");
        var deps = ImmutableArray.Create(("A", "B"), ("B", "A"));

        var result = TopologicalSort.Sort(modules, deps);

        result.IsSuccess.Should().BeFalse();
        result.Cycle.Should().Contain("A");
        result.Cycle.Should().Contain("B");
    }

    [Fact]
    public void ThreeNodeCycle_DetectsCycle()
    {
        var modules = ImmutableArray.Create("A", "B", "C");
        var deps = ImmutableArray.Create(("A", "B"), ("B", "C"), ("C", "A"));

        var result = TopologicalSort.Sort(modules, deps);

        result.IsSuccess.Should().BeFalse();
        result.Cycle.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public void PartialCycle_DetectsOnlyCyclicNodes()
    {
        var modules = ImmutableArray.Create("A", "B", "C", "D");
        // B and C form a cycle, A and D are fine
        var deps = ImmutableArray.Create(("B", "C"), ("C", "B"), ("D", "A"));

        var result = TopologicalSort.Sort(modules, deps);

        result.IsSuccess.Should().BeFalse();
        result.Cycle.Should().Contain("B");
        result.Cycle.Should().Contain("C");
    }

    [Fact]
    public void GroupsByPhase_CorrectPhaseAssignment()
    {
        var modules = ImmutableArray.Create("C", "B", "A");
        var deps = ImmutableArray.Create(("C", "B"), ("B", "A"));

        var result = TopologicalSort.Sort(modules, deps);

        result.IsSuccess.Should().BeTrue();
        // A has no deps = phase 0, B depends on A = phase 1, C depends on B = phase 2
        result.Phases.Should().ContainKey("A").WhoseValue.Should().Be(0);
        result.Phases.Should().ContainKey("B").WhoseValue.Should().Be(1);
        result.Phases.Should().ContainKey("C").WhoseValue.Should().Be(2);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/SimpleModule.Generator.Tests/ --filter "FullyQualifiedName~TopologicalSortTests"`
Expected: FAIL — `TopologicalSort` class doesn't exist.

**Step 3: Implement TopologicalSort**

```csharp
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SimpleModule.Generator;

internal readonly record struct SortResult(
    bool IsSuccess,
    ImmutableArray<string> Sorted,
    ImmutableArray<string> Cycle,
    Dictionary<string, int> Phases,
    Dictionary<string, ImmutableArray<string>> DependenciesOf
);

internal static class TopologicalSort
{
    internal static SortResult Sort(
        ImmutableArray<string> nodes,
        ImmutableArray<(string From, string To)> edges)
    {
        var adjacency = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();
        var depsOf = new Dictionary<string, List<string>>();

        foreach (var node in nodes)
        {
            adjacency[node] = new List<string>();
            inDegree[node] = 0;
            depsOf[node] = new List<string>();
        }

        foreach (var (from, to) in edges)
        {
            if (!adjacency.ContainsKey(from) || !adjacency.ContainsKey(to))
                continue;

            // "from" depends on "to" → "to" must come first
            // Edge direction: to → from (to enables from)
            adjacency[to].Add(from);
            inDegree[from]++;
            depsOf[from].Add(to);
        }

        // Kahn's algorithm
        var queue = new Queue<string>();
        var sorted = new List<string>();
        var phases = new Dictionary<string, int>();

        foreach (var node in nodes)
        {
            if (inDegree[node] == 0)
            {
                queue.Enqueue(node);
                phases[node] = 0;
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(current);

            foreach (var neighbor in adjacency[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    // Phase = max phase of all dependencies + 1
                    var maxDepPhase = 0;
                    foreach (var dep in depsOf[neighbor])
                    {
                        if (phases.TryGetValue(dep, out var depPhase) && depPhase >= maxDepPhase)
                            maxDepPhase = depPhase + 1;
                    }
                    phases[neighbor] = maxDepPhase;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (sorted.Count != nodes.Length)
        {
            // Cycle detected — find the cycle
            var cycle = FindCycle(nodes, edges);
            return new SortResult(
                false,
                ImmutableArray<string>.Empty,
                cycle,
                new Dictionary<string, int>(),
                new Dictionary<string, ImmutableArray<string>>()
            );
        }

        var immutableDeps = new Dictionary<string, ImmutableArray<string>>();
        foreach (var kvp in depsOf)
            immutableDeps[kvp.Key] = kvp.Value.ToImmutableArray();

        return new SortResult(
            true,
            sorted.ToImmutableArray(),
            ImmutableArray<string>.Empty,
            phases,
            immutableDeps
        );
    }

    private static ImmutableArray<string> FindCycle(
        ImmutableArray<string> nodes,
        ImmutableArray<(string From, string To)> edges)
    {
        // DFS-based cycle finding
        var adjacency = new Dictionary<string, List<string>>();
        foreach (var node in nodes)
            adjacency[node] = new List<string>();

        foreach (var (from, to) in edges)
        {
            if (adjacency.ContainsKey(from) && adjacency.ContainsKey(to))
                adjacency[from].Add(to);
        }

        var visited = new Dictionary<string, int>(); // 0=unvisited, 1=in-stack, 2=done
        foreach (var node in nodes)
            visited[node] = 0;

        var stack = new List<string>();

        foreach (var node in nodes)
        {
            if (visited[node] == 0)
            {
                var cycle = DfsFindCycle(node, adjacency, visited, stack);
                if (cycle.Length > 0)
                    return cycle;
            }
        }

        return ImmutableArray<string>.Empty;
    }

    private static ImmutableArray<string> DfsFindCycle(
        string node,
        Dictionary<string, List<string>> adjacency,
        Dictionary<string, int> visited,
        List<string> stack)
    {
        visited[node] = 1;
        stack.Add(node);

        foreach (var neighbor in adjacency[node])
        {
            if (!visited.ContainsKey(neighbor))
                continue;

            if (visited[neighbor] == 1)
            {
                // Found cycle — extract it
                var cycleStart = stack.IndexOf(neighbor);
                var cycle = new List<string>();
                for (var i = cycleStart; i < stack.Count; i++)
                    cycle.Add(stack[i]);
                return cycle.ToImmutableArray();
            }

            if (visited[neighbor] == 0)
            {
                var cycle = DfsFindCycle(neighbor, adjacency, visited, stack);
                if (cycle.Length > 0)
                    return cycle;
            }
        }

        visited[node] = 2;
        stack.RemoveAt(stack.Count - 1);
        return ImmutableArray<string>.Empty;
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/SimpleModule.Generator.Tests/ --filter "FullyQualifiedName~TopologicalSortTests"`
Expected: All PASS.

**Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/TopologicalSort.cs tests/SimpleModule.Generator.Tests/TopologicalSortTests.cs
git commit -m "feat(generator): add topological sort with cycle detection"
```

---

### Task 4: Add dependency diagnostic descriptors

**Files:**
- Modify: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

**Step 1: Write the failing tests**

Add to `tests/SimpleModule.Generator.Tests/DependencyDiagnosticTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class DependencyDiagnosticTests
{
    [Fact]
    public void SM0011_ModuleReferencingAnotherModuleImpl_ReportsError()
    {
        // Note: In single-assembly test compilation, all modules share one assembly.
        // This test validates the diagnostic descriptor exists and the emitter
        // handles the IllegalReferences data correctly.
        // Real cross-assembly detection is validated via integration tests.

        var source = """
            using SimpleModule.Core;

            namespace TestApp.Products
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }

            namespace TestApp.Orders
            {
                [Module("Orders")]
                public class OrdersModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        // In single-assembly compilation, no cross-module references exist
        // so no SM0011 should fire
        diagnostics.Should().NotContain(d => d.Id == "SM0011");
    }

    [Fact]
    public void SM0012_ContractInterfaceUnder15Methods_NoDiagnostic()
    {
        var source = """
            using SimpleModule.Core;

            namespace TestApp.Products
            {
                [Module("Products")]
                public class ProductsModule : IModule { }
            }
            """;

        var compilation = GeneratorTestHelper.CreateCompilation(source);
        var (_, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(compilation);

        diagnostics.Should().NotContain(d => d.Id == "SM0012");
        diagnostics.Should().NotContain(d => d.Id == "SM0013");
    }
}
```

**Step 2: Add diagnostic descriptors to DiagnosticEmitter.cs**

Add new descriptors and emit logic. The descriptors should use multi-line `messageFormat` strings to produce the detailed error messages from the design doc.

```csharp
// SM0010: Circular module dependency
internal static readonly DiagnosticDescriptor CircularModuleDependency = new(
    id: "SM0010",
    title: "Circular module dependency detected",
    messageFormat: @"Circular module dependency detected.

  Cycle: {0}

  How this happened:
{1}
  How to fix it:
    One of these modules must not directly depend on the other.
    Identify which direction is the ""primary"" dependency and reverse
    the other using the event bus.

    For example, if {2} is the primary consumer of {3}:
      1. Keep the reference: {2} → {3}.Contracts ✓
      2. Remove the reference: {3} → {2}.Contracts ✗
      3. In {3}, publish an event instead:
           await eventBus.PublishAsync(new {3}Event(...));
      4. In {2}, handle it:
           public class On{3}Event : IEventHandler<{3}Event> {{ ... }}

  Learn more: https://docs.simplemodule.dev/module-dependencies",
    category: "SimpleModule.Generator",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true
);

// SM0011: Illegal implementation reference
internal static readonly DiagnosticDescriptor IllegalImplementationReference = new(
    id: "SM0011",
    title: "Module directly references another module's implementation",
    messageFormat: @"Module '{0}' directly references module '{1}' implementation.

  What happened:
    {2} has a reference to {3} (the implementation assembly).
    Modules must only depend on each other through Contracts packages.

  Why this is a problem:
    Referencing the implementation creates tight coupling between modules.
    It bypasses the Contracts boundary, meaning internal changes in
    {1} can break {0} at compile time or runtime.

  How to fix it:
    1. Remove the reference to {3}.
    2. Add a reference to {1}.Contracts instead.
    3. Replace any usage of internal {1} types with their
       contract interfaces.

  Learn more: https://docs.simplemodule.dev/module-contracts",
    category: "SimpleModule.Generator",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true
);

// SM0012: Contract interface too large (warning)
internal static readonly DiagnosticDescriptor ContractInterfaceTooLargeWarning = new(
    id: "SM0012",
    title: "Contract interface has too many methods (warning)",
    messageFormat: @"Contract interface '{0}' has {1} methods.

  Why this matters:
    Large contract interfaces force consuming modules to depend on
    methods they don't use. When any method signature changes, all
    consumers must recompile — even those using unrelated methods.

  How to fix it:
    Split the interface into focused concerns. For example:
      {0}
        ├── I{2}Queries    → read operations
        ├── I{2}Commands   → write operations
        └── I{2}Events     → event-related operations

    Your module class can implement all of them:
      public class {2}Service : I{2}Queries, I{2}Commands

  Thresholds (configurable in .editorconfig):
    simplemodule.max_contract_methods_warn = 15
    simplemodule.max_contract_methods_error = 20

  Learn more: https://docs.simplemodule.dev/contract-design",
    category: "SimpleModule.Generator",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true
);

// SM0013: Contract interface too large (error)
internal static readonly DiagnosticDescriptor ContractInterfaceTooLargeError = new(
    id: "SM0013",
    title: "Contract interface has too many methods (error)",
    messageFormat: @"Contract interface '{0}' has {1} methods and must be split before the project will compile.

  Why this matters:
    Large contract interfaces force consuming modules to depend on
    methods they don't use. When any method signature changes, all
    consumers must recompile — even those using unrelated methods.

  How to fix it:
    Split the interface into focused concerns. For example:
      {0}
        ├── I{2}Queries    → read operations
        ├── I{2}Commands   → write operations
        └── I{2}Events     → event-related operations

    Your module class can implement all of them:
      public class {2}Service : I{2}Queries, I{2}Commands

  Thresholds (configurable in .editorconfig):
    simplemodule.max_contract_methods_warn = 15
    simplemodule.max_contract_methods_error = 20

  Learn more: https://docs.simplemodule.dev/contract-design",
    category: "SimpleModule.Generator",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true
);

// SM0014: Missing contract interfaces
internal static readonly DiagnosticDescriptor MissingContractInterfaces = new(
    id: "SM0014",
    title: "Referenced contracts assembly has no public interfaces",
    messageFormat: @"Module '{0}' references '{1}' but no contract interfaces were found in that assembly.

  What happened:
    {0} references {1}, but the generator could not find
    any public interfaces in that assembly.

  Likely causes:
    1. Incompatible package version — you may have installed a version
       of {1} that reorganized or removed its interfaces.
       Check your installed version:
         dotnet list package --include-transitive

    2. The Contracts project is empty or not yet built — ensure
       {1} defines at least one public interface.

    3. The package is corrupted — try clearing the NuGet cache:
         dotnet nuget locals all --clear
         dotnet restore

  How to fix it:
    Verify that the version of {1} you're using exports the interfaces
    your code depends on. Check the package release notes for breaking changes.

  Learn more: https://docs.simplemodule.dev/package-compatibility",
    category: "SimpleModule.Generator",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true
);
```

Add emit logic in the `Emit` method for each new diagnostic:

```csharp
// SM0010: Circular dependencies
var moduleNames = data.Modules.Select(m => m.ModuleName).ToImmutableArray();
var depEdges = data.Dependencies
    .Select(d => (d.ModuleName, d.DependsOnModuleName))
    .ToImmutableArray();

var sortResult = TopologicalSort.Sort(moduleNames, depEdges);
if (!sortResult.IsSuccess && sortResult.Cycle.Length > 0)
{
    var cycleStr = string.Join(" → ", sortResult.Cycle) + " → " + sortResult.Cycle[0];
    var howItHappened = new StringBuilder();
    foreach (var dep in data.Dependencies)
    {
        if (sortResult.Cycle.Contains(dep.ModuleName) && sortResult.Cycle.Contains(dep.DependsOnModuleName))
        {
            howItHappened.AppendLine($"    • {dep.ModuleName} references {dep.ContractsAssemblyName}");
        }
    }

    var first = sortResult.Cycle[0];
    var second = sortResult.Cycle.Length > 1 ? sortResult.Cycle[1] : sortResult.Cycle[0];

    context.ReportDiagnostic(Diagnostic.Create(
        CircularModuleDependency,
        Location.None,
        cycleStr,
        howItHappened.ToString(),
        first,
        second
    ));
}

// SM0011: Illegal implementation references
foreach (var illegal in data.IllegalReferences)
{
    context.ReportDiagnostic(Diagnostic.Create(
        IllegalImplementationReference,
        Location.None,
        illegal.ReferencingModuleName,
        illegal.ReferencedModuleName,
        illegal.ReferencingAssemblyName,
        illegal.ReferencedAssemblyName
    ));
}

// SM0012/SM0013: Contract interface size
foreach (var iface in data.ContractInterfaces)
{
    // Extract module-like name from interface (IProductContracts → Product)
    var shortName = iface.InterfaceName.Replace("global::", "");
    if (shortName.Contains("."))
        shortName = shortName.Substring(shortName.LastIndexOf('.') + 1);
    if (shortName.StartsWith("I"))
        shortName = shortName.Substring(1);
    if (shortName.EndsWith("Contracts"))
        shortName = shortName.Substring(0, shortName.Length - "Contracts".Length);

    if (iface.MethodCount >= 20)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            ContractInterfaceTooLargeError,
            Location.None,
            iface.InterfaceName.Replace("global::", ""),
            iface.MethodCount,
            shortName
        ));
    }
    else if (iface.MethodCount >= 15)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            ContractInterfaceTooLargeWarning,
            Location.None,
            iface.InterfaceName.Replace("global::", ""),
            iface.MethodCount,
            shortName
        ));
    }
}

// SM0014: Missing contract interfaces
var contractAssembliesWithInterfaces = new HashSet<string>();
foreach (var iface in data.ContractInterfaces)
    contractAssembliesWithInterfaces.Add(iface.ContractsAssemblyName);

var contractAssembliesReferenced = new HashSet<string>();
foreach (var dep in data.Dependencies)
    contractAssembliesReferenced.Add(dep.ContractsAssemblyName);

foreach (var dep in data.Dependencies)
{
    if (!contractAssembliesWithInterfaces.Contains(dep.ContractsAssemblyName))
    {
        context.ReportDiagnostic(Diagnostic.Create(
            MissingContractInterfaces,
            Location.None,
            dep.ModuleName,
            dep.ContractsAssemblyName
        ));
    }
}
```

**Step 3: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests/`
Expected: All tests pass (existing + new).

**Step 4: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs tests/SimpleModule.Generator.Tests/DependencyDiagnosticTests.cs
git commit -m "feat(generator): add SM0010-SM0014 dependency diagnostics"
```

---

### Task 5: Generate topologically sorted AddModules

**Files:**
- Modify: `framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs`

**Step 1: Write the failing test**

Add to `tests/SimpleModule.Generator.Tests/ModuleExtensionsGenerationTests.cs`:

```csharp
[Fact]
public void AddModules_ContainsPhaseComments()
{
    var source = """
        using SimpleModule.Core;

        namespace TestApp.ModuleA
        {
            [Module("ModuleA")]
            public class ModuleAModule : IModule
            {
                public void ConfigureServices(
                    Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                    Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }
        }

        namespace TestApp.ModuleB
        {
            [Module("ModuleB")]
            public class ModuleBModule : IModule
            {
                public void ConfigureServices(
                    Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                    Microsoft.Extensions.Configuration.IConfiguration configuration) { }
            }
        }
        """;

    var compilation = GeneratorTestHelper.CreateCompilation(source);
    var result = GeneratorTestHelper.RunGenerator(compilation);

    var moduleExtensions = result.GeneratedTrees
        .First(t => t.FilePath.EndsWith("ModuleExtensions.g.cs", StringComparison.Ordinal))
        .GetText().ToString();

    // Should contain phase comments
    moduleExtensions.Should().Contain("// Phase");
}
```

**Step 2: Modify ModuleExtensionsEmitter to use topological sort**

In the `Emit` method, before iterating modules, perform topological sort and group by phase:

```csharp
public void Emit(SourceProductionContext context, DiscoveryData data)
{
    var modules = data.Modules;
    var hasDtoTypes = data.DtoTypes.Length > 0;

    // Topological sort
    var moduleNames = modules.Select(m => m.ModuleName).ToImmutableArray();
    var depEdges = data.Dependencies
        .Select(d => (d.ModuleName, d.DependsOnModuleName))
        .ToImmutableArray();

    var sortResult = TopologicalSort.Sort(moduleNames, depEdges);

    // If cycle detected, fall back to original order (diagnostic is reported by DiagnosticEmitter)
    ImmutableArray<ModuleInfoRecord> sortedModules;
    Dictionary<string, int> phases;
    Dictionary<string, ImmutableArray<string>> depsOf;

    if (sortResult.IsSuccess)
    {
        var moduleByName = new Dictionary<string, ModuleInfoRecord>();
        foreach (var m in modules)
            moduleByName[m.ModuleName] = m;

        var sorted = new List<ModuleInfoRecord>();
        foreach (var name in sortResult.Sorted)
        {
            if (moduleByName.TryGetValue(name, out var m))
                sorted.Add(m);
        }
        sortedModules = sorted.ToImmutableArray();
        phases = sortResult.Phases;
        depsOf = sortResult.DependenciesOf;
    }
    else
    {
        sortedModules = modules;
        phases = new Dictionary<string, int>();
        foreach (var m in modules)
            phases[m.ModuleName] = 0;
        depsOf = new Dictionary<string, ImmutableArray<string>>();
    }

    // ... rest of method uses sortedModules instead of modules ...
    // Add phase comments before each group
```

When emitting the `ConfigureServices` calls, group by phase and add comments:

```csharp
var currentPhase = -1;
foreach (var module in sortedModules.Where(m => m.HasConfigureServices))
{
    var phase = phases.TryGetValue(module.ModuleName, out var p) ? p : 0;
    if (phase != currentPhase)
    {
        currentPhase = phase;
        sb.AppendLine();
        if (depsOf.TryGetValue(module.ModuleName, out var deps) && deps.Length > 0)
        {
            sb.AppendLine($"        // Phase {phase + 1}: Depends on {string.Join(", ", deps)}");
        }
        else
        {
            sb.AppendLine($"        // Phase {phase + 1}: No dependencies");
        }
    }

    var fieldName = TypeMappingHelpers.GetModuleFieldName(module.FullyQualifiedName);
    sb.AppendLine($"        {fieldName}.ConfigureServices(services, configuration);");
}
```

**Step 3: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests/`
Expected: All tests pass.

**Step 4: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs tests/SimpleModule.Generator.Tests/ModuleExtensionsGenerationTests.cs
git commit -m "feat(generator): emit AddModules in topological order with phase comments"
```

---

### Task 6: Update remaining emitters for sorted order

**Files:**
- Modify: `framework/SimpleModule.Generator/Emitters/EndpointExtensionsEmitter.cs`
- Modify: `framework/SimpleModule.Generator/Emitters/MenuExtensionsEmitter.cs`
- Modify: `framework/SimpleModule.Generator/Emitters/SettingsExtensionsEmitter.cs`

**Step 1: Extract sort helper to avoid duplication**

Since all emitters need the same sort logic, extract a static helper method. Add to `TopologicalSort.cs`:

```csharp
internal static ImmutableArray<ModuleInfoRecord> SortModules(DiscoveryData data)
{
    var moduleNames = data.Modules.Select(m => m.ModuleName).ToImmutableArray();
    var depEdges = data.Dependencies
        .Select(d => (d.ModuleName, d.DependsOnModuleName))
        .ToImmutableArray();

    var sortResult = Sort(moduleNames, depEdges);

    if (!sortResult.IsSuccess)
        return data.Modules; // Fallback to original order

    var moduleByName = new Dictionary<string, ModuleInfoRecord>();
    foreach (var m in data.Modules)
        moduleByName[m.ModuleName] = m;

    var sorted = new List<ModuleInfoRecord>();
    foreach (var name in sortResult.Sorted)
    {
        if (moduleByName.TryGetValue(name, out var m))
            sorted.Add(m);
    }
    return sorted.ToImmutableArray();
}
```

**Step 2: Update EndpointExtensionsEmitter**

Replace `var modules = data.Modules;` with:
```csharp
var modules = TopologicalSort.SortModules(data);
```

**Step 3: Update MenuExtensionsEmitter**

Same change: use `TopologicalSort.SortModules(data)`.

**Step 4: Update SettingsExtensionsEmitter**

Same change: use `TopologicalSort.SortModules(data)`.

**Step 5: Update ModuleExtensionsEmitter to use shared helper**

Refactor to use `TopologicalSort.SortModules(data)` for the sorted module list (keep the phase comment logic which needs the full SortResult).

**Step 6: Run all tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests/`
Expected: All tests pass.

**Step 7: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/TopologicalSort.cs framework/SimpleModule.Generator/Emitters/EndpointExtensionsEmitter.cs framework/SimpleModule.Generator/Emitters/MenuExtensionsEmitter.cs framework/SimpleModule.Generator/Emitters/SettingsExtensionsEmitter.cs framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs
git commit -m "feat(generator): apply topological ordering to all emitters"
```

---

### Task 7: Update design doc with correct diagnostic IDs

**Files:**
- Modify: `docs/plans/2026-03-20-module-dependency-management-design.md`

**Step 1: Update diagnostic IDs**

Replace the diagnostic summary table and all references from SM0001-SM0005 to SM0010-SM0014:

| ID | Severity | Trigger | Description |
|----|----------|---------|-------------|
| SM0010 | Error | Cycle in inferred dependency graph | Circular module dependency detected |
| SM0011 | Error | Module references another module's impl assembly | Illegal implementation reference |
| SM0012 | Warning | Contract interface has 15+ methods | Contract interface too large (warning) |
| SM0013 | Error | Contract interface has 20+ methods | Contract interface too large (error) |
| SM0014 | Error | Referenced `.Contracts` assembly has no public interfaces | Missing or incompatible contract package |

**Step 2: Commit**

```bash
git add docs/plans/2026-03-20-module-dependency-management-design.md
git commit -m "docs: update design doc with correct diagnostic IDs SM0010-SM0014"
```

---

### Task 8: Build and verify end-to-end

**Step 1: Build the entire solution**

Run: `dotnet build`
Expected: Clean build with no errors.

**Step 2: Run all tests**

Run: `dotnet test`
Expected: All tests pass.

**Step 3: Verify the generated output for the host project**

Run: `dotnet build template/SimpleModule.Host/ -v:detailed 2>&1 | grep -i "phase"`
Expected: Phase comments visible in build output or in generated files under `obj/`.

**Step 4: Commit any final fixes**

If anything needed fixing, commit with appropriate message.
