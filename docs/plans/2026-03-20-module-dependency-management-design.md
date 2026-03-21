# Module Dependency Management Design

## Problem Statement

The SimpleModule framework has no dependency management infrastructure. Modules are discovered alphabetically, can reference each other's implementations directly, and contract interfaces can grow unbounded. This creates five concrete problems:

1. **Circular dependencies** — nothing prevents Module A ↔ Module B cycles
2. **No dependency ordering** — modules initialize in alphabetical order, not dependency order
3. **Fat contract interfaces** — single `IModuleContracts` per module violates Interface Segregation
4. **No compile-time guardrails** — developers can reference implementation assemblies directly
5. **Package compatibility** — no clear diagnostics when contract packages are incompatible

## Design Decisions

- **Fully automatic** — dependencies inferred from assembly references, zero configuration required
- **All validation at compile time** via the source generator (no runtime validator)
- **Generator-centric** — all five solutions live in `SimpleModule.Generator`
- **Distribution model** — monorepo for first-party modules, NuGet packages for third-party
- **Thresholds configurable** via `.editorconfig`
- **Host project exempt** from implementation reference checks

## Solution: Source Generator Dependency Analysis

### 1. Automatic Dependency Inference

The source generator adds a new analysis pass in `SymbolDiscovery` after module discovery:

1. **Build contracts-to-module map** — For each discovered module in assembly `X`, check if an assembly `X.Contracts` exists in references. Map `X.Contracts` → Module `X`.

2. **Infer dependencies** — For each module in assembly `Y`, scan `Y`'s assembly references. For each reference that matches a known `.Contracts` assembly (excluding `Y.Contracts` itself), record: "Module Y depends on Module X."

3. **Topological sort** — Order modules by dependencies. Module X (no deps) initializes before Module Y (depends on X). The generated `AddModules()` method calls `ConfigureServices` in this order.

4. **Cycle detection** — During topological sort, if a cycle is found, emit a compile error.

**Diagnostic SM0010 — Circular dependency:**

```
error SM0010: Circular module dependency detected.

  Cycle: Orders → Products → Orders

  How this happened:
    • Orders.csproj references Products.Contracts (IProductContracts)
    • Products.csproj references Orders.Contracts (IOrderContracts)

  How to fix it:
    One of these modules must not directly depend on the other.
    Identify which direction is the "primary" dependency and reverse
    the other using the event bus.

    For example, if Orders is the primary consumer of Products:
      1. Keep the reference: Orders → Products.Contracts ✓
      2. Remove the reference: Products → Orders.Contracts ✗
      3. In Products, publish an event instead:
           await eventBus.PublishAsync(new ProductCreatedEvent(...));
      4. In Orders, handle it:
           public class OnProductCreated : IEventHandler<ProductCreatedEvent> { ... }

  Learn more: https://docs.simplemodule.dev/module-dependencies
```

### 2. Illegal Implementation Reference Detection

During the assembly scanning pass, the generator knows which assemblies contain `[Module]` classes. If module assembly `Y` references module assembly `X` (not `X.Contracts`), that's a violation. The Host project is exempt since it legitimately references all implementation assemblies.

**Diagnostic SM0011 — Illegal implementation reference:**

```
error SM0011: Module 'Orders' directly references module 'Products' implementation.

  What happened:
    Orders.csproj has a reference to Products (the implementation assembly).
    Modules must only depend on each other through Contracts packages.

  Why this is a problem:
    Referencing the implementation creates tight coupling between modules.
    It bypasses the Contracts boundary, meaning internal changes in
    Products can break Orders at compile time or runtime.

  How to fix it:
    1. In Orders.csproj, remove the reference to Products:
         <ProjectReference Include="..\..\Products\src\Products\Products.csproj" />  ← remove

    2. Add a reference to Products.Contracts instead:
         <ProjectReference Include="..\..\Products\src\Products.Contracts\Products.Contracts.csproj" />

    3. Replace any usage of internal Products types with their
       contract interfaces (e.g., ProductService → IProductContracts).

  Learn more: https://docs.simplemodule.dev/module-contracts
```

### 3. Contract Interface Hygiene (ISP Enforcement)

The generator scans all public interfaces in `.Contracts` assemblies. If an interface exceeds a configurable threshold, it emits a warning or error.

