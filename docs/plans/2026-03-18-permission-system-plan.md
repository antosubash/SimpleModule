# Permission System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a module-scoped permission system with strongly-typed permissions, attribute-based endpoint protection, hybrid storage (dedicated tables + claims projection), deny-by-default policy, and admin permission seeding.

**Architecture:** Modules declare permissions via static constant classes registered through `ConfigurePermissions()` on `IModule`. The source generator discovers `[RequirePermission]` and `[AllowAnonymous]` on endpoints and emits authorization wiring. Permissions are stored in dedicated `UserPermission`/`RolePermission` tables and projected into claims at login. Admin role gets all permissions seeded automatically.

**Tech Stack:** ASP.NET Core Authorization, EF Core (SQLite/PostgreSQL), Roslyn IIncrementalGenerator (netstandard2.0), xUnit.v3 + FluentAssertions.

**Design doc:** `docs/plans/2026-03-18-permission-system-design.md`

---

### Task 1: Add `RequirePermissionAttribute` to Core

**Files:**
- Create: `framework/SimpleModule.Core/Authorization/RequirePermissionAttribute.cs`

**Step 1: Create the attribute**

```csharp
using System;

namespace SimpleModule.Core.Authorization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequirePermissionAttribute : Attribute
{
    public string[] Permissions { get; }

    public RequirePermissionAttribute(params string[] permissions)
    {
        Permissions = permissions;
    }
}
```

**Step 2: Verify it builds**

Run: `dotnet build framework/SimpleModule.Core`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add framework/SimpleModule.Core/Authorization/RequirePermissionAttribute.cs
git commit -m "feat: add RequirePermissionAttribute to Core"
```

---

### Task 2: Add `PermissionRegistryBuilder` and `PermissionRegistry` to Core

**Files:**
- Create: `framework/SimpleModule.Core/Authorization/PermissionRegistry.cs`
- Create: `framework/SimpleModule.Core/Authorization/PermissionRegistryBuilder.cs`

**Step 1: Create PermissionRegistry**

```csharp
using System.Collections.Generic;

namespace SimpleModule.Core.Authorization;

public sealed class PermissionRegistry
{
    public IReadOnlySet<string> AllPermissions { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ByModule { get; }

    internal PermissionRegistry(
        IReadOnlySet<string> allPermissions,
        IReadOnlyDictionary<string, IReadOnlyList<string>> byModule)
    {
        AllPermissions = allPermissions;
        ByModule = byModule;
    }
}
```

**Step 2: Create PermissionRegistryBuilder**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpleModule.Core.Authorization;

public sealed class PermissionRegistryBuilder
{
    private readonly Dictionary<string, List<string>> _byModule = new();

    public void AddPermissions<T>() where T : class
    {
        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

        foreach (var field in fields)
        {
            var value = (string)field.GetRawConstantValue()!;
            AddPermission(value);
        }
    }

    public void AddPermission(string permission)
    {
        var dotIndex = permission.IndexOf('.');
        var module = dotIndex >= 0 ? permission[..dotIndex] : "Global";

        if (!_byModule.TryGetValue(module, out var list))
        {
            list = new List<string>();
            _byModule[module] = list;
        }

        if (!list.Contains(permission))
        {
            list.Add(permission);
        }
    }

    public PermissionRegistry Build()
    {
        var all = new HashSet<string>();
        var byModule = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var (module, permissions) in _byModule)
        {
            byModule[module] = permissions.AsReadOnly();
            foreach (var p in permissions)
            {
                all.Add(p);
            }
        }

        return new PermissionRegistry(all, byModule);
    }
}
```

**Step 3: Verify it builds**

Run: `dotnet build framework/SimpleModule.Core`
Expected: SUCCESS

**Step 4: Commit**

```bash
git add framework/SimpleModule.Core/Authorization/PermissionRegistry.cs framework/SimpleModule.Core/Authorization/PermissionRegistryBuilder.cs
git commit -m "feat: add PermissionRegistryBuilder and PermissionRegistry"
```

---

### Task 3: Add `ConfigurePermissions` to `IModule`

**Files:**
- Modify: `framework/SimpleModule.Core/IModule.cs:8-13`

**Step 1: Add the method**

Add `using SimpleModule.Core.Authorization;` to the top and add the new virtual method to `IModule`:

```csharp
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;

namespace SimpleModule.Core;

public interface IModule
{
    virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
    virtual void ConfigureMenu(IMenuBuilder menus) { }
    virtual void ConfigurePermissions(PermissionRegistryBuilder builder) { }
}
```

**Step 2: Verify it builds**

Run: `dotnet build framework/SimpleModule.Core`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add framework/SimpleModule.Core/IModule.cs
git commit -m "feat: add ConfigurePermissions to IModule interface"
```

---

### Task 4: Add `RequirePermission` extension method for inline use

**Files:**
- Create: `framework/SimpleModule.Core/Authorization/EndpointPermissionExtensions.cs`

**Step 1: Create the extension method**

```csharp
using Microsoft.AspNetCore.Builder;

namespace SimpleModule.Core.Authorization;

public static class EndpointPermissionExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, params string[] permissions)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy =>
        {
            policy.RequireAuthenticatedUser();
            foreach (var permission in permissions)
            {
                policy.RequireClaim("permission", permission);
            }
        });
    }
}
```

**Step 2: Verify it builds**

Run: `dotnet build framework/SimpleModule.Core`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add framework/SimpleModule.Core/Authorization/EndpointPermissionExtensions.cs
git commit -m "feat: add RequirePermission extension method for inline use"
```

