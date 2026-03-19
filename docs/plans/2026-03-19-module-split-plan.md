# OpenIddict & Permissions Module Split — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Extract OpenIddict and permissions into two new modules, slimming the Users module to identity-only concerns.

**Architecture:** Create `modules/Permissions/` (owns permission entities + queries, no dependencies) and `modules/OpenIddict/` (owns OAuth2/OIDC, depends on Users.Contracts + Permissions.Contracts). Users module keeps only Identity entities, UserService, and ConsoleEmailSender. Admin module switches from `UsersDbContext` to `IPermissionContracts` for permission operations.

**Tech Stack:** .NET 10, EF Core, OpenIddict, xUnit.v3, FluentAssertions, SQLite in-memory for tests.

---

### Task 1: Create Permissions.Contracts Project

**Files:**
- Create: `modules/Permissions/src/Permissions.Contracts/Permissions.Contracts.csproj`
- Create: `modules/Permissions/src/Permissions.Contracts/IPermissionContracts.cs`
- Create: `modules/Permissions/src/Permissions.Contracts/PermissionsConstants.cs`

**Step 1: Create csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Create IPermissionContracts**

```csharp
namespace SimpleModule.Permissions.Contracts;

public interface IPermissionContracts
{
    Task<IReadOnlySet<string>> GetPermissionsForUserAsync(string userId);
    Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(string roleId);
    Task SetPermissionsForUserAsync(string userId, IEnumerable<string> permissions);
    Task SetPermissionsForRoleAsync(string roleId, IEnumerable<string> permissions);
}
```

**Step 3: Create PermissionsConstants**

```csharp
namespace SimpleModule.Permissions.Contracts;

public static class PermissionsConstants
{
    public const string ModuleName = "Permissions";
}
```

**Step 4: Add to solution**

```bash
dotnet slnx add modules/Permissions/src/Permissions.Contracts/Permissions.Contracts.csproj --solution-folder /modules/Permissions/
```

**Step 5: Build to verify**

Run: `dotnet build modules/Permissions/src/Permissions.Contracts/Permissions.Contracts.csproj`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add modules/Permissions/src/Permissions.Contracts/ SimpleModule.slnx
git commit -m "feat(permissions): add Permissions.Contracts project with IPermissionContracts"
```

---

### Task 2: Create Permissions Module Implementation

**Files:**
- Create: `modules/Permissions/src/Permissions/Permissions.csproj`
- Create: `modules/Permissions/src/Permissions/PermissionsModule.cs`
- Create: `modules/Permissions/src/Permissions/PermissionsDbContext.cs`
- Create: `modules/Permissions/src/Permissions/Entities/UserPermission.cs`
- Create: `modules/Permissions/src/Permissions/Entities/RolePermission.cs`
- Create: `modules/Permissions/src/Permissions/PermissionService.cs`
- Create: `modules/Permissions/src/Permissions/Services/PermissionSeedService.cs`

**Step 1: Create csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Database\SimpleModule.Database.csproj" />
    <ProjectReference Include="..\Permissions.Contracts\Permissions.Contracts.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Create entities**

`Entities/UserPermission.cs`:
```csharp
namespace SimpleModule.Permissions.Entities;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class UserPermission
#pragma warning restore CA1711
{
    public string UserId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
}
```

`Entities/RolePermission.cs`:
```csharp
namespace SimpleModule.Permissions.Entities;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class RolePermission
#pragma warning restore CA1711
{
    public string RoleId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
}
```

Note: Navigation properties to `ApplicationUser`/`ApplicationRole` are removed — these entities now use plain string IDs with no FK relationship to Identity tables.

**Step 3: Create PermissionsDbContext**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Permissions.Entities;

namespace SimpleModule.Permissions;

public class PermissionsDbContext(
    DbContextOptions<PermissionsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.Permission });
            entity.ToTable("UserPermissions");
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.Permission });
            entity.ToTable("RolePermissions");
        });

        builder.ApplyModuleSchema("Permissions", dbOptions.Value);
    }
}
```

**Step 4: Create PermissionService**