- **Warning at 15+ methods** (configurable via `simplemodule.max_contract_methods_warn`)
- **Error at more than 20 methods** (configurable via `simplemodule.max_contract_methods_error`)

**Diagnostic SM0012 — Warning:**

```
warning SM0012: Contract interface 'IProductContracts' has 16 methods.

  Location: Products.Contracts/IProductContracts.cs

  Why this matters:
    Large contract interfaces force consuming modules to depend on
    methods they don't use. When any method signature changes, all
    consumers must recompile — even those using unrelated methods.

  How to fix it:
    Split the interface into focused concerns. For example:

      IProductContracts (16 methods)
        ├── IProductQueries    → GetById, Search, GetByCatalog
        ├── IProductCommands   → Create, Update, Delete
        └── IProductInventory  → CheckStock, Reserve, Release

    Each consuming module then references only the interface it needs.
    Your module class can implement all of them:

      public class ProductService : IProductQueries, IProductCommands, IProductInventory

  Thresholds (configurable in .editorconfig):
    simplemodule.max_contract_methods_warn = 15
    simplemodule.max_contract_methods_error = 20

  Learn more: https://docs.simplemodule.dev/contract-design
```

**Diagnostic SM0013 — Error (at 20+ methods):**

Same structure as SM0012 but as a compile error with stronger language: "This interface must be split before the project will compile."

### 4. Contract Resolution Validation

When the generator infers that Module B depends on Module A (because B references `A.Contracts`), it verifies that `A.Contracts` contains at least one public interface. If the contracts assembly is referenced but contains no public interfaces, something is wrong.

**Diagnostic SM0014 — Missing contract interfaces:**

```
error SM0014: Module 'Orders' references 'Products.Contracts' but no
  contract interfaces were found in that assembly.

  What happened:
    Orders.csproj references Products.Contracts, but the generator
    could not find any public interfaces in that assembly.

  Likely causes:
    1. Incompatible package version — you may have installed a version
       of Products.Contracts that reorganized or removed its interfaces.
       Check your installed version:
         dotnet list Orders.csproj package --include-transitive

    2. The Contracts project is empty or not yet built — ensure
       Products.Contracts defines at least one public interface.

    3. The package is corrupted — try clearing the NuGet cache:
         dotnet nuget locals all --clear
         dotnet restore

  How to fix it:
    Verify that the version of Products.Contracts you're using
    exports the interfaces your code depends on. Check the package
    release notes for breaking changes.

  Learn more: https://docs.simplemodule.dev/package-compatibility
```

### 5. Generated Module Ordering

The generated `AddModules()` uses topological sort order with phase comments so developers can inspect the resolved dependency graph:

```csharp
// Generated: topologically sorted — dependencies initialize first
public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
{
    // Phase 1: No dependencies
    Settings.Instance.ConfigureServices(services, configuration);
    Products.Instance.ConfigureServices(services, configuration);

    // Phase 2: Depends on Settings
    AuditLogs.Instance.ConfigureServices(services, configuration);

    // Phase 3: Depends on Products
    Orders.Instance.ConfigureServices(services, configuration);

    // Phase 4: Depends on AuditLogs, Settings
    Admin.Instance.ConfigureServices(services, configuration);
}
```

The same ordering applies to `MapModuleEndpoints()`, `CollectModuleMenuItems()`, and `CollectModuleSettings()`.

## Diagnostic Summary

| ID | Severity | Trigger | Description |
|----|----------|---------|-------------|
| SM0010 | Error | Cycle in inferred dependency graph | Circular module dependency detected |
| SM0011 | Error | Module references another module's impl assembly | Illegal implementation reference |
| SM0012 | Warning | Contract interface has 15+ methods | Contract interface too large (warning) |
| SM0013 | Error | Contract interface has 20+ methods | Contract interface too large (error) |
| SM0014 | Error | Referenced `.Contracts` assembly has no public interfaces | Missing or incompatible contract package |

## What Doesn't Change

- The `[Module]` attribute — no new properties needed
- The Contracts pattern — same project structure, now enforced
- The event bus — unchanged, recommended in cycle-breaking guidance
- NuGet versioning — rely on semver + compiler, with clear SM0014 diagnostics

## Package Versioning Strategy

No custom versioning infrastructure. The C# compiler catches binary incompatibility natively, and NuGet semver conventions handle the rest. The framework adds clear SM0014 diagnostics when contracts assemblies are missing expected types, pointing users to version checks and cache clearing.