---

### Task 5: Add `PermissionAuthorizationHandler`

**Files:**
- Create: `framework/SimpleModule.Core/Authorization/PermissionAuthorizationHandler.cs`
- Create: `framework/SimpleModule.Core/Authorization/PermissionRequirement.cs`

**Step 1: Create the requirement**

```csharp
using Microsoft.AspNetCore.Authorization;

namespace SimpleModule.Core.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
```

**Step 2: Create the handler**

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SimpleModule.Core.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Admin role bypasses all permission checks
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

**Step 3: Verify it builds**

Run: `dotnet build framework/SimpleModule.Core`
Expected: SUCCESS

**Step 4: Commit**

```bash
git add framework/SimpleModule.Core/Authorization/PermissionAuthorizationHandler.cs framework/SimpleModule.Core/Authorization/PermissionRequirement.cs
git commit -m "feat: add PermissionAuthorizationHandler with admin bypass"
```

---

### Task 6: Update source generator — discover `[RequirePermission]` and `[AllowAnonymous]` on endpoints

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs:101` (EndpointInfoRecord)
- Modify: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs:204` (EndpointInfo mutable class)
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs:278-335` (FindEndpointTypes)

**Step 1: Extend `EndpointInfoRecord` with permission data**

In `DiscoveryData.cs`, replace:

```csharp
internal readonly record struct EndpointInfoRecord(string FullyQualifiedName);
```

With:

```csharp
internal readonly record struct EndpointInfoRecord(
    string FullyQualifiedName,
    ImmutableArray<string> RequiredPermissions,
    bool AllowAnonymous
)
{
    public bool Equals(EndpointInfoRecord other)
    {
        return FullyQualifiedName == other.FullyQualifiedName
            && AllowAnonymous == other.AllowAnonymous
            && RequiredPermissions.SequenceEqual(other.RequiredPermissions);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + AllowAnonymous.GetHashCode();
            foreach (var p in RequiredPermissions)
                hash = hash * 31 + p.GetHashCode();
            return hash;
        }
    }
}
```

**Step 2: Update the mutable `EndpointInfo` class**

Replace:

```csharp
internal sealed class EndpointInfo
{
    public string FullyQualifiedName { get; set; } = "";
}
```

With:

```csharp
internal sealed class EndpointInfo
{
    public string FullyQualifiedName { get; set; } = "";
    public List<string> RequiredPermissions { get; set; } = new();
    public bool AllowAnonymous { get; set; }
}
```

**Step 3: Update `EndpointInfoRecord` construction in `SymbolDiscovery.Extract`**

In `SymbolDiscovery.cs`, find the line (around line 167):

```csharp
m.Endpoints.Select(e => new EndpointInfoRecord(e.FullyQualifiedName))
```

Replace with:

```csharp
m.Endpoints.Select(e => new EndpointInfoRecord(
    e.FullyQualifiedName,
    e.RequiredPermissions.ToImmutableArray(),
    e.AllowAnonymous))