```csharp
using Microsoft.EntityFrameworkCore;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Permissions.Entities;

namespace SimpleModule.Permissions;

public class PermissionService(PermissionsDbContext db) : IPermissionContracts
{
    public async Task<IReadOnlySet<string>> GetPermissionsForUserAsync(string userId)
    {
        var userPerms = await db.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();

        return new HashSet<string>(userPerms);
    }

    public async Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(string roleId)
    {
        var rolePerms = await db.RolePermissions
            .Where(p => p.RoleId == roleId)
            .Select(p => p.Permission)
            .ToListAsync();

        return new HashSet<string>(rolePerms);
    }

    public async Task SetPermissionsForUserAsync(string userId, IEnumerable<string> permissions)
    {
        var existing = await db.UserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync();

        db.UserPermissions.RemoveRange(existing);

        foreach (var permission in permissions)
        {
            db.UserPermissions.Add(new UserPermission { UserId = userId, Permission = permission });
        }

        await db.SaveChangesAsync();
    }

    public async Task SetPermissionsForRoleAsync(string roleId, IEnumerable<string> permissions)
    {
        var existing = await db.RolePermissions
            .Where(p => p.RoleId == roleId)
            .ToListAsync();

        db.RolePermissions.RemoveRange(existing);

        foreach (var permission in permissions)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, Permission = permission });
        }

        await db.SaveChangesAsync();
    }
}
```

**Step 5: Create PermissionSeedService**

Move from `SimpleModule.Users.Services.PermissionSeedService` with adaptations:

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Authorization;
using SimpleModule.Permissions.Entities;

namespace SimpleModule.Permissions.Services;

public partial class PermissionSeedService(
    IServiceProvider serviceProvider,
    PermissionRegistry permissionRegistry,
    ILogger<PermissionSeedService> logger
) : IHostedService
{
    private const string AdminRole = "Admin";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PermissionsDbContext>();

        // Look up Admin role ID from Identity's role store
        var roleManager = scope.ServiceProvider.GetRequiredService<IRoleStore<IdentityRole>>();

        // We need to find the Admin role. Since we don't depend on Users module,
        // we query the RolePermissions table — if no Admin role permissions exist yet,
        // we need the role ID from Identity. Use a dynamic lookup via IServiceProvider.
        var adminRoleId = await FindAdminRoleIdAsync(scope);
        if (adminRoleId is null)
            return;

        var existingPermissions = await dbContext
            .RolePermissions.Where(rp => rp.RoleId == adminRoleId)
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existingPermissions);
        var newPermissions = new List<RolePermission>();

        foreach (var permission in permissionRegistry.AllPermissions)
        {
            if (!existingSet.Contains(permission))
            {
                newPermissions.Add(
                    new RolePermission { RoleId = adminRoleId, Permission = permission }
                );
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

    private static async Task<string?> FindAdminRoleIdAsync(IServiceScope scope)
    {
        // Use RoleManager<IdentityRole> if available, otherwise try the generic version.
        // This avoids a hard dependency on Users module's ApplicationRole type.
        try
        {
            var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>();
            if (roleManager is not null)
            {
                var role = await roleManager.FindByNameAsync(AdminRole);
                return role?.Id;
            }
        }
        catch (InvalidOperationException)
        {
            // RoleManager not registered with IdentityRole — try dynamic approach
        }

        return null;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Seeding {Count} permissions for Admin role..."
    )]
    private static partial void LogSeedingPermissions(ILogger logger, int count);
}
```

Note: The seed service has a challenge — it needs the Admin role ID but can't depend on `ApplicationRole`. We'll address this in Task 7 by having it resolve `RoleManager` dynamically through the DI container (ASP.NET Identity registers `RoleManager<ApplicationRole>` which is also resolvable as its base type). If this doesn't work cleanly, an alternative is to add a `GetRoleIdByNameAsync(string name)` method to `IUserContracts` and have Permissions depend on Users.Contracts.

**Step 6: Create PermissionsModule**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Database;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Permissions.Services;

namespace SimpleModule.Permissions;

[Module(PermissionsConstants.ModuleName)]
public class PermissionsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<PermissionsDbContext>(
            configuration,
            PermissionsConstants.ModuleName
        );

        services.AddScoped<IPermissionContracts, PermissionService>();
        services.AddHostedService<PermissionSeedService>();
    }
}
```

**Step 7: Add to solution**

