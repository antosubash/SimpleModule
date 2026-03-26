---
outline: deep
---

# Type Generation

SimpleModule automatically generates TypeScript interfaces from your C# DTO types. This ensures your React frontend always has accurate type definitions that match the server-side data shapes, with zero manual synchronization.

## The Pipeline

Type generation is a three-stage pipeline:

```
C# DTO types in *.Contracts assemblies
    │
    ▼
Source Generator (compile time)
    │  Reads public types from Contracts assemblies
    │  Maps C# types to TypeScript types
    │  Embeds TS interfaces as comments in DtoTypeScript_{Module}.g.cs
    │
    ▼
extract-ts-types.mjs (build tool)
    │  Reads generated .g.cs files from obj/ directory
    │  Extracts TypeScript interfaces from comment blocks
    │  Writes types.ts into each module's src/ directory
    │
    ▼
modules/{Module}/src/{Module}/types.ts
    Ready for import in React components
```

## Marking Types for Generation

### Convention-Based Discovery

By default, **all public types** in `*.Contracts` assemblies are treated as DTOs and included in TypeScript generation. You do not need to add any attributes to your types.

For example, this class in `Products.Contracts`:

```csharp
namespace SimpleModule.Products.Contracts;

public class Product
{
    public ProductId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

is automatically discovered and generates a TypeScript interface.

### The `[Dto]` Attribute

The `[Dto]` attribute can be used to explicitly mark types for generation in assemblies that are not `*.Contracts` assemblies:

```csharp
using SimpleModule.Core;

[Dto]
public class CustomResponse
{
    public string Message { get; set; } = string.Empty;
    public int Code { get; set; }
}
```

The attribute targets classes and structs:

```csharp
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false
)]
public sealed class DtoAttribute : Attribute { }
```

### The `[NoDtoGeneration]` Escape Hatch

To exclude a type in a Contracts assembly from TypeScript generation, apply `[NoDtoGeneration]`:

```csharp
using SimpleModule.Core;

namespace SimpleModule.Products.Contracts;

[NoDtoGeneration]
public class InternalHelper
{
    // This type will not appear in types.ts
}
```

This attribute can be applied to classes, structs, and interfaces:

```csharp
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
    AllowMultiple = false,
    Inherited = false
)]
public sealed class NoDtoGenerationAttribute : Attribute { }
```

::: tip When to use [NoDtoGeneration]
Use it for types that live in a Contracts assembly but are not meant for the frontend -- for example, contract interfaces like `IProductContracts`, internal helper types, or types used only for inter-module communication.
:::

## Running Type Generation

Generate TypeScript types with:

```bash
npm run generate:types
```

This runs the `extract-ts-types.mjs` tool, which reads the source generator's output files from the `obj/` directory and writes `types.ts` files into each module.

::: info
You must build the .NET project before running type generation, since the tool reads the generated `.g.cs` files from the build output.
:::

## Output Location

Each module gets its own `types.ts` file at:

```
modules/{ModuleName}/src/{ModuleName}/types.ts
```

For example, the Products module produces:

```
modules/Products/src/Products/types.ts
```

The file is marked as auto-generated and should not be edited manually:

```typescript
// Auto-generated from [Dto] types — do not edit
export interface CreateProductRequest {
  name: string;
  price: number;
}

export interface Product {
  id: number;
  name: string;
  price: number;
}

export interface UpdateProductRequest {
  name: string;
  price: number;
}
```

## Type Mapping

The source generator maps C# types to TypeScript types using the following rules:

### Primitive Types

| C# Type | TypeScript Type |
|---------|----------------|
| `string` | `string` |
| `int`, `long`, `short`, `byte` | `number` |
| `float`, `double`, `decimal` | `number` |
| `bool` | `boolean` |
| `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` | `string` |
| `Guid` | `string` |

### Nullable Types

`Nullable<T>` (or `T?`) maps to `T | null`:

| C# Type | TypeScript Type |
|---------|----------------|
| `int?` | `number \| null` |
| `string?` | `string \| null` |
| `DateTime?` | `string \| null` |

### Collection Types

Generic collections map to TypeScript arrays:

| C# Type | TypeScript Type |
|---------|----------------|
| `List<T>` | `T[]` |
| `IList<T>` | `T[]` |
| `IEnumerable<T>` | `T[]` |
| `IReadOnlyList<T>` | `T[]` |
| `ICollection<T>` | `T[]` |

### DTO References

When a property references another `[Dto]` type, the generator resolves it to the TypeScript interface name rather than `any`.

### Value Objects

Vogen value objects (strongly-typed IDs, etc.) are mapped to their **underlying primitive type**. For example, a `ProductId` wrapping `int` maps to `number` in TypeScript.

### Unknown Types

Any type not recognized by the mapping rules falls back to `any`.

## Using Generated Types in React

Import the generated types directly in your React components:

```tsx
import type { Product, CreateProductRequest } from '../types';

interface BrowseProps {
  products: Product[];
}

export default function Browse({ products }: BrowseProps) {
  return (
    <table>
      <thead>
        <tr>
          <th>Name</th>
          <th>Price</th>
        </tr>
      </thead>
      <tbody>
        {products.map((product) => (
          <tr key={product.id}>
            <td>{product.name}</td>
            <td>${product.price.toFixed(2)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
```

## How It Works Internally

The source generator embeds TypeScript interfaces as **comments inside C# files**. This approach allows the TS definitions to travel through the normal build pipeline without affecting compilation:

```csharp
// <auto-generated/>
#if SIMPLEMODULE_TS
/*
// @module Products

export interface Product {
  id: number;
  name: string;
  price: number;
}

*/
#endif
```

The `extract-ts-types.mjs` tool then:

1. Reads all `DtoTypeScript_*.g.cs` files from the generated output directory
2. Extracts the module name from the `// @module` comment
3. Parses the TypeScript interfaces from the comment block
4. Writes a `types.ts` file to the module's source directory

Property names are automatically converted from `PascalCase` (C#) to `camelCase` (TypeScript) during generation, matching the default `System.Text.Json` serialization behavior.