```

**Step 4: Update `FindEndpointTypes` to extract attributes**

In `SymbolDiscovery.cs`, in the `FindEndpointTypes` method, update the `IEndpoint` branch (around line 328-330). After creating the `EndpointInfo`, scan the type's attributes:

Replace:

```csharp
else if (ImplementsInterface(typeSymbol, endpointInterfaceSymbol))
{
    endpoints.Add(new EndpointInfo { FullyQualifiedName = fqn });
}
```

With:

```csharp
else if (ImplementsInterface(typeSymbol, endpointInterfaceSymbol))
{
    var info = new EndpointInfo { FullyQualifiedName = fqn };

    foreach (var attr in typeSymbol.GetAttributes())
    {
        var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (attrName == "global::SimpleModule.Core.Authorization.RequirePermissionAttribute")
        {
            // params string[] permissions — stored as a single array constructor argument
            if (attr.ConstructorArguments.Length > 0)
            {
                var arg = attr.ConstructorArguments[0];
                if (arg.Kind == TypedConstantKind.Array)
                {
                    foreach (var val in arg.Values)
                    {
                        if (val.Value is string s)
                            info.RequiredPermissions.Add(s);
                    }
                }
                else if (arg.Value is string single)
                {
                    info.RequiredPermissions.Add(single);
                }
            }
        }
        else if (attrName == "global::Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute")
        {
            info.AllowAnonymous = true;
        }
    }

    endpoints.Add(info);
}
```

**Step 5: Update `ModuleInfoRecord` equality to account for new `EndpointInfoRecord` fields**

The `ModuleInfoRecord.Equals` already calls `Endpoints.SequenceEqual(other.Endpoints)`, which will use the updated `EndpointInfoRecord.Equals`. No changes needed here.

**Step 6: Verify it builds**

Run: `dotnet build framework/SimpleModule.Generator`
Expected: SUCCESS

**Step 7: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/DiscoveryData.cs framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs
git commit -m "feat: generator discovers RequirePermission and AllowAnonymous on endpoints"
```

---

### Task 7: Update `EndpointExtensionsEmitter` to emit authorization wiring

**Files:**
- Modify: `framework/SimpleModule.Generator/Emitters/EndpointExtensionsEmitter.cs:14-58`

**Step 1: Update usings and endpoint mapping**

Add `using Microsoft.AspNetCore.Authorization;` to the generated output. Then update the endpoint registration loop to append auth calls.

In `EndpointExtensionsEmitter.cs`, update the generated code. The emitter currently emits:

```csharp
sb.AppendLine($"            new {endpoint.FullyQualifiedName}().Map(group);");
```

Change each endpoint mapping line to check for `AllowAnonymous` and `RequiredPermissions`:

For the route-prefix branch (lines 45-48), replace:

```csharp
foreach (var endpoint in module.Endpoints)
{
    sb.AppendLine($"            new {endpoint.FullyQualifiedName}().Map(group);");
}
```

With a helper method call. Add a private method to the emitter class:

```csharp
private static void EmitEndpointMapping(StringBuilder sb, EndpointInfoRecord endpoint, string target)
{
    sb.Append($"            new {endpoint.FullyQualifiedName}().Map({target});");

    // Note: The Map() method registers the route internally.
    // We cannot chain .AllowAnonymous() or .RequireAuthorization() on the Map() call
    // because Map() is void. Instead we need a different approach.
}
```

**IMPORTANT DESIGN CONSIDERATION:** The `IEndpoint.Map()` method returns `void`, so we cannot chain `.AllowAnonymous()` or `.RequireAuthorization()` on it. The emitter needs a different approach.

**Revised approach:** Instead of chaining on `Map()`, the emitter should generate a wrapper that:
1. Calls `endpoint.Map(group)` as before
2. After all endpoints in a module are mapped, applies auth policies to the group level

Actually, the better approach is to apply authorization at the **route group** level for the module, and let `[AllowAnonymous]` on individual endpoints override. Since we already have `var group = app.MapGroup(...)`, we can add `.RequireAuthorization()` to the group to enforce deny-by-default. Individual endpoints with `[AllowAnonymous]` will need to call `.AllowAnonymous()` inside their `Map()` method.