```bash
dotnet slnx add modules/Permissions/src/Permissions/Permissions.csproj --solution-folder /modules/Permissions/
```

**Step 8: Build to verify**

Run: `dotnet build modules/Permissions/src/Permissions/Permissions.csproj`
Expected: Build succeeded

**Step 9: Commit**

```bash
git add modules/Permissions/ SimpleModule.slnx
git commit -m "feat(permissions): add Permissions module with DbContext, service, and seed"
```

---

### Task 3: Create OpenIddict.Contracts Project

**Files:**
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/OpenIddict.Contracts.csproj`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/OpenIddictConstants.cs`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/AuthConstants.cs`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/ConnectRouteConstants.cs`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/ClientConstants.cs`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/ConfigKeys.cs`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/SeedConstants.cs`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/AuthErrorMessages.cs`

**Step 1: Create csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Move constants from Users.Contracts to OpenIddict.Contracts**

Move all constant classes from `modules/Users/src/Users.Contracts/Constants/` to `modules/OpenIddict/src/OpenIddict.Contracts/`, changing the namespace from `SimpleModule.Users.Constants` to `SimpleModule.OpenIddict.Contracts`:

- `AuthConstants.cs` → namespace `SimpleModule.OpenIddict.Contracts`
- `ConnectRouteConstants.cs` → namespace `SimpleModule.OpenIddict.Contracts`
- `ClientConstants.cs` → namespace `SimpleModule.OpenIddict.Contracts`
- `ConfigKeys.cs` → namespace `SimpleModule.OpenIddict.Contracts` (split: keep `SeedAdminPassword` in Users, move OpenIddict keys)
- `SeedConstants.cs` → namespace `SimpleModule.OpenIddict.Contracts` (split: keep user seed constants in Users, move client seed constants if any)
- `AuthErrorMessages.cs` → namespace `SimpleModule.OpenIddict.Contracts`

Create `OpenIddictConstants.cs` (the module name constant):
```csharp
namespace SimpleModule.OpenIddict.Contracts;

public static class OpenIddictModuleConstants
{
    public const string ModuleName = "OpenIddict";
}
```

Note: `SeedConstants` and `ConfigKeys` need splitting. `SeedConstants.AdminRole/AdminEmail/AdminDisplayName/DefaultAdminPassword` stay in Users (or move to a shared Users constant). `ConfigKeys.SeedAdminPassword` stays in Users. All `OpenIddict*` config keys move to OpenIddict.

**Step 3: Add to solution**

```bash
dotnet slnx add modules/OpenIddict/src/OpenIddict.Contracts/OpenIddict.Contracts.csproj --solution-folder /modules/OpenIddict/
```

**Step 4: Build to verify**

Run: `dotnet build modules/OpenIddict/src/OpenIddict.Contracts/OpenIddict.Contracts.csproj`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add modules/OpenIddict/ SimpleModule.slnx
git commit -m "feat(openiddict): add OpenIddict.Contracts project with auth constants"
```

---

### Task 4: Create OpenIddict Module Implementation

**Files:**
- Create: `modules/OpenIddict/src/OpenIddict/OpenIddict.csproj`
- Create: `modules/OpenIddict/src/OpenIddict/OpenIddictModule.cs`
- Create: `modules/OpenIddict/src/OpenIddict/OpenIddictAppDbContext.cs`
- Create: `modules/OpenIddict/src/OpenIddict/Services/OpenIddictSeedService.cs`
- Create: `modules/OpenIddict/src/OpenIddict/Endpoints/Connect/AuthorizationEndpoint.cs`
- Create: `modules/OpenIddict/src/OpenIddict/Endpoints/Connect/LogoutEndpoint.cs`
- Create: `modules/OpenIddict/src/OpenIddict/Endpoints/Connect/UserinfoEndpoint.cs`

**Step 1: Create csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
    <PackageReference Include="OpenIddict.AspNetCore" />
    <PackageReference Include="OpenIddict.EntityFrameworkCore" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Database\SimpleModule.Database.csproj" />
    <ProjectReference Include="..\OpenIddict.Contracts\OpenIddict.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\modules\Users\src\Users.Contracts\Users.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\modules\Permissions\src\Permissions.Contracts\Permissions.Contracts.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Create OpenIddictAppDbContext**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;

namespace SimpleModule.OpenIddict;

public class OpenIddictAppDbContext(
    DbContextOptions<OpenIddictAppDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyModuleSchema("OpenIddict", dbOptions.Value);
    }
}
```

