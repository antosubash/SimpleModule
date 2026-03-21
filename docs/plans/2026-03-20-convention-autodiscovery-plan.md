# Convention-Based Auto-Discovery Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace manual ceremony (contract DI registration, permission registration, `[Dto]` attributes) with convention-based auto-discovery in the source generator, plus 14 new compile-time diagnostics.

**Architecture:** Extend `SymbolDiscovery.Extract()` to collect contract implementations, permission classes, and Contracts-assembly public types. Add new record types to `DiscoveryData`. Extend `ModuleExtensionsEmitter` to emit contract and permission registrations. Extend `TypeScriptDefinitionsEmitter` and `JsonResolverEmitter` to include convention-based DTOs. Add new diagnostics to `DiagnosticEmitter`. Migrate existing modules to remove manual ceremony.

**Tech Stack:** Roslyn IIncrementalGenerator (netstandard2.0), C# source generation, xUnit generator tests.

**Design doc:** `docs/plans/2026-03-20-convention-autodiscovery-design.md`

---

## Task 1: Add `IModulePermissions` marker interface and `[NoDtoGeneration]` attribute to Core

**Files:**
- Create: `framework/SimpleModule.Core/Authorization/IModulePermissions.cs`
- Create: `framework/SimpleModule.Core/NoDtoGenerationAttribute.cs`

**Step 1: Create IModulePermissions**

```csharp
// framework/SimpleModule.Core/Authorization/IModulePermissions.cs
namespace SimpleModule.Core.Authorization;

/// <summary>
/// Marker interface for permission classes. Implementations are auto-discovered
/// by the source generator and registered with the permission registry.
/// Permission classes must be sealed and contain only public const string fields.
/// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
public interface IModulePermissions;
#pragma warning restore CA1040
```

**Step 2: Create NoDtoGenerationAttribute**

```csharp
// framework/SimpleModule.Core/NoDtoGenerationAttribute.cs
using System;

namespace SimpleModule.Core;

/// <summary>
/// Excludes a public type in a Contracts assembly from automatic DTO/TypeScript generation.
/// By convention, all public types in *.Contracts assemblies are treated as DTOs.
/// Apply this attribute to types that should not be included (e.g., marker interfaces, constants).
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
    AllowMultiple = false,
    Inherited = false
)]
public sealed class NoDtoGenerationAttribute : Attribute { }
```

**Step 3: Verify build**

Run: `dotnet build framework/SimpleModule.Core/SimpleModule.Core.csproj`

**Step 4: Commit**

```
feat(core): add IModulePermissions marker interface and [NoDtoGeneration] attribute
```

---

## Task 2: Extend DiscoveryData with new record types

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs`

**Step 1: Add new record types**

Add these new record types to `DiscoveryData.cs` in the equatable data model region:

```csharp
internal readonly record struct ContractImplementationRecord(
    string InterfaceFqn,
    string ImplementationFqn,
    string ModuleName,
    bool IsPublic,
    bool IsAbstract
);

internal readonly record struct PermissionClassRecord(
    string FullyQualifiedName,
    string ModuleName,
    bool IsSealed,
    ImmutableArray<PermissionFieldRecord> Fields
)
{
    public bool Equals(PermissionClassRecord other) =>
        FullyQualifiedName == other.FullyQualifiedName
        && ModuleName == other.ModuleName
        && IsSealed == other.IsSealed
        && Fields.SequenceEqual(other.Fields);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + (ModuleName ?? "").GetHashCode();
            hash = hash * 31 + IsSealed.GetHashCode();
            foreach (var f in Fields) hash = hash * 31 + f.GetHashCode();
            return hash;
        }
    }
}

internal readonly record struct PermissionFieldRecord(
    string FieldName,
    string Value,
    bool IsConstString
);
```

**Step 2: Add fields to DiscoveryData**

Add `ContractImplementations` and `PermissionClasses` to the `DiscoveryData` record constructor, default to empty in `DiscoveryData.Empty`, and include them in `Equals` and `GetHashCode`.

**Step 3: Add mutable working types**

Add to the mutable working types region:

```csharp
internal sealed class ContractImplementationInfo
{
    public string InterfaceFqn { get; set; } = "";
    public string ImplementationFqn { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool IsAbstract { get; set; }
}

internal sealed class PermissionClassInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public bool IsSealed { get; set; }
    public List<PermissionFieldInfo> Fields { get; set; } = new();
}