**Revised emitter changes:**

Update the route-prefix group creation line (line 43):

Replace:

```csharp
sb.AppendLine(
    $"            var group = app.MapGroup(\"{module.RoutePrefix}\").WithTags(\"{module.ModuleName}\");"
);
```

With:

```csharp
sb.AppendLine(
    $"            var group = app.MapGroup(\"{module.RoutePrefix}\").WithTags(\"{module.ModuleName}\").RequireAuthorization();"
);
```

This makes all auto-registered endpoints require authentication by default (deny-by-default). Endpoints with `[AllowAnonymous]` opt out inside their `Map()` call. Endpoints with `[RequirePermission]` need the permission check applied.

For `[RequirePermission]` — since `Map()` is void, the endpoint class itself should apply `.RequireAuthorization()` in its `Map()` method (inline approach). The attribute is still useful for the **source generator to auto-generate the `Map()` body for `CrudEndpoints`** (Task 9).

**However**, a better approach that doesn't change `IEndpoint.Map()`: make `Map()` return `RouteHandlerBuilder?` or modify the emitter to NOT use `Map()` for endpoints with permissions. Let's keep it simple:

**Final revised approach:** Add `.RequireAuthorization()` to the group (deny-by-default). The `[RequirePermission]` attribute is used by developers as documentation and by the CrudEndpoints generator (Task 9). For manually-written endpoints, developers use the inline `.RequirePermission()` extension inside their `Map()` method. This keeps `IEndpoint` unchanged and the emitter changes minimal.

So the only emitter change is adding `.RequireAuthorization()` to groups:

In `EndpointExtensionsEmitter.cs`:

Replace line 43:
```csharp
$"            var group = app.MapGroup(\"{module.RoutePrefix}\").WithTags(\"{module.ModuleName}\");"
```
With:
```csharp
$"            var group = app.MapGroup(\"{module.RoutePrefix}\").WithTags(\"{module.ModuleName}\").RequireAuthorization();"
```

Replace line 76 (view group):
```csharp
$"            var viewGroup = app.MapGroup(\"{module.ViewPrefix}\").WithTags(\"{module.ModuleName}\").ExcludeFromDescription();"
```
With:
```csharp
$"            var viewGroup = app.MapGroup(\"{module.ViewPrefix}\").WithTags(\"{module.ModuleName}\").ExcludeFromDescription().RequireAuthorization();"
```

Also add the `using Microsoft.AspNetCore.Authorization;` to the generated file (after line 18):
```csharp
sb.AppendLine("using Microsoft.AspNetCore.Authorization;");
```

**Step 2: Verify it builds**

Run: `dotnet build framework/SimpleModule.Generator`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add framework/SimpleModule.Generator/Emitters/EndpointExtensionsEmitter.cs
git commit -m "feat: emitter adds RequireAuthorization to endpoint groups (deny-by-default)"
```

---

### Task 8: Update `ModuleExtensionsEmitter` to call `ConfigurePermissions` and register services

**Files:**
- Modify: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs` (add `HasConfigurePermissions` to `ModuleInfoRecord` and `ModuleInfo`)
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs` (detect `ConfigurePermissions`)
- Modify: `framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs:37-66`

**Step 1: Add `HasConfigurePermissions` to data model**

In `DiscoveryData.cs`, add `bool HasConfigurePermissions` to `ModuleInfoRecord` (after `HasConfigureMenu`):

```csharp
internal readonly record struct ModuleInfoRecord(
    string FullyQualifiedName,
    string ModuleName,
    bool HasConfigureServices,
    bool HasConfigureEndpoints,
    bool HasConfigureMenu,
    bool HasConfigurePermissions,
    bool HasRazorComponents,
    string RoutePrefix,
    string ViewPrefix,
    ImmutableArray<EndpointInfoRecord> Endpoints,
    ImmutableArray<ViewInfoRecord> Views
)
```

Update `Equals` and `GetHashCode` to include `HasConfigurePermissions`.

In `ModuleInfo` mutable class, add:
```csharp
public bool HasConfigurePermissions { get; set; }
```

**Step 2: Detect `ConfigurePermissions` in `SymbolDiscovery`**

In `SymbolDiscovery.cs`, in the `FindModuleTypes` method (around line 266), add:

```csharp
HasConfigurePermissions = DeclaresMethod(typeSymbol, "ConfigurePermissions"),
```

Also update the `ModuleInfoRecord` construction (around line 158) to include:

```csharp
m.HasConfigurePermissions,
```

**Step 3: Update `ModuleExtensionsEmitter` to generate permission registration**

In `ModuleExtensionsEmitter.cs`, add authorization-related usings to the generated output (after line 21):

```csharp
sb.AppendLine("using Microsoft.AspNetCore.Authorization;");
sb.AppendLine("using SimpleModule.Core.Authorization;");
```

After the `ConfigureServices` calls (around line 47), add permission registration:

```csharp
// Permission registry
sb.AppendLine();
sb.AppendLine("        var permissionBuilder = new PermissionRegistryBuilder();");