**Step 3: Create OpenIddictModule**

Move OpenIddict configuration from `UsersModule.ConfigureServices`. The module registers:
- `OpenIddictAppDbContext` with `UseOpenIddict()`
- OpenIddict server options (authorization code flow, PKCE, endpoints, certs, scopes)
- OpenIddict validation
- `OpenIddictSeedService`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Database;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.OpenIddict.Services;

namespace SimpleModule.OpenIddict;

[Module(OpenIddictModuleConstants.ModuleName)]
public class OpenIddictModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<OpenIddictAppDbContext>(
            configuration,
            OpenIddictModuleConstants.ModuleName,
            opts => opts.UseOpenIddict()
        );

        services
            .AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<OpenIddictAppDbContext>();
            })
            .AddServer(options =>
            {
                options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
                options.AllowRefreshTokenFlow();

                options
                    .SetAuthorizationEndpointUris(ConnectRouteConstants.ConnectAuthorize)
                    .SetTokenEndpointUris(ConnectRouteConstants.ConnectToken)
                    .SetEndSessionEndpointUris(ConnectRouteConstants.ConnectEndSession)
                    .SetUserInfoEndpointUris(ConnectRouteConstants.ConnectUserInfo);

                var encryptionCertPath = configuration[ConfigKeys.OpenIddictEncryptionCertPath];
                var signingCertPath = configuration[ConfigKeys.OpenIddictSigningCertPath];

                if (
                    !string.IsNullOrEmpty(encryptionCertPath)
                    && !string.IsNullOrEmpty(signingCertPath)
                )
                {
                    var certPassword = configuration[ConfigKeys.OpenIddictCertPassword];
                    options.AddEncryptionCertificate(
                        System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
                            encryptionCertPath,
                            certPassword
                        )
                    );
                    options.AddSigningCertificate(
                        System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
                            signingCertPath,
                            certPassword
                        )
                    );
                }
                else
                {
                    options
                        .AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }

                options.RegisterScopes(
                    AuthConstants.OpenIdScope,
                    AuthConstants.ProfileScope,
                    AuthConstants.EmailScope,
                    AuthConstants.RolesScope
                );

                options
                    .UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddHostedService<OpenIddictSeedService>();
    }
}
```

**Step 4: Move Connect endpoints**

Move the 3 Connect endpoints from `modules/Users/src/Users/Endpoints/Connect/` to `modules/OpenIddict/src/OpenIddict/Endpoints/Connect/`, updating:
- Namespace: `SimpleModule.OpenIddict.Endpoints.Connect`
- `AuthorizationEndpoint`: Replace direct `UsersDbContext` permission query with `IPermissionContracts.GetPermissionsForUserAsync()`. Also needs to resolve role IDs — add a helper or get roles from `UserManager` claims and use `IPermissionContracts.GetPermissionsForRoleAsync()` per role. Since the endpoint already has `UserManager<ApplicationUser>`, it can get roles, then for each role look up the role entity to get its ID. But we don't have access to `ApplicationRole` from here...

The cleaner approach: The `AuthorizationEndpoint` currently queries permission tables directly. After the split, it should call `IPermissionContracts.GetPermissionsForUserAsync(userId)` which returns ALL permissions (user + role). But `IPermissionContracts` currently doesn't know about role membership — it only has role ID → permissions.

**Revised approach for AuthorizationEndpoint**: We need `IPermissionContracts` to have a method that takes a user ID and a set of role IDs, returning combined permissions. Or simpler: add a method `GetAllPermissionsForUserAsync(string userId, IEnumerable<string> roleIds)` to `IPermissionContracts`.

Actually, let's keep it simpler. The `AuthorizationEndpoint` already has `UserManager<ApplicationUser>` which gives it roles. It can:
1. Get role names from `UserManager.GetRolesAsync(user)`
2. It needs role IDs to query permissions. But role IDs aren't available without the `UsersDbContext`.

**Best solution**: Add `GetRoleIdByNameAsync` to `IUserContracts`, OR have `IPermissionContracts` accept role names (not IDs). Let's change the permission tables to use role name instead of role ID — role names are unique in Identity and this avoids the cross-module lookup entirely.

**REVISED DESIGN DECISION**: `RolePermission.RoleId` → `RolePermission.RoleName` (string). This is cleaner because:
- Role names are unique (enforced by Identity)
- No FK dependency on Identity tables
- No need for cross-module ID lookups

Update `IPermissionContracts` methods to use role name:
```csharp
Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(string roleName);
Task SetPermissionsForRoleAsync(string roleName, IEnumerable<string> permissions);
```

And the AuthorizationEndpoint becomes:
```csharp
var roles = await userManager.GetRolesAsync(user);
var permissionContracts = context.RequestServices.GetRequiredService<IPermissionContracts>();

