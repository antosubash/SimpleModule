---
outline: deep
---

# Permissions

SimpleModule includes a claims-based permission system that integrates with ASP.NET Core authorization. Each module defines its own permissions, registers them with the framework, and protects endpoints using attributes or extension methods.

## Overview

The permission system has three layers:

1. **Permission definitions** -- constants in a class implementing `IModulePermissions`
2. **Permission registry** -- collects all permissions at startup, grouped by module
3. **Authorization handler** -- checks `permission` claims on the current user

## Defining Permissions

Create a sealed class implementing `IModulePermissions` with `public const string` fields. The convention is `ModuleName.Action`:

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

::: tip Auto-Discovery
Classes implementing `IModulePermissions` are **automatically discovered** by the source generator. You do not need to register them manually -- the generator scans for all `IModulePermissions` implementations and registers their constants with the `PermissionRegistry`.
:::

The `PermissionRegistryBuilder` uses reflection to find all `public const string` fields in the class and groups them by the prefix before the first dot:

```csharp
// "Products.View" → grouped under "Products" module
// "Orders.Create" → grouped under "Orders" module
// "GlobalAdmin"   → grouped under "Global" (no dot prefix)
```

## Protecting Endpoints

### Extension Method (Recommended)

Use the `RequirePermission` extension method on endpoint builders:

```csharp
public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/",
                (CreateProductRequest request, IProductContracts productContracts) =>
                {
                    // ... handle request
                }
            )
            .RequirePermission(ProductsPermissions.Create);
}
```

Multiple permissions can be required on a single endpoint:

```csharp
app.MapDelete("/{id}", (int id, IProductContracts products) =>
        products.DeleteProductAsync(id)
    )
    .RequirePermission(ProductsPermissions.View, ProductsPermissions.Delete);
```

Each permission becomes a separate `PermissionRequirement`. The user must satisfy **all** of them.

### Attribute (for Source Generator Discovery)

The `[RequirePermission]` attribute can be applied to endpoint classes:

```csharp
[RequirePermission(ProductsPermissions.View)]
public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", (int id, IProductContracts products) =>
            products.GetProductByIdAsync(id)
        );
}
```

## How Authorization Works

### PermissionRequirement

Each permission string is wrapped in a `PermissionRequirement` implementing `IAuthorizationRequirement`:

```csharp
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
```

### PermissionAuthorizationHandler

The framework registers a `PermissionAuthorizationHandler` that checks for permission claims:

```csharp
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
    )
    {
        if (context.User.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

The handler delegates to `ClaimsPrincipalExtensions.HasPermission`, which is where the real policy lives:

- Users with the **Admin role** bypass all permission checks
- For other users, each `permission` claim is tested against the requirement via `PermissionMatcher.IsMatch`, which supports **wildcard matching**: a claim value of `"Products.*"` satisfies any `Products.X` requirement, and a bare `"*"` claim satisfies any requirement. Only trailing wildcards (`prefix.*` or `*`) are supported.
- If no matching claim is found, the requirement fails (returns 403 Forbidden)

### RequirePermission Extension

The `RequirePermission` extension method on `IEndpointConventionBuilder` composes ASP.NET Core's `RequireAuthorization` with permission requirements:

```csharp
public static TBuilder RequirePermission<TBuilder>(
    this TBuilder builder,
    params string[] permissions
)
    where TBuilder : IEndpointConventionBuilder
{
    return builder.RequireAuthorization(policy =>
    {
        policy.RequireAuthenticatedUser();
        foreach (var permission in permissions)
        {
            policy.AddRequirements(new PermissionRequirement(permission));
        }
    });
}
```

This ensures the user is **authenticated** and has the required **permission claims**.

## PermissionRegistry

The `PermissionRegistry` provides read-only access to all registered permissions at runtime:

```csharp
public sealed class PermissionRegistry
{
    public IReadOnlySet<string> AllPermissions { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ByModule { get; }
}
```

This is useful for building admin UIs that display and assign permissions. For example, a role management page can enumerate all permissions grouped by module:

```csharp
app.MapGet("/permissions", (PermissionRegistry registry) =>
{
    return registry.ByModule; // { "Products": ["Products.View", ...], "Orders": [...] }
});
```

## Assigning Permissions to Users

The permission system checks for `permission` claims on the `ClaimsPrincipal`. How those claims get there depends on your authentication setup:

- **Database-backed roles**: Store role-permission mappings, load as claims at login
- **OpenID Connect**: Include permissions in the ID token or userinfo response
- **Claims transformation**: Use an `IClaimsTransformation` to add permission claims dynamically

Example claims transformation:

```csharp
public class PermissionClaimsTransformation(IUserRoleService roleService)
    : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return principal;

        var permissions = await roleService.GetPermissionsForUserAsync(userId);
        var identity = new ClaimsIdentity();

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        principal.AddIdentity(identity);
        return principal;
    }
}
```

## Testing Permissions

The test infrastructure provides `CreateAuthenticatedClient` which accepts claims. Add permission claims to test protected endpoints:

```csharp
[Fact]
public async Task Create_product_requires_permission()
{
    var client = factory.CreateAuthenticatedClient(
        new Claim("permission", ProductsPermissions.Create)
    );

    var response = await client.PostAsJsonAsync("/products", new { Name = "Test" });

    response.StatusCode.Should().Be(HttpStatusCode.Created);
}

[Fact]
public async Task Create_product_forbidden_without_permission()
{
    var client = factory.CreateAuthenticatedClient(); // no permission claims

    var response = await client.PostAsJsonAsync("/products", new { Name = "Test" });

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

## Next Steps

- [Menus](/guide/menus) -- navigation menu system with auth-aware visibility
- [Settings](/guide/settings) -- module-scoped configurable settings
- [Integration Tests](/testing/integration-tests) -- test permission-protected endpoints end-to-end