foreach (var module in modules.Where(m => m.HasConfigurePermissions))
{
    var fieldName = TypeMappingHelpers.GetModuleFieldName(module.FullyQualifiedName);
    sb.AppendLine($"        {fieldName}.ConfigurePermissions(permissionBuilder);");
}

sb.AppendLine("        var permissionRegistry = permissionBuilder.Build();");
sb.AppendLine("        services.AddSingleton(permissionRegistry);");
sb.AppendLine();
sb.AppendLine("        services.AddAuthorizationBuilder()");
sb.AppendLine("            .SetDefaultPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());");
sb.AppendLine("        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();");
```

**Step 4: Verify it builds**

Run: `dotnet build framework/SimpleModule.Generator`
Expected: SUCCESS

**Step 5: Commit**

```bash
git add framework/SimpleModule.Generator/Discovery/DiscoveryData.cs framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs framework/SimpleModule.Generator/Emitters/ModuleExtensionsEmitter.cs
git commit -m "feat: generator calls ConfigurePermissions and registers authorization services"
```

---

### Task 9: Add permission entities and DbContext changes to Users module

**Files:**
- Create: `modules/Users/src/Users/Entities/UserPermission.cs`
- Create: `modules/Users/src/Users/Entities/RolePermission.cs`
- Modify: `modules/Users/src/Users/UsersDbContext.cs:1-20`

**Step 1: Create `UserPermission` entity**

```csharp
namespace SimpleModule.Users.Entities;

public class UserPermission
{
    public string UserId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }
}
```

**Step 2: Create `RolePermission` entity**

```csharp
namespace SimpleModule.Users.Entities;

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;

    public ApplicationRole? Role { get; set; }
}
```

**Step 3: Update `UsersDbContext`**

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users;

public class UsersDbContext(
    DbContextOptions<UsersDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.Permission });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.Permission });
            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.ApplyModuleSchema("Users", dbOptions.Value);
    }
}
```

**Step 4: Verify it builds**

Run: `dotnet build modules/Users/src/Users`
Expected: SUCCESS

**Step 5: Commit**

```bash
git add modules/Users/src/Users/Entities/UserPermission.cs modules/Users/src/Users/Entities/RolePermission.cs modules/Users/src/Users/UsersDbContext.cs
git commit -m "feat: add UserPermission and RolePermission entities with DbContext config"
```

---

### Task 10: Add permission claims projection to `AuthorizationEndpoint`

**Files:**
- Modify: `modules/Users/src/Users/Endpoints/Connect/AuthorizationEndpoint.cs:69-94`

**Step 1: Add permission claims loading**

After the roles are added (line 80), add permission loading:

```csharp
// Load permissions from user's roles
var dbContext = context.RequestServices.GetRequiredService<UsersDbContext>();

var roleIds = await dbContext.Roles
    .Where(r => roles.Contains(r.Name!))
    .Select(r => r.Id)
    .ToListAsync();

var rolePermissions = await dbContext.RolePermissions
    .Where(rp => roleIds.Contains(rp.RoleId))
    .Select(rp => rp.Permission)
    .ToListAsync();

// Load direct user permissions
var userId = await userManager.GetUserIdAsync(user);
var userPermissions = await dbContext.UserPermissions
    .Where(up => up.UserId == userId)
    .Select(up => up.Permission)
    .ToListAsync();

// Merge and deduplicate, add as claims
var allPermissions = new HashSet<string>(rolePermissions);
foreach (var p in userPermissions)
    allPermissions.Add(p);

foreach (var permission in allPermissions)
{
    identity.AddClaim("permission", permission);
}
```