var allPermissions = new HashSet<string>();
// Get role-based permissions
foreach (var role in roles)
{
    var rolePerms = await permissionContracts.GetPermissionsForRoleAsync(role);
    allPermissions.UnionWith(rolePerms);
}
// Get direct user permissions
var userPerms = await permissionContracts.GetPermissionsForUserAsync(userId);
allPermissions.UnionWith(userPerms);
```

**Step 5: Move OpenIddictSeedService**

Move from Users module, update namespace, update references. Remove role/admin user seeding (those stay in Users).

**Step 6: Add to solution**

```bash
dotnet slnx add modules/OpenIddict/src/OpenIddict/OpenIddict.csproj --solution-folder /modules/OpenIddict/
```

**Step 7: Commit**

```bash
git add modules/OpenIddict/ SimpleModule.slnx
git commit -m "feat(openiddict): add OpenIddict module with DbContext, endpoints, and seed service"
```

---

### Task 5: Slim Down Users Module

**Files:**
- Modify: `modules/Users/src/Users/UsersModule.cs` — remove all OpenIddict config, remove PermissionSeedService
- Modify: `modules/Users/src/Users/UsersDbContext.cs` — remove `UserPermissions`, `RolePermissions` DbSets, remove `UseOpenIddict()` call
- Modify: `modules/Users/src/Users/Users.csproj` — remove OpenIddict package references
- Delete: `modules/Users/src/Users/Endpoints/Connect/` (all 3 endpoints)
- Delete: `modules/Users/src/Users/Entities/UserPermission.cs`
- Delete: `modules/Users/src/Users/Entities/RolePermission.cs`
- Delete: `modules/Users/src/Users/Services/OpenIddictSeedService.cs`
- Delete: `modules/Users/src/Users/Services/PermissionSeedService.cs`
- Delete: `modules/Users/src/Users.Contracts/Constants/AuthConstants.cs`
- Delete: `modules/Users/src/Users.Contracts/Constants/ConnectRouteConstants.cs`
- Delete: `modules/Users/src/Users.Contracts/Constants/ClientConstants.cs`
- Delete: `modules/Users/src/Users.Contracts/Constants/AuthErrorMessages.cs`
- Modify: `modules/Users/src/Users.Contracts/Constants/ConfigKeys.cs` — keep only `SeedAdminPassword`
- Modify: `modules/Users/src/Users.Contracts/Constants/SeedConstants.cs` — keep user seed constants

**Step 1: Update UsersModule.cs**

Remove OpenIddict block (lines 22-112), PermissionSeedService registration (line 116), OpenIddictSeedService (line 115). Keep only:
- `AddModuleDbContext<UsersDbContext>` (without `UseOpenIddict()`)
- Identity configuration
- `ConsoleEmailSender`
- `IUserContracts` registration

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Entities;
using SimpleModule.Users.Services;

namespace SimpleModule.Users;

[Module(UsersConstants.ModuleName)]
public class UsersModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<UsersDbContext>(configuration, UsersConstants.ModuleName);

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddDefaultTokenProviders();

        services.AddHostedService<OpenIddictUserSeedService>();
        services.AddSingleton<IEmailSender<ApplicationUser>, ConsoleEmailSender>();
        services.AddScoped<IUserContracts, UserService>();
    }

    // ConfigureMenu stays unchanged
}
```

Note: `OpenIddictSeedService` contained role/admin user seeding that must stay in Users. Rename to `UserSeedService` and keep only the role + admin user seeding parts.

