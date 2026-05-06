---
outline: deep
---

# Contracts

Contracts are the public API of a module. They define the interface through which other modules interact with your module, without exposing implementation details. This is the key mechanism that keeps modules decoupled in a SimpleModule application.

## Why Contracts?

In a modular monolith, modules must communicate, but direct dependencies between module implementations would create tight coupling. The contracts pattern solves this:

- Modules depend on **interfaces**, never implementations
- Each module exposes a `.Contracts` project with its public types
- The source generator auto-registers the implementation at startup
- At runtime, DI resolves the concrete service behind the interface

This means you can refactor a module's internals freely without breaking other modules, as long as the contract interface stays stable.

## Contracts Project Structure

Each module has a separate contracts project alongside its main project:

```
modules/Customers/
  src/
    Customers.Contracts/         # Public API -- other modules depend on this
      Customers.Contracts.csproj
      ICustomerContracts.cs      # The interface
      Customer.cs                # Shared DTO
      CustomerId.cs              # Strongly-typed ID
      CreateCustomerRequest.cs   # Request DTO
      UpdateCustomerRequest.cs   # Request DTO
    Customers/                   # Private implementation -- no one depends on this
      CustomersModule.cs
      CustomerService.cs         # Implements ICustomerContracts
      ...
```

The contracts project uses `Microsoft.NET.Sdk` (not Razor) and references only `SimpleModule.Core`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference
      Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

::: tip
Keep contracts projects minimal. They should contain only the interface, DTOs, events, and strongly-typed IDs. No business logic, no database dependencies, no ASP.NET references.
:::

## The `I<Name>Contracts` Interface

Each module exposes a single contract interface that defines all operations other modules can call:

```csharp
namespace SimpleModule.Customers.Contracts;

public interface ICustomerContracts
{
    Task<IEnumerable<Customer>> GetAllCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(CustomerId id);
    Task<IReadOnlyList<Customer>> GetCustomersByIdsAsync(IEnumerable<CustomerId> ids);
    Task<Customer> CreateCustomerAsync(CreateCustomerRequest request);
    Task<Customer> UpdateCustomerAsync(CustomerId id, UpdateCustomerRequest request);
    Task DeleteCustomerAsync(CustomerId id);
}
```

The source generator discovers the concrete implementation of this interface and auto-registers it as a scoped service. You never write `services.AddScoped<ICustomerContracts, CustomerService>()` by hand -- the generated `AddModules()` method handles it.

### Invoices Contract

Here is another example showing a hypothetical Invoices module contract:

```csharp
namespace SimpleModule.Invoices.Contracts;

public interface IInvoiceContracts
{
    Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
    Task<Invoice?> GetInvoiceByIdAsync(InvoiceId id);
    Task<Invoice> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<Invoice> UpdateInvoiceAsync(InvoiceId id, UpdateInvoiceRequest request);
    Task DeleteInvoiceAsync(InvoiceId id);
}
```

## Shared DTO Types

DTOs (Data Transfer Objects) defined in contracts projects are the data shapes shared between modules. They should be plain classes with public properties:

```csharp
namespace SimpleModule.Customers.Contracts;

public class Customer
{
    public CustomerId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

```csharp
namespace SimpleModule.Customers.Contracts;

public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

### Strongly-Typed IDs

Modules use [Vogen](https://github.com/SteveDunn/Vogen) to define strongly-typed IDs that prevent accidental misuse of raw integer or GUID values:

```csharp
using Vogen;

namespace SimpleModule.Customers.Contracts;

[ValueObject<int>(
    conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct CustomerId;
```

The `Conversions` flags generate JSON and EF Core value converters automatically, so the ID type works seamlessly in API serialization and database queries.

::: tip
Strongly-typed IDs belong in the contracts project, not in Core. They are domain types specific to their module.
:::

## Cross-Module Dependencies

When one module needs to use another module's data, it depends on the contracts project -- never the implementation.

For example, a hypothetical Invoices module uses `CustomerId` from Customers and `UserId` from Users:

```csharp
// Invoices.Contracts/InvoiceLine.cs
using SimpleModule.Customers.Contracts;

namespace SimpleModule.Invoices.Contracts;

public class InvoiceLine
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
```

```csharp
// Invoices.Contracts/Invoice.cs
using SimpleModule.Customers.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Invoices.Contracts;

public class Invoice
{
    public InvoiceId Id { get; set; }
    public CustomerId CustomerId { get; set; }
    public UserId IssuedBy { get; set; }
    public List<InvoiceLine> Lines { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

The project file makes the dependency explicit:

```xml
<!-- Invoices.Contracts.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\Customers\src\Customers.Contracts\Customers.Contracts.csproj" />
    <ProjectReference Include="..\..\..\Users\src\Users.Contracts\Users.Contracts.csproj" />
  </ItemGroup>
</Project>
```

The source generator detects these inter-module dependencies and ensures modules are registered in the correct order (Customers before Invoices).

## The `[Dto]` Attribute

The `[Dto]` attribute marks types for automatic TypeScript interface generation:

```csharp
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false)]
public sealed class DtoAttribute : Attribute { }
```

### Convention-Based Discovery

By convention, **all public types in `*.Contracts` assemblies are treated as DTOs** and will have TypeScript interfaces generated automatically. You do not need to apply `[Dto]` to types in contracts projects.

The `[Dto]` attribute is needed for types **outside** contracts assemblies that should still participate in TypeScript generation:

```csharp
// In a non-contracts project -- [Dto] is required here
using SimpleModule.Core;