Add required usings at top of file:
```csharp
using Microsoft.EntityFrameworkCore;
```

**Step 2: Update `GetDestinations` to include permission claims**

Add a case for the `"permission"` claim type:

```csharp
case "permission":
    yield return Destinations.AccessToken;
    yield break;
```

**Step 3: Verify it builds**

Run: `dotnet build modules/Users/src/Users`
Expected: SUCCESS

**Step 4: Commit**

```bash
git add modules/Users/src/Users/Endpoints/Connect/AuthorizationEndpoint.cs
git commit -m "feat: project permissions into claims at OAuth2 token issuance"
```

---

### Task 11: Add `ProductsPermissions` static class and register on module

**Files:**
- Create: `modules/Products/src/Products/ProductsPermissions.cs`
- Modify: `modules/Products/src/Products/ProductsModule.cs:15-21`

**Step 1: Create the permissions class**

```csharp
namespace SimpleModule.Products;

public static class ProductsPermissions
{
    public const string View = "Products.View";
    public const string Create = "Products.Create";
    public const string Update = "Products.Update";
    public const string Delete = "Products.Delete";
}
```

**Step 2: Add `ConfigurePermissions` to `ProductsModule`**

Add using and method:

```csharp
using SimpleModule.Core.Authorization;
```

Add method to `ProductsModule` class:

```csharp
public void ConfigurePermissions(PermissionRegistryBuilder builder)
{
    builder.AddPermissions<ProductsPermissions>();
}
```

**Step 3: Add `[RequirePermission]` to Product endpoints**

Update each endpoint class in `modules/Products/src/Products/Endpoints/Products/`:

- `GetAllEndpoint.cs`: Add `[RequirePermission(ProductsPermissions.View)]`
- `GetByIdEndpoint.cs`: Add `[RequirePermission(ProductsPermissions.View)]`
- `CreateEndpoint.cs`: Add `[RequirePermission(ProductsPermissions.Create)]`
- `UpdateEndpoint.cs`: Add `[RequirePermission(ProductsPermissions.Update)]`
- `DeleteEndpoint.cs`: Add `[RequirePermission(ProductsPermissions.Delete)]`

Each file needs `using SimpleModule.Core.Authorization;` and the attribute on the class. Then inside the `Map()` method, add the inline permission check. Example for `GetAllEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

[RequirePermission(ProductsPermissions.View)]
public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", (IProductContracts productContracts) =>
            CrudEndpoints.GetAll(productContracts.GetAllProductsAsync))
            .RequirePermission(ProductsPermissions.View);
}
```

**Step 4: Verify it builds**

Run: `dotnet build modules/Products/src/Products`
Expected: SUCCESS

**Step 5: Commit**

```bash
git add modules/Products/src/Products/ProductsPermissions.cs modules/Products/src/Products/ProductsModule.cs modules/Products/src/Products/Endpoints/Products/
git commit -m "feat: add ProductsPermissions and protect Product endpoints"
```

---

### Task 12: Add `OrdersPermissions` and protect Orders endpoints

**Files:**
- Create: `modules/Orders/src/Orders/OrdersPermissions.cs`
- Modify: `modules/Orders/src/Orders/OrdersModule.cs`
- Modify: All Orders endpoint files

Follow the same pattern as Task 11. Create `OrdersPermissions` with `View`, `Create`, `Update`, `Delete` constants. Add `ConfigurePermissions` to `OrdersModule`. Add `[RequirePermission]` attributes and inline `.RequirePermission()` calls to all Orders endpoints.

**Step 1: Create permissions, update module, protect endpoints**

**Step 2: Verify it builds**

