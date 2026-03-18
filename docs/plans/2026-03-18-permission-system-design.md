# Permission System Design

**Date:** 2026-03-18
**Status:** Approved

## Overview

A module-scoped permission system for SimpleModule. Modules declare strongly-typed permissions via static constant classes, registered through a `ConfigurePermissions` method on `IModule`. Permissions can be assigned to both users and roles, stored in dedicated tables, and projected into claims at login for fast runtime checks.

## Design Decisions

| Aspect | Decision |
|--------|----------|
| Definition | Static class with `const string` fields per module |
| Registration | `ConfigurePermissions(PermissionRegistryBuilder)` on `IModule` |
| Checking | `[RequirePermission]` attribute (generator-wired) + inline `.RequirePermission()` escape hatch |
| Storage | Dedicated `UserPermissions` / `RolePermissions` tables |
| Runtime | Claims projection at login, claim-based checks at request time |
| Default policy | Deny by default — `[AllowAnonymous]` to opt out |
| Auto-generation | Generator emits permissions class for `CrudEndpoints<T>` modules |
| Admin override | Admin role bypasses all permission checks |
| Admin UI | Extended role/user edit pages with module-grouped permission checklists |

## Section 1: Permission Definition

Each module defines a static class with string constants. The module name is used as a prefix to prevent collisions.

```csharp
// modules/Products/src/Products/ProductsPermissions.cs
public static class ProductsPermissions
{
    public const string View = "Products.View";
    public const string Create = "Products.Create";
    public const string Update = "Products.Update";
    public const string Delete = "Products.Delete";
    // Custom permissions added manually:
    public const string Export = "Products.Export";
}
```

For modules using `CrudEndpoints<T>`, the **source generator auto-emits** the four CRUD permission constants (`View`, `Create`, `Update`, `Delete`). The developer can extend with additional custom permissions in a separate partial class or a new class.

The module registers its permissions in `ConfigurePermissions`:

```csharp
public class ProductsModule : IModule
{
    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<ProductsPermissions>();
    }
}
```

`AddPermissions<T>()` reflects over the static class at startup (one-time registration) and registers all `public const string` fields as known permissions.

## Section 2: Permission Checking on Endpoints

Two mechanisms, both supported:

### Attribute-based (common case)

```csharp
[RequirePermission(ProductsPermissions.Create)]
public class CreateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/products", HandleAsync);
    }
}
```

The source generator discovers `[RequirePermission]` on `IEndpoint` classes and emits `.RequireAuthorization(policy => policy.RequireClaim("permission", "Products.Create"))` in the generated `MapModuleEndpoints()` method.

### Inline (escape hatch)

```csharp
public class SpecialProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/products/special", HandleAsync)
            .RequirePermission(ProductsPermissions.Create, ProductsPermissions.Export);
    }
}
```

### AllowAnonymous opt-out

```csharp
[AllowAnonymous]
public class PublicProductListEndpoint : IEndpoint { ... }
```

The generator recognizes `[AllowAnonymous]` and skips adding `.RequireAuthorization()`. Without either attribute, the deny-by-default policy requires authentication but no specific permission.

### Multiple permissions

Multiple permissions on one endpoint are treated as **AND** (user must have all). For OR semantics, use inline with a custom policy.

## Section 3: Storage & Claims Projection

### Dedicated tables (Users module)

```
UserPermissions
├── UserId (FK → AspNetUsers.Id)
├── Permission (string, e.g., "Products.Create")
└── PK: (UserId, Permission)

RolePermissions
├── RoleId (FK → AspNetRoles.Id)
├── Permission (string, e.g., "Products.Create")
└── PK: (RoleId, Permission)
```

### Claims projection at login

When the OAuth2 token is issued (in `AuthorizationEndpoint`):

1. Load the user's direct permissions from `UserPermissions`
2. Load permissions from all the user's roles via `RolePermissions`
3. Merge and deduplicate
4. Add each as a claim: `type = "permission"`, `value = "Products.Create"`

Claims travel with the token/cookie — no database hits at request time.

### Authorization handler

A custom `PermissionAuthorizationHandler` checks for the required `"permission"` claim(s) on the `ClaimsPrincipal`.

### Admin override

Users with the `Admin` role bypass permission checks entirely (all permissions implicitly granted).

## Section 4: Source Generator Changes

### EndpointExtensionsEmitter updates

Discovers `[RequirePermission]` and `[AllowAnonymous]` on endpoint classes:

```csharp
// Generated output in MapModuleEndpoints():
endpoints.MapPost("/products", CreateProductEndpoint.HandleAsync)
    .RequireAuthorization(policy => policy.RequireClaim("permission", "Products.Create"));

endpoints.MapGet("/products/public", PublicProductListEndpoint.HandleAsync)
    .AllowAnonymous();

// No attribute → deny-by-default:
endpoints.MapGet("/products/internal", InternalEndpoint.HandleAsync)
    .RequireAuthorization();  // authenticated, no specific permission
```

### New PermissionsEmitter

Scans modules that use `CrudEndpoints<T>`. For each, emits:

```csharp
// <auto-generated>
namespace Products;

public static partial class ProductsPermissions
{
    public const string View = "Products.View";
    public const string Create = "Products.Create";
    public const string Update = "Products.Update";
    public const string Delete = "Products.Delete";
}
```

The class is `partial` so developers can extend it with custom permissions. The generator also auto-applies the correct `[RequirePermission]` to the CRUD endpoints it maps.

## Section 5: IModule Interface & Registration Flow

### IModule changes

```csharp
public interface IModule
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    void ConfigureEndpoints(IEndpointRouteBuilder builder) { }
    void ConfigurePermissions(PermissionRegistryBuilder builder) { }  // new
}
```

### Registration flow at startup

1. `AddModules()` called in `Program.cs`
2. For each module:
   - `module.ConfigureServices(services, config)`
   - `module.ConfigurePermissions(permissionRegistry)`
3. `PermissionRegistry` registered as a singleton
4. `AddAuthorization()` registers:
   - Default policy: `RequireAuthenticatedUser()`
   - `PermissionAuthorizationHandler`
5. At login (OAuth2 token issuance):
   - Load user + role permissions from DB
   - Merge, deduplicate, add as `"permission"` claims

### PermissionRegistryBuilder API

```csharp
public class PermissionRegistryBuilder
{
    public void AddPermissions<T>() where T : class;  // scans static string constants
    public void AddPermission(string permission);      // manual registration
}

public class PermissionRegistry
{
    public IReadOnlySet<string> AllPermissions { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ByModule { get; }  // "Products" → [...]
}
```

The `PermissionRegistry` is used by the admin UI to display available permissions — no hardcoded lists.

## Section 6: Admin UI for Permission Management

### Role permission management

- `GET /admin/roles/{id}/edit` — Extended to show permissions checklist grouped by module (from `PermissionRegistry`).
- `POST /admin/roles/{id}/permissions` — New endpoint. Saves selected permissions to `RolePermissions` table.

### User permission management

- `GET /admin/users/{id}/edit` — Extended to show:
  - Inherited permissions (from roles) — displayed read-only
  - Direct permissions — editable checklist
- `POST /admin/users/{id}/permissions` — New endpoint. Saves to `UserPermissions` table.

### Frontend

- React components receive `PermissionRegistry.ByModule` as props
- Permissions rendered as checkboxes grouped under module headings
- Inherited-from-role permissions shown as checked + disabled with tooltip showing which role grants them
- No new pages — everything fits into existing edit views for roles and users