internal sealed class PermissionFieldInfo
{
    public string FieldName { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsConstString { get; set; }
}
```

**Step 4: Verify build**

Run: `dotnet build framework/SimpleModule.Generator/SimpleModule.Generator.csproj`

**Step 5: Commit**

```
feat(generator): add data model for contract implementations and permission classes
```

---

## Task 3: Extend SymbolDiscovery to find contract implementations

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

**Step 1: Add contract implementation discovery**

In `SymbolDiscovery.Extract()`, after the existing contract interface scanning (Step 3, around line 211), add discovery of contract implementations:

For each module assembly, for each contract interface found in the module's Contracts assembly:
1. Scan the module assembly for non-abstract classes implementing that interface
2. Record the implementation FQN, whether it's public, and whether it's abstract

Add a new method `FindContractImplementations` that:
- Takes a module assembly's global namespace, a list of contract interface FQNs from the contracts assembly, and the compilation
- Recursively walks namespaces looking for classes that implement any of the contract interfaces
- Returns `List<ContractImplementationInfo>`

**Step 2: Wire into Extract() and include in returned DiscoveryData**

The contract implementations should be included in the `DiscoveryData` constructor call at the end of `Extract()`.

**Step 3: Verify build**

Run: `dotnet build framework/SimpleModule.Generator/SimpleModule.Generator.csproj`

**Step 4: Commit**

```
feat(generator): discover contract interface implementations per module
```

---

## Task 4: Extend SymbolDiscovery to find IModulePermissions implementors

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

**Step 1: Add permission class discovery**

In `SymbolDiscovery.Extract()`, resolve the `IModulePermissions` symbol:
```csharp
var modulePermissionsSymbol = compilation.GetTypeByMetadataName(
    "SimpleModule.Core.Authorization.IModulePermissions"
);
```

For each module assembly, scan for classes implementing `IModulePermissions`. For each:
- Record FQN, module name (by closest namespace), whether sealed
- Collect all public fields: name, value, whether it's `const string`

Add a `FindPermissionClasses` method similar to `FindEndpointTypes`.

**Step 2: Wire into Extract() and DiscoveryData**

**Step 3: Verify build**

Run: `dotnet build framework/SimpleModule.Generator/SimpleModule.Generator.csproj`

**Step 4: Commit**

```
feat(generator): discover IModulePermissions implementors per module
```

---

## Task 5: Extend SymbolDiscovery to find convention-based DTOs in Contracts assemblies

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`

**Step 1: Modify DTO scanning**

Currently `FindDtoTypes` only finds types with `[Dto]` attribute. Extend it:

After the existing `[Dto]`-based scanning, add a second pass over Contracts assemblies (already in `contractsAssemblySymbols`):
- For each Contracts assembly, scan all public classes/records/structs
- Skip types that have `[NoDtoGeneration]` attribute
- Skip interfaces (they're not serializable DTOs)
- Skip types already found via `[Dto]`
- Add them to `dtoTypes` using the same `DtoTypeInfo` structure

Resolve the `NoDtoGenerationAttribute` symbol:
```csharp
var noDtoAttrSymbol = compilation.GetTypeByMetadataName(
    "SimpleModule.Core.NoDtoGenerationAttribute"
);
```

**Step 2: Verify build**

Run: `dotnet build framework/SimpleModule.Generator/SimpleModule.Generator.csproj`

**Step 3: Commit**

```
feat(generator): auto-discover DTOs from public types in Contracts assemblies
```

---

## Task 6: Extend ModuleExtensionsEmitter to emit contract registrations

**Files:**
- Modify: `framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs`

**Step 1: Generate AddScoped calls for discovered contracts**

In `ModuleExtensionsEmitter.Emit()`, after the module `ConfigureServices` calls and before the permission section, emit:

```csharp
// Auto-discovered contract registrations
sb.AppendLine();
sb.AppendLine("        // Contract implementations (auto-discovered)");
foreach (var impl in data.ContractImplementations)
{
    if (impl.IsPublic && !impl.IsAbstract)
    {
        sb.AppendLine($"        services.AddScoped<{impl.InterfaceFqn}, {impl.ImplementationFqn}>();");
    }
}
```

Only emit for valid implementations (public, non-abstract, exactly one per interface — diagnostics handle the error cases).

**Step 2: Verify build**

Run: `dotnet build`

**Step 3: Commit**

```
feat(generator): emit auto-discovered contract DI registrations in AddModules()
```

---

## Task 7: Extend ModuleExtensionsEmitter to emit permission registrations

**Files:**
- Modify: `framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs`

**Step 1: Generate AddPermissions calls for discovered permission classes**

In the permission section of `Emit()`, after the existing `ConfigurePermissions` calls, add:

```csharp
// Auto-discovered permission classes (IModulePermissions)
foreach (var perm in data.PermissionClasses)
{
    if (perm.IsSealed)
    {
        sb.AppendLine($"        permissionBuilder.AddPermissions<{perm.FullyQualifiedName}>();");
    }
}
```

**Step 2: Verify build**

Run: `dotnet build`

**Step 3: Commit**

```
feat(generator): emit auto-discovered permission registrations in AddModules()
```

---

## Task 8: Add all new diagnostics to DiagnosticEmitter

**Files:**
- Modify: `framework/SimpleModule.Generator/Emitters/DiagnosticEmitter.cs`

**Step 1: Add DiagnosticDescriptor definitions**

Add SM0025–SM0038 as `DiagnosticDescriptor` fields following the existing pattern. Use exact IDs and messages from the design doc:

- SM0025: No contract implementation found
- SM0026: Multiple contract implementations found
- SM0028: Contract implementation not public
- SM0029: Contract implementation is abstract
- SM0027: Permission class has non-const-string fields
- SM0031: Permission value wrong naming convention
- SM0032: Permission class not sealed
- SM0033: Duplicate permission values
- SM0034: Permission value prefix doesn't match module
- SM0035: Contracts type with no public properties
- SM0036: Non-serializable DTO property type
- SM0037: Circular DTO reference
- SM0038: Infrastructure type in Contracts

**Step 2: Add diagnostic reporting logic in Emit()**

For contract diagnostics (SM0025, SM0026, SM0028, SM0029):
- Group `ContractImplementations` by `InterfaceFqn`
- For each contract interface: check count, public/abstract status

For permission diagnostics (SM0027, SM0031, SM0032, SM0033, SM0034):
- Iterate `PermissionClasses`
- Check sealed, field types, value format, duplicates, module prefix

For DTO diagnostics (SM0035, SM0036, SM0037, SM0038):
- Iterate DTOs from Contracts assemblies
- Check property count, property types, circular refs, infrastructure types (DbContext, etc.)

**Step 3: Verify build**

Run: `dotnet build framework/SimpleModule.Generator/SimpleModule.Generator.csproj`

**Step 4: Commit**

```
feat(generator): add SM0025-SM0038 diagnostics for auto-discovery validation
```

---

## Task 9: Add generator tests for contract auto-discovery

**Files:**
- Create: `tests/SimpleModule.Generator.Tests/ContractAutoDiscoveryTests.cs`

**Step 1: Write tests**

Using `GeneratorTestHelper.CreateCompilation()` and `RunGeneratorWithDiagnostics()`:

Test cases:
- `SingleImplementation_GeneratesRegistration` — module with one implementation of contract interface → generated code contains `AddScoped`
- `NoImplementation_EmitsSM0025` — contract interface with no implementation → SM0025 diagnostic
- `MultipleImplementations_EmitsSM0026` — two classes implementing same contract → SM0026 diagnostic
- `InternalImplementation_EmitsSM0028` — internal class implementing contract → SM0028 diagnostic
- `AbstractImplementation_EmitsSM0029` — abstract class implementing contract → SM0029 diagnostic

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests/ --filter ContractAutoDiscovery`

**Step 3: Commit**

```
test(generator): add tests for contract auto-discovery and SM0025-SM0029 diagnostics
```

---

## Task 10: Add generator tests for permission auto-discovery

**Files:**
- Create: `tests/SimpleModule.Generator.Tests/PermissionAutoDiscoveryTests.cs`

**Step 1: Write tests**

Test cases:
- `SealedPermissionClass_GeneratesRegistration` — sealed class with IModulePermissions → generated `AddPermissions` call
- `NonSealedClass_EmitsSM0032` — non-sealed class → SM0032 diagnostic
- `NonConstField_EmitsSM0027` — class with non-const or non-string field → SM0027 diagnostic
- `WrongNamingConvention_EmitsSM0031` — permission value not matching `{Module}.{Action}` → SM0031
- `DuplicateValues_EmitsSM0033` — same permission value in two classes → SM0033
- `WrongPrefix_EmitsSM0034` — permission value prefix doesn't match module name → SM0034

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests/ --filter PermissionAutoDiscovery`

**Step 3: Commit**

```
test(generator): add tests for permission auto-discovery and SM0027-SM0034 diagnostics
```

---

## Task 11: Add generator tests for convention-based DTO discovery

**Files:**
- Create: `tests/SimpleModule.Generator.Tests/DtoConventionTests.cs`

**Step 1: Write tests**

Test cases:
- `PublicContractsType_IncludedAsDto` — public class in Contracts assembly → included in TypeScript generation
- `NoDtoGenerationAttribute_Excluded` — class with `[NoDtoGeneration]` → excluded
- `InterfaceInContracts_Excluded` — interfaces are not DTOs
- `ExplicitDtoAttribute_StillWorks` — `[Dto]` on non-Contracts type still works
- `NoPublicProperties_EmitsSM0035` — public class with no properties → SM0035
- `InfrastructureType_EmitsSM0038` — DbContext subclass in Contracts → SM0038

**Step 2: Run tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests/ --filter DtoConvention`

**Step 3: Commit**

```
test(generator): add tests for convention-based DTO discovery and SM0035-SM0038 diagnostics
```

---

## Task 12: Migrate existing modules — add IModulePermissions to permission classes

**Files:**
- Modify: `modules/Products/src/Products/ProductsPermissions.cs`
- Modify: `modules/Orders/src/Orders/OrdersPermissions.cs` (if exists)
- Modify: `modules/Users/src/Users/UsersPermissions.cs` (if exists)
- Modify: `modules/Admin/src/Admin/AdminPermissions.cs` (if exists)
- Modify: `modules/Permissions/src/Permissions/PermissionsPermissions.cs` (if exists)
- Modify: `modules/AuditLogs/src/AuditLogs/AuditLogsPermissions.cs`
- Modify: All other `*Permissions.cs` files

For each permission class, add `: IModulePermissions` and the using `using SimpleModule.Core.Authorization;`.

Example:
```csharp
using SimpleModule.Core.Authorization;

namespace SimpleModule.Products;

public sealed class ProductsPermissions : IModulePermissions
{
    public const string View = "Products.View";
    public const string Create = "Products.Create";
    public const string Update = "Products.Update";
    public const string Delete = "Products.Delete";
}
```

**Step 1: Find all permission classes**

Run: `grep -r "sealed class.*Permissions" modules/ --include="*.cs" -l`

**Step 2: Add IModulePermissions to each**

**Step 3: Remove ConfigurePermissions overrides from module classes**

For each module that now has auto-discovered permissions, remove the `ConfigurePermissions` override since the generator handles it.

**Step 4: Verify build**

Run: `dotnet build`

**Step 5: Run all tests**

Run: `dotnet test`

**Step 6: Commit**

```
refactor: migrate permission classes to IModulePermissions auto-discovery
```

---

## Task 13: Migrate existing modules — remove manual contract registrations

**Files:**
- Modify: All `*Module.cs` files that have `services.AddScoped<I*Contracts, *Service>()`

For each module, remove the manual `AddScoped` line for the contract interface since the generator now handles it. Keep other DI registrations (DbContext, caches, etc.).

**Step 1: Find all contract registrations**

Run: `grep -r "AddScoped<I.*Contracts" modules/ --include="*.cs" -l`

**Step 2: Remove manual contract registrations from each module's ConfigureServices**

**Step 3: Verify build**

Run: `dotnet build`

**Step 4: Run all tests**

Run: `dotnet test`

**Step 5: Commit**

```
refactor: remove manual contract DI registrations in favor of auto-discovery
```

---

## Task 14: Optionally remove [Dto] attributes from Contracts types

**Files:**
- Modify: All `*.Contracts` projects — remove `[Dto]` attributes from public types

This is optional cleanup. The `[Dto]` attributes are now redundant on Contracts types since they're auto-discovered by convention. Removing them reduces noise.

**Step 1: Find all [Dto] in Contracts projects**

Run: `grep -rn "\[Dto\]" modules/*/src/*.Contracts/ --include="*.cs"`

**Step 2: Remove [Dto] attributes from Contracts types**

Keep `[Dto]` on types in non-Contracts assemblies (like `PagedResult<T>` in Core).

**Step 3: Verify build**

Run: `dotnet build`

**Step 4: Run all tests**

Run: `dotnet test`

**Step 5: Commit**

```
refactor: remove redundant [Dto] attributes from Contracts types
```

---

## Task 15: Full verification and cleanup

**Step 1: Full solution build**

Run: `dotnet build`

**Step 2: All tests**

Run: `dotnet test`

**Step 3: Verify generated code**

Check `template/SimpleModule.Host/obj/Debug/net10.0/generated/SimpleModule.Generator/` for the generated `ModuleExtensions.g.cs` — verify it contains:
- Auto-discovered contract `AddScoped` registrations
- Auto-discovered `AddPermissions` calls
- No duplicate registrations

**Step 4: Verify TypeScript generation**

Run: `npm run generate:types`
Verify that Contracts types are included in generated `.ts` files.

**Step 5: Commit if any fixes needed**

```
chore: verify convention-based auto-discovery integration
```