namespace SimpleModule.AuditLogs.Contracts;

[Dto]
public class DashboardStats
{
    public int TotalEntries { get; set; }
    public int UniqueUsers { get; set; }
    public double AverageDurationMs { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, int> BySource { get; set; } = new();
    public List<NamedCount> TopUsers { get; set; } = [];
    public List<TimelinePoint> Timeline { get; set; } = [];
}

[Dto]
public class NamedCount
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

[Dto]
public class TimelinePoint
{
    public string Date { get; set; } = "";
    public int Http { get; set; }
    public int Domain { get; set; }
    public int Changes { get; set; }
}
```

## TypeScript Generation Pipeline

The TypeScript generation pipeline converts C# DTO types into TypeScript interfaces that your React components can use:

1. **Source generator** scans for `[Dto]` types and public types in `*.Contracts` assemblies
2. **TypeScript definitions are embedded** in the generated source as string resources
3. **`extract-ts-types.mjs`** extracts the definitions and writes a single `types.ts` per module

The generated TypeScript files are placed in each module's primary source project (e.g. `modules/Customers/src/SimpleModule.Customers/types.ts`):

```typescript
// Auto-generated from [Dto] types -- do not edit
export interface CreateCustomerRequest {
  name: string;
  email: string;
}

export interface Customer {
  id: number;
  name: string;
  email: string;
}

export interface UpdateCustomerRequest {
  name: string;
  email: string;
}
```

::: warning
These files are auto-generated. Do not edit them manually -- your changes will be overwritten on the next build.
:::

## The `[NoDtoGeneration]` Escape Hatch

Some public types in contracts assemblies should not have TypeScript interfaces generated. For example, event types or internal marker interfaces. Use `[NoDtoGeneration]` to exclude them:

```csharp
[NoDtoGeneration]
public sealed record InvoiceCreatedEvent(
    InvoiceId InvoiceId, CustomerId CustomerId, decimal Total) : IEvent;
```

The attribute can be applied to classes, structs, and interfaces:

```csharp
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
    AllowMultiple = false,
    Inherited = false)]
public sealed class NoDtoGenerationAttribute : Attribute { }
```

## Events in Contracts

Domain events are also defined in contracts projects so that handlers in other modules can subscribe to them:

```csharp
using SimpleModule.Core.Events;
using SimpleModule.Customers.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Invoices.Contracts.Events;

public sealed record InvoiceCreatedEvent(
    InvoiceId InvoiceId, CustomerId CustomerId, decimal Total) : IEvent;
```

Any module can handle `InvoiceCreatedEvent` by declaring a handler class with a `Handle` method — Wolverine discovers it by naming convention — without depending on the Invoices implementation.

## Summary

| Concept | Where It Lives | Purpose |
|---------|---------------|---------|
| `I<Name>Contracts` | Contracts project | Public interface for inter-module calls |
| DTO classes | Contracts project | Shared data shapes |
| Strongly-typed IDs | Contracts project | Type-safe identifiers (Vogen) |
| Events (`IEvent`) | Contracts project | Cross-module event definitions |
| `[Dto]` | On types outside contracts | Opt-in to TypeScript generation |
| `[NoDtoGeneration]` | On types inside contracts | Opt-out of TypeScript generation |

## Next Steps

- [Database](/guide/database) -- configure per-module database contexts and schema isolation
- [Events](/guide/events) -- publish and handle cross-module events
- [Type Generation](/advanced/type-generation) -- how DTOs become TypeScript interfaces