Run: `dotnet build modules/Orders/src/Orders`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add modules/Orders/src/Orders/OrdersPermissions.cs modules/Orders/src/Orders/OrdersModule.cs modules/Orders/src/Orders/Endpoints/
git commit -m "feat: add OrdersPermissions and protect Orders endpoints"
```

---

### Task 13: Mark public endpoints with `[AllowAnonymous]`

**Files:**
- Modify: Connect endpoints in Users module (AuthorizationEndpoint, TokenEndpoint, EndSessionEndpoint, UserInfoEndpoint)
- Modify: Any view endpoints that should be public (login page, etc.)
- Modify: Identity/Account endpoints

**Step 1: Add `[AllowAnonymous]` to OAuth2 connect endpoints**

These endpoints handle auth flow itself and must remain public. Add `[Microsoft.AspNetCore.Authorization.AllowAnonymous]` attribute to:
- `AuthorizationEndpoint`
- `TokenEndpoint`
- `EndSessionEndpoint`
- `UserInfoEndpoint`

And inside their `Map()` methods, chain `.AllowAnonymous()` on the route:

```csharp
app.MapMethods(ConnectRouteConstants.ConnectAuthorize, ...)
    .ExcludeFromDescription()
    .AllowAnonymous();
```

**Step 2: Add `[AllowAnonymous]` to public view endpoints**

Any browse/landing pages that should be accessible without login. Check each `IViewEndpoint` and decide based on the current `RequiresAuth` menu property.

**Step 3: Verify it builds**

Run: `dotnet build`
Expected: SUCCESS

**Step 4: Commit**

```bash
git add modules/Users/src/Users/Endpoints/
git commit -m "feat: mark public endpoints with AllowAnonymous"
```

---

### Task 14: Seed all permissions for Admin role

**Files:**
- Create: `modules/Users/src/Users/Services/PermissionSeedService.cs`
- Modify: `modules/Users/src/Users/UsersModule.cs:114` (register the service)

**Step 1: Create the seed service**

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Authorization;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Services;

public partial class PermissionSeedService(
    IServiceProvider serviceProvider,
    PermissionRegistry permissionRegistry,
    ILogger<PermissionSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        var adminRole = await roleManager.FindByNameAsync(SeedConstants.AdminRole);
        if (adminRole is null)
            return;

        var existingPermissions = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existingPermissions);
        var newPermissions = new List<RolePermission>();

        foreach (var permission in permissionRegistry.AllPermissions)
        {
            if (!existingSet.Contains(permission))
            {
                newPermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    Permission = permission,
                });
            }
        }

        if (newPermissions.Count > 0)
        {
            LogSeedingPermissions(logger, newPermissions.Count);
            dbContext.RolePermissions.AddRange(newPermissions);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Seeding {Count} permissions for Admin role..."
    )]
    private static partial void LogSeedingPermissions(ILogger logger, int count);
}
```

**Step 2: Register in `UsersModule.ConfigureServices`**

After `services.AddHostedService<OpenIddictSeedService>();` (line 115), add:

```csharp
services.AddHostedService<PermissionSeedService>();
```

**Step 3: Verify it builds**

Run: `dotnet build modules/Users/src/Users`
Expected: SUCCESS

**Step 4: Commit**

```bash
git add modules/Users/src/Users/Services/PermissionSeedService.cs modules/Users/src/Users/UsersModule.cs
git commit -m "feat: seed all registered permissions for Admin role on startup"
```

---

### Task 15: Update `Program.cs` — remove duplicate `AddAuthorization` call

**Files:**
- Modify: `template/SimpleModule.Host/Program.cs:126`

**Step 1: Remove the standalone `AddAuthorization` call**

The generated `AddModules()` now registers authorization with the correct default policy. Remove line 126:

```csharp
builder.Services.AddAuthorization();
```

This prevents the generated default policy from being overwritten by a bare `AddAuthorization()` call.

**Step 2: Verify it builds and runs**

Run: `dotnet build && dotnet run --project template/SimpleModule.Host --launch-profile https`
Expected: App starts, endpoints require auth by default

**Step 3: Commit**

```bash
git add template/SimpleModule.Host/Program.cs
git commit -m "fix: remove duplicate AddAuthorization, use generator-configured policy"
```

---

### Task 16: Update test infrastructure for permissions

**Files:**
- Modify: `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs`

**Step 1: Update `CreateAuthenticatedClient` to support permission claims**

