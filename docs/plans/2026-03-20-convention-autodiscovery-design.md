# Convention-Based Auto-Discovery Design

## Overview

Replace manual ceremony in module classes with convention-based auto-discovery in the source generator. Three features: contract DI registration, permission discovery, and DTO convention for Contracts assemblies.

Principle: make mistakes impossible rather than detectable.

## 1. Contract Registration Auto-Discovery

### How It Works

The generator scans each module assembly for classes implementing the module's `I{Module}Contracts` interface from the corresponding Contracts assembly. If exactly one concrete, public implementation is found, it generates `services.AddScoped<IProductContracts, ProductService>()` in the `AddModules()` extension method.

### What Changes

- Generator emits contract DI registration in `AddModules()`
- Modules no longer need `services.AddScoped<IProductContracts, ProductService>()` in `ConfigureServices`
- Existing manual registrations still work during migration period

### Compile Errors

| ID | Condition | Message |
|----|-----------|---------|
| SM0025 | Zero implementations found | No implementation of '{0}' found in module '{1}'. Add a class implementing this interface. |
| SM0026 | Multiple implementations found | Multiple implementations of '{0}' found: {1}. Only one implementation per contract interface is allowed. |
| SM0028 | Implementation is internal/private | Implementation '{0}' of '{1}' must be public. The DI container cannot access internal types across assemblies. |
| SM0029 | Implementation is abstract | '{0}' implements '{1}' but is abstract. Provide a concrete implementation. |
| SM0030 | Unresolvable constructor parameter | '{0}' constructor requires '{1}' which is not a registered service or DbContext. Verify all constructor parameters are available in DI. |

---

## 2. Permissions Auto-Discovery

### How It Works

New `IModulePermissions` empty marker interface in `SimpleModule.Core`. The generator scans module assemblies for classes implementing it. For each, it calls `builder.AddPermissions<T>()` in the generated code.

### New Interface

```csharp
// SimpleModule.Core/Authorization/IModulePermissions.cs
public interface IModulePermissions { }
```

### Migration

Change `public sealed class ProductsPermissions` → `public sealed class ProductsPermissions : IModulePermissions`. Remove `ConfigurePermissions` override from module classes.

### Compile Errors

| ID | Condition | Message |
|----|-----------|---------|
| SM0027 | Non-const or non-string public fields | Permission class '{0}' must contain only public const string fields. Found field '{1}' of type '{2}'. |
| SM0031 | Value doesn't follow naming convention | Permission '{0}' in '{1}' should follow the '{Module}.{Action}' pattern (e.g., 'Products.View'). |
| SM0032 | Class not sealed | '{0}' implements IModulePermissions but is not sealed. Permission classes must be sealed. |
| SM0033 | Duplicate permission values across modules | Permission value '{0}' is defined in both '{1}' and '{2}'. Each permission value must be unique. |
| SM0034 | Value prefix doesn't match module name | Permission '{0}' is defined in module '{1}'. Permission values must be prefixed with the owning module name '{1}'. |

---

## 3. DTO Convention for Contracts Assemblies

### How It Works

- All public classes/records/structs in `*.Contracts` assemblies are treated as DTOs automatically — no `[Dto]` needed
- `[Dto]` still works for types outside Contracts assemblies (e.g., `PagedResult<T>` in Core)
- New `[NoDtoGeneration]` attribute to exclude a type from TypeScript generation
- Existing `[Dto]` attributes on Contracts types become redundant (no-op, no warning)

### What Changes

- Generator's DTO scanning expands: Contracts assembly public types + any `[Dto]` types elsewhere
- New `[NoDtoGeneration]` attribute in Core
- Existing `[Dto]` attributes can be gradually removed from Contracts types

### Compile Errors

| ID | Condition | Message |
|----|-----------|---------|
| SM0035 | Public type with no public properties | '{0}' in '{1}' has no public properties. If this is not a DTO, mark it with [NoDtoGeneration]. If it is, add public properties. |
| SM0036 | Non-serializable property type | Property '{0}' on '{1}' has type '{2}' which cannot be serialized to JSON or TypeScript. Use primitive types, collections, or other DTOs. |
| SM0037 | Circular reference | '{0}' creates a circular reference: {1}. This will cause infinite recursion during JSON serialization. |
| SM0038 | Infrastructure type in Contracts | '{0}' in '{1}' is a {2}. Infrastructure types should not be in Contracts assemblies. Move it to the module implementation project. |

---

## Backward Compatibility

All three changes are additive and backward compatible:

- Manual `AddScoped` calls in `ConfigureServices` still work
- `ConfigurePermissions` overrides still work
- `[Dto]` attributes still work everywhere

The generator generates the auto-discovered registrations alongside any manual ones. Over time, modules migrate to the convention and remove the manual ceremony.

## Migration Path

1. Add new interfaces and attributes to Core
2. Update generator with auto-discovery logic and diagnostics
3. Migrate existing modules one at a time:
   - Add `: IModulePermissions` to permission classes
   - Remove `ConfigurePermissions` override
   - Remove `services.AddScoped<I*Contracts, *Service>()` from `ConfigureServices`
   - Optionally remove `[Dto]` from Contracts types
4. Once all modules migrated, remove manual registration support (optional)