**Step 2: Create UserSeedService (from old OpenIddictSeedService, user-seeding parts only)**

Create `modules/Users/src/Users/Services/UserSeedService.cs`:
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Services;

public partial class UserSeedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    ILogger<UserSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!hostEnvironment.IsDevelopment())
            return;

        using var scope = serviceProvider.CreateScope();
        await SeedRolesAsync(scope);
        await SeedAdminUserAsync(scope);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ... SeedRolesAsync and SeedAdminUserAsync unchanged from current OpenIddictSeedService
}
```

**Step 3: Update UsersDbContext**

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
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyModuleSchema("Users", dbOptions.Value);
    }
}
```

**Step 4: Update Users.csproj — remove OpenIddict packages**

Remove:
```xml
<PackageReference Include="OpenIddict.AspNetCore" />
<PackageReference Include="OpenIddict.EntityFrameworkCore" />
```

**Step 5: Delete moved files**

Delete `Endpoints/Connect/` directory, `Entities/UserPermission.cs`, `Entities/RolePermission.cs`, `Services/OpenIddictSeedService.cs`, `Services/PermissionSeedService.cs`, and the moved constants files.

**Step 6: Update Users.Contracts constants**

- Delete `AuthConstants.cs`, `ConnectRouteConstants.cs`, `ClientConstants.cs`, `AuthErrorMessages.cs`
- Update `ConfigKeys.cs` to only keep `SeedAdminPassword`
- `SeedConstants.cs` stays as-is (admin role/user seeding is still in Users)

**Step 7: Build to verify**

Run: `dotnet build modules/Users/src/Users/Users.csproj`
Expected: Build succeeded

**Step 8: Commit**

```bash
git add -A
git commit -m "refactor(users): remove OpenIddict and permission concerns from Users module"
```

---

### Task 6: Update Admin Module

**Files:**
- Modify: `modules/Admin/src/Admin/Admin.csproj` — replace `Users.csproj` reference with `Users.Contracts`, add `Permissions.Contracts`
- Modify: `modules/Admin/src/Admin/Endpoints/Admin/AdminUsersEndpoint.cs` — replace `UsersDbContext` with `IPermissionContracts`
- Modify: `modules/Admin/src/Admin/Endpoints/Admin/AdminRolesEndpoint.cs` — replace `UsersDbContext` with `IPermissionContracts`

**Step 1: Update Admin.csproj**

Replace:
```xml
<ProjectReference Include="..\..\..\..\modules\Users\src\Users\Users.csproj" />
```
With:
```xml
<ProjectReference Include="..\..\..\..\modules\Users\src\Users.Contracts\Users.Contracts.csproj" />
<ProjectReference Include="..\..\..\..\modules\Permissions\src\Permissions.Contracts\Permissions.Contracts.csproj" />
```

Note: Admin still needs `UserManager<ApplicationUser>` and `RoleManager<ApplicationRole>` — these are registered via DI by the Users module. But `ApplicationUser` and `ApplicationRole` types are in the `Users` project, not `Users.Contracts`. The Admin module currently references `Users.csproj` directly for this reason. This is a known tight coupling.

**Decision**: Admin keeps its reference to `Users.csproj` (it needs `UserManager<ApplicationUser>`) but adds `Permissions.Contracts` for permission operations. The `UsersDbContext` usage is what gets replaced.

Updated Admin.csproj:
```xml
<ProjectReference Include="..\..\..\..\modules\Users\src\Users\Users.csproj" />
<ProjectReference Include="..\..\..\..\modules\Permissions\src\Permissions.Contracts\Permissions.Contracts.csproj" />
```

**Step 2: Update AdminUsersEndpoint.cs**

Replace `UsersDbContext` injection in the permissions endpoint with `IPermissionContracts`:

In `POST /{id}/permissions`:
```csharp
async (string id, HttpContext context, IPermissionContracts permissionContracts, AuditService audit) =>
{
    var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    var form = await context.Request.ReadFormAsync();
    var newPermissions = form["permissions"]
        .Where(p => !string.IsNullOrEmpty(p))
        .Select(p => p!)
        .ToHashSet();

    var currentPermissions = await permissionContracts.GetPermissionsForUserAsync(id);

    // Audit removed permissions
    foreach (var perm in currentPermissions.Where(p => !newPermissions.Contains(p)))
    {
        await audit.LogAsync(id, adminId, "PermissionRevoked", $"Revoked permission {perm}");
    }
    // Audit added permissions
    foreach (var perm in newPermissions.Where(p => !currentPermissions.Contains(p)))
    {
        await audit.LogAsync(id, adminId, "PermissionGranted", $"Granted permission {perm}");
    }

    await permissionContracts.SetPermissionsForUserAsync(id, newPermissions);

    return Results.Redirect($"/admin/users/{id}/edit?tab=roles");
}
```

**Step 3: Update AdminRolesEndpoint.cs**

Replace `UsersDbContext` usage with `IPermissionContracts`:

In `POST /` (create role):
```csharp
// Replace UsersDbContext with IPermissionContracts
async (..., IPermissionContracts permissionContracts, ...) =>
{
    // ... create role ...
    var form = await context.Request.ReadFormAsync();
    var permissions = form["permissions"]
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Select(p => p!)
        .ToList();

    if (permissions.Count > 0)
    {
        await permissionContracts.SetPermissionsForRoleAsync(role.Name!, permissions);
    }
    // ...
}
```

In `POST /{id}/permissions`:
```csharp
async (string id, HttpContext context, RoleManager<ApplicationRole> roleManager, IPermissionContracts permissionContracts, AuditService audit) =>
{
    var role = await roleManager.FindByIdAsync(id);
    if (role is null)
        return Results.NotFound();

    var form = await context.Request.ReadFormAsync();
    var newPermissions = form["permissions"]
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Select(p => p!)
        .ToHashSet(StringComparer.Ordinal);

    var currentPermissions = await permissionContracts.GetPermissionsForRoleAsync(role.Name!);
    var adminUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    foreach (var perm in currentPermissions.Where(p => !newPermissions.Contains(p)))
    {
        await audit.LogAsync(role.Id, adminUserId, "RolePermissionRemoved",
            $"Permission '{perm}' removed from role '{role.Name}'");
    }
    foreach (var perm in newPermissions.Where(p => !currentPermissions.Contains(p)))
    {
        await audit.LogAsync(role.Id, adminUserId, "RolePermissionAdded",
            $"Permission '{perm}' added to role '{role.Name}'");
    }

    await permissionContracts.SetPermissionsForRoleAsync(role.Name!, newPermissions);

    return Results.Redirect($"/admin/roles/{id}/edit?tab=permissions");
}
```

In `DELETE /{id}`:
```csharp
// Replace UsersDbContext permission cleanup with IPermissionContracts
await permissionContracts.SetPermissionsForRoleAsync(role.Name!, []);
```

**Step 4: Build to verify**