Add a convenience overload:

```csharp
public HttpClient CreateAuthenticatedClient(string[] permissions, params Claim[] additionalClaims)
{
    var claims = new List<Claim>(additionalClaims);
    foreach (var permission in permissions)
    {
        claims.Add(new Claim("permission", permission));
    }
    return CreateAuthenticatedClient(claims.ToArray());
}
```

**Step 2: Verify it builds**

Run: `dotnet build tests/SimpleModule.Tests.Shared`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs
git commit -m "feat: add permission-aware test client helper"
```

---

### Task 17: Write tests for permission system

**Files:**
- Create: `tests/SimpleModule.Core.Tests/Authorization/PermissionRegistryBuilderTests.cs`
- Create: `tests/SimpleModule.Core.Tests/Authorization/PermissionAuthorizationHandlerTests.cs`
- Modify: `modules/Products/tests/Products.Tests/Integration/` (update existing tests for auth)

**Step 1: Write `PermissionRegistryBuilderTests`**

```csharp
using FluentAssertions;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Core.Tests.Authorization;

public class PermissionRegistryBuilderTests
{
    private static class TestPermissions
    {
        public const string View = "Test.View";
        public const string Create = "Test.Create";
    }

    [Fact]
    public void AddPermissions_ScansStaticConstants()
    {
        var builder = new PermissionRegistryBuilder();
        builder.AddPermissions<TestPermissions>();
        var registry = builder.Build();

        registry.AllPermissions.Should().Contain("Test.View");
        registry.AllPermissions.Should().Contain("Test.Create");
    }

    [Fact]
    public void Build_GroupsByModule()
    {
        var builder = new PermissionRegistryBuilder();
        builder.AddPermission("Products.View");
        builder.AddPermission("Products.Create");
        builder.AddPermission("Orders.View");
        var registry = builder.Build();

        registry.ByModule.Should().ContainKey("Products");
        registry.ByModule["Products"].Should().HaveCount(2);
        registry.ByModule.Should().ContainKey("Orders");
        registry.ByModule["Orders"].Should().HaveCount(1);
    }

    [Fact]
    public void AddPermission_DeduplicatesValues()
    {
        var builder = new PermissionRegistryBuilder();
        builder.AddPermission("Products.View");
        builder.AddPermission("Products.View");
        var registry = builder.Build();

        registry.AllPermissions.Should().HaveCount(1);
    }
}
```

**Step 2: Write `PermissionAuthorizationHandlerTests`**

```csharp
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Core.Tests.Authorization;

public class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Handle_UserWithPermissionClaim_Succeeds()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Products.View");
        var user = CreateUser(new Claim("permission", "Products.View"));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserWithoutPermissionClaim_Fails()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Products.View");
        var user = CreateUser(new Claim("permission", "Orders.View"));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AdminRole_BypassesPermissionCheck()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Products.Delete");
        var user = CreateUser(new Claim(ClaimTypes.Role, "Admin"));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    private static ClaimsPrincipal CreateUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }
}
```

**Step 3: Update Products integration tests**

The existing product endpoint tests need to pass permission claims. Update tests to use `CreateAuthenticatedClient` with the appropriate permissions. Tests should verify:
- Unauthenticated → 401
- Authenticated without permission → 403
- Authenticated with permission → 200

**Step 4: Run all tests**

Run: `dotnet test`
Expected: ALL PASS

**Step 5: Commit**

```bash
git add tests/SimpleModule.Core.Tests/Authorization/ modules/Products/tests/Products.Tests/
git commit -m "test: add permission system unit and integration tests"
```

---

### Task 18: Full integration test — build and run

**Step 1: Clean build**

Run: `dotnet build`
Expected: SUCCESS with no errors

**Step 2: Run all tests**

Run: `dotnet test`
Expected: ALL PASS

**Step 3: Manual smoke test**

Run: `dotnet run --project template/SimpleModule.Host --launch-profile https`
Expected:
- App starts on https://localhost:5001
- Swagger UI loads
- Unauthenticated API calls to `/api/products` return 401
- Admin user can access all endpoints after login
- Check logs for "Seeding X permissions for Admin role..."

**Step 4: Final commit if any fixes needed**
