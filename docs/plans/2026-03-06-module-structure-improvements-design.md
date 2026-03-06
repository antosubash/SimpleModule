# Module Structure Improvements Design

## Overview

Improve the SimpleModule framework and example modules across six areas: Core SDK correctness, `IModule` interface ergonomics, explicit DTO marking, per-module contracts projects, feature-based internal structure, and generator accuracy.

---

## 1. Core Project SDK Fix

**Problem:** `SimpleModule.Core.csproj` uses `Microsoft.NET.Sdk.Web`, which is incorrect for a library.

**Change:** Switch to `Microsoft.NET.Sdk` with an explicit `FrameworkReference`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
```

---

## 2. `IModule` Interface — Default Implementations

**Problem:** Both `ConfigureServices` and `ConfigureEndpoints` are mandatory. Modules that only register services must implement an empty `ConfigureEndpoints`, and vice versa.

**Change:** Add `virtual` default no-op implementations so modules only override what they use:
```csharp
public interface IModule
{
    virtual void ConfigureServices(IServiceCollection services) { }
    virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
}
```

Modules must remain stateless — the generator may instantiate a module separately for `ConfigureServices` and `ConfigureEndpoints`.

---

## 3. Explicit `[Dto]` Attribute

**Problem:** The generator uses heuristic shape-detection to find DTO types (public class, no interfaces, not the module class). This is fragile and will pick up unintended types.

**Change:** Add a `[Dto]` attribute to `SimpleModule.Core`. Types must opt-in explicitly:
```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class DtoAttribute : Attribute { }
```

The generator replaces heuristic detection with `[Dto]` attribute lookup, matching the same pattern as `[Module]` discovery.

---

## 4. Per-Module Contracts Projects

**Problem:** `OrdersModule` directly references `IUserService` and `IProductService` from sibling module projects, breaking module isolation.

**Solution:** Each module gets a companion `<Name>.Contracts` project containing only the public contract interface and shared DTO types. Sibling modules reference `.Contracts` projects only — never the implementation project.

**Registration pattern:**
```csharp
// In UsersModule.ConfigureServices:
services.AddScoped<IUserContracts, UserService>();

// In OrderService constructor:
public OrderService(IUserContracts users, IProductContracts products) { ... }
```

---

## 5. Module Folder Grouping + Feature-Based Internal Structure

**Problem:** Implementation and contracts are flat siblings. Internally, each module is a single large file with no organisation.

**New layout:**
```
src/modules/
  Users/
    Users/                        <- implementation project
      Users.csproj
      UsersModule.cs
      Features/
        GetAllUsers/
          GetAllUsersEndpoint.cs
          GetAllUsersHandler.cs
        GetUserById/
          GetUserByIdEndpoint.cs
          GetUserByIdHandler.cs
    Users.Contracts/              <- contracts project (references Core only)
      Users.Contracts.csproj
      IUserContracts.cs
      User.cs                     <- [Dto] marked
  Products/
    Products/
      ...
    Products.Contracts/
      ...
  Orders/
    Orders/
      Orders.csproj               <- references Users.Contracts + Products.Contracts
      OrdersModule.cs
      Features/
        GetAllOrders/
        GetOrderById/
        CreateOrder/
    Orders.Contracts/
      ...
```

**Rules:**
- `.Contracts` projects reference only `SimpleModule.Core`
- Implementation projects reference their own `.Contracts` project + any sibling `.Contracts` projects needed
- Implementation projects never reference sibling implementation projects
- All DTO types in `.Contracts` are marked `[Dto]`

---

## 6. Generator Improvements

**DTO discovery:** Replace heuristic shape-detection with `[Dto]` attribute lookup.

**Conditional call emission:** Before emitting a `ConfigureServices` or `ConfigureEndpoints` call, check whether the module actually overrides that method. Skip the call if the module relies on the default no-op — avoids unnecessary instantiations.

---

## Affected Files

| File | Action |
|------|--------|
| `src/SimpleModule.Core/SimpleModule.Core.csproj` | Fix SDK |
| `src/SimpleModule.Core/IModule.cs` | Add default implementations |
| `src/SimpleModule.Core/DtoAttribute.cs` | New file |
| `src/SimpleModule.Generator/ModuleDiscovererGenerator.cs` | Replace DTO heuristics; add override check |
| `src/modules/Users/` | Restructure into `Users/Users/` + `Users/Users.Contracts/` |
| `src/modules/Products/` | Same restructure |
| `src/modules/Orders/` | Same restructure; remove sibling project references |
| `src/SimpleModule.Api/SimpleModule.Api.csproj` | Update project reference paths |
| `SimpleModule.sln` | Update project paths |
| `CLAUDE.md` | Update "Adding a New Module" steps |