Run: `dotnet build modules/Admin/src/Admin/Admin.csproj`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add modules/Admin/
git commit -m "refactor(admin): use IPermissionContracts instead of direct UsersDbContext"
```

---

### Task 7: Update Host Project

**Files:**
- Modify: `template/SimpleModule.Host/SimpleModule.Host.csproj` — add OpenIddict and Permissions project references
- Modify: `template/SimpleModule.Host/Program.cs` — update using statements for moved constants, update HostDbContext registration

**Step 1: Update Host csproj**

Add:
```xml
<ProjectReference Include="..\..\modules\OpenIddict\src\OpenIddict\OpenIddict.csproj" />
<ProjectReference Include="..\..\modules\Permissions\src\Permissions\Permissions.csproj" />
```

**Step 2: Update Program.cs**

Replace `using SimpleModule.Users.Constants;` with `using SimpleModule.OpenIddict.Contracts;` (for `AuthConstants`, `ConnectRouteConstants`, `ClientConstants`).

The `HostDbContext` currently uses `opts => opts.UseOpenIddict()` — this needs to stay since OpenIddict stores its entities via the OpenIddictAppDbContext now, but the HostDbContext might still need OpenIddict for migration purposes. Evaluate whether `HostDbContext` still needs `UseOpenIddict()`.

**Step 3: Build to verify**

Run: `dotnet build template/SimpleModule.Host/SimpleModule.Host.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add template/SimpleModule.Host/
git commit -m "refactor(host): add OpenIddict and Permissions module references, update usings"
```

---

### Task 8: Update Test Infrastructure

**Files:**
- Modify: `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs` — add `PermissionsDbContext` and `OpenIddictAppDbContext` replacements
- Modify: `tests/SimpleModule.Tests.Shared/SimpleModule.Tests.Shared.csproj` — add project references

**Step 1: Update test factory**

Add:
```csharp
ReplaceDbContext<PermissionsDbContext>(services);
ReplaceDbContext<OpenIddictAppDbContext>(services, useOpenIddict: true);
```

Update `UsersDbContext` line — remove `useOpenIddict: true` since OpenIddict is no longer on UsersDbContext:
```csharp
ReplaceDbContext<UsersDbContext>(services);
```

**Step 2: Update test csproj**

Add references to the new module projects.

**Step 3: Build all tests**

Run: `dotnet build tests/SimpleModule.Tests.Shared/SimpleModule.Tests.Shared.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add tests/
git commit -m "test: update test infrastructure for OpenIddict and Permissions modules"
```

---

### Task 9: Fix Permissions Seed Service — RoleManager Resolution

**Context:** The `PermissionSeedService` needs to find the Admin role ID to seed permissions. Since Permissions module doesn't depend on Users, it can't reference `ApplicationRole`. However, at runtime, `RoleManager<ApplicationRole>` is registered in DI by the Users module.

**Approach:** Have the Permissions module depend on `Users.Contracts` and add a `GetRoleByNameAsync(string roleName)` method that returns the role ID. OR, since we changed `RolePermission` to use role name (not ID), the seed service just needs the role name string "Admin" — no lookup needed.

**With role name approach (decided in Task 4):**

```csharp
// PermissionSeedService just uses the role name directly
foreach (var permission in permissionRegistry.AllPermissions)
{
    if (!existingSet.Contains(permission))
    {
        newPermissions.Add(
            new RolePermission { RoleName = "Admin", Permission = permission }
        );
    }
}
```

This is already handled in Task 2. This task is for verification and integration testing.

**Step 1: Run all tests**

Run: `dotnet test`
Expected: All tests pass

**Step 2: Run the application**

Run: `dotnet run --project template/SimpleModule.Host`
Expected: App starts, OpenIddict seeds client, permissions seed runs, admin user created

**Step 3: Commit any fixes**

```bash
git add -A
git commit -m "fix: resolve integration issues from module split"
```

---

### Task 10: Create Permissions Module Tests

**Files:**
- Create: `modules/Permissions/tests/Permissions.Tests/Permissions.Tests.csproj`
- Create: `modules/Permissions/tests/Permissions.Tests/PermissionServiceTests.cs`

**Step 1: Create test project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Permissions\Permissions.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Write tests for PermissionService**

Test `GetPermissionsForUserAsync`, `GetPermissionsForRoleAsync`, `SetPermissionsForUserAsync`, `SetPermissionsForRoleAsync` using SQLite in-memory.

**Step 3: Add to solution**

```bash
dotnet slnx add modules/Permissions/tests/Permissions.Tests/Permissions.Tests.csproj --solution-folder /modules/Permissions/
```

**Step 4: Run tests**

Run: `dotnet test modules/Permissions/tests/Permissions.Tests/`
Expected: All tests pass

**Step 5: Commit**

```bash
git add modules/Permissions/tests/ SimpleModule.slnx
git commit -m "test(permissions): add unit tests for PermissionService"
```

---

### Task 11: Final Verification

**Step 1: Full build**

Run: `dotnet build`
Expected: Build succeeded with zero warnings (TreatWarningsAsErrors)

**Step 2: Full test suite**

Run: `dotnet test`
Expected: All tests pass

**Step 3: Lint frontend (if any changes)**

Run: `npm run check`
Expected: No lint errors

**Step 4: Run application end-to-end**

Run: `dotnet run --project template/SimpleModule.Host`
Verify:
- App starts on https://localhost:5001
- Swagger UI loads with OAuth2 config
- Can authenticate via OpenIddict flow
- Permissions are included in tokens

**Step 5: Final commit if needed**

```bash
git add -A
git commit -m "chore: final cleanup after OpenIddict/Permissions module split"
```
