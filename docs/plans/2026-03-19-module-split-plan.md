# OpenIddict & Permissions Module Split — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Extract OpenIddict and permissions into two new modules, slimming the Users module to identity-only concerns.

**Architecture:** Create `modules/Permissions/` (owns permission entities + queries, depends on Users.Contracts for `UserId`) and `modules/OpenIddict/` (owns OAuth2/OIDC, depends on Users.Contracts + Permissions.Contracts). Users module keeps only Identity entities, UserService, and ConsoleEmailSender. Admin module switches from `UsersDbContext` to `IPermissionContracts` for permission operations.

**Tech Stack:** .NET 10, EF Core, OpenIddict, Vogen (typed IDs), xUnit.v3, FluentAssertions, Bogus, SQLite in-memory for tests.

---

### Task 1: Create Permissions.Contracts Project

**Files:**
- Create: `modules/Permissions/src/Permissions.Contracts/Permissions.Contracts.csproj`
- Create: `modules/Permissions/src/Permissions.Contracts/IPermissionContracts.cs`
- Create: `modules/Permissions/src/Permissions.Contracts/PermissionsConstants.cs`
- Create: `modules/Permissions/src/Permissions.Contracts/RoleId.cs`

**Step 1: Create csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
    <DefineConstants>$(DefineConstants);VOGEN_NO_VALIDATION</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Vogen" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\modules\Users\src\Users.Contracts\Users.Contracts.csproj" />
  </ItemGroup>
</Project>
```

Note: References `Users.Contracts` for the `UserId` type. This is consistent with the project's convention that domain types like strongly-typed IDs belong in module Contracts.

**Step 2: Create RoleId**

```csharp
using Vogen;

namespace SimpleModule.Permissions.Contracts;

[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct RoleId
{
    private static Validation Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Validation.Invalid("RoleId cannot be empty")
            : Validation.Ok;
}
```

**Step 3: Create IPermissionContracts**

```csharp
using SimpleModule.Users.Contracts;

namespace SimpleModule.Permissions.Contracts;

public interface IPermissionContracts
{
    Task<IReadOnlySet<string>> GetPermissionsForUserAsync(UserId userId);
    Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(RoleId roleId);
    Task<IReadOnlySet<string>> GetAllPermissionsForUserAsync(UserId userId, IEnumerable<RoleId> roleIds);
    Task SetPermissionsForUserAsync(UserId userId, IEnumerable<string> permissions);
    Task SetPermissionsForRoleAsync(RoleId roleId, IEnumerable<string> permissions);
}
```

`GetAllPermissionsForUserAsync` combines user permissions + permissions for all given roles. This is what the AuthorizationEndpoint needs — it has the user ID and role IDs, and wants all permissions in one call.

**Step 4: Create PermissionsConstants**

```csharp
namespace SimpleModule.Permissions.Contracts;

public static class PermissionsConstants
{
    public const string ModuleName = "Permissions";
}
```

**Step 5: Add to solution**

```bash
dotnet slnx add modules/Permissions/src/Permissions.Contracts/Permissions.Contracts.csproj --solution-folder /modules/Permissions/
```

**Step 6: Build to verify**

Run: `dotnet build modules/Permissions/src/Permissions.Contracts/Permissions.Contracts.csproj`
Expected: Build succeeded

**Step 7: Commit**

```bash
git add modules/Permissions/src/Permissions.Contracts/ SimpleModule.slnx
git commit -m "feat(permissions): add Permissions.Contracts with IPermissionContracts, RoleId"
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
using SimpleModule.Users.Contracts;

namespace SimpleModule.Permissions.Entities;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class UserPermission
#pragma warning restore CA1711
{
    public UserId UserId { get; set; }
    public string Permission { get; set; } = string.Empty;
}
```

`Entities/RolePermission.cs`:
```csharp
using SimpleModule.Permissions.Contracts;

namespace SimpleModule.Permissions.Entities;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class RolePermission
#pragma warning restore CA1711
{
    public RoleId RoleId { get; set; }
    public string Permission { get; set; } = string.Empty;
}
```

No navigation properties — these are pure string-backed value object mappings with no FK to Identity tables.

**Step 3: Create PermissionsDbContext**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Permissions.Entities;
using SimpleModule.Users.Contracts;

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
            entity.Property(e => e.UserId).HasConversion<UserId.EfCoreValueConverter>();
            entity.ToTable("UserPermissions");
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.Permission });
            entity.Property(e => e.RoleId).HasConversion<RoleId.EfCoreValueConverter>();
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
using SimpleModule.Users.Contracts;

namespace SimpleModule.Permissions;

public class PermissionService(PermissionsDbContext db) : IPermissionContracts
{
    public async Task<IReadOnlySet<string>> GetPermissionsForUserAsync(UserId userId)
    {
        var perms = await db.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();

        return new HashSet<string>(perms);
    }

    public async Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(RoleId roleId)
    {
        var perms = await db.RolePermissions
            .Where(p => p.RoleId == roleId)
            .Select(p => p.Permission)
            .ToListAsync();

        return new HashSet<string>(perms);
    }

    public async Task<IReadOnlySet<string>> GetAllPermissionsForUserAsync(
        UserId userId,
        IEnumerable<RoleId> roleIds)
    {
        var roleIdList = roleIds.ToList();

        var rolePerms = await db.RolePermissions
            .Where(p => roleIdList.Contains(p.RoleId))
            .Select(p => p.Permission)
            .ToListAsync();

        var userPerms = await db.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();

        var result = new HashSet<string>(rolePerms);
        foreach (var p in userPerms)
            result.Add(p);

        return result;
    }

    public async Task SetPermissionsForUserAsync(UserId userId, IEnumerable<string> permissions)
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

    public async Task SetPermissionsForRoleAsync(RoleId roleId, IEnumerable<string> permissions)
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

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Authorization;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Permissions.Entities;
using SimpleModule.Users.Entities;

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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var adminRole = await roleManager.FindByNameAsync(AdminRole);
        if (adminRole is null)
            return;

        var adminRoleId = RoleId.From(adminRole.Id);

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

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Seeding {Count} permissions for Admin role..."
    )]
    private static partial void LogSeedingPermissions(ILogger logger, int count);
}
```

Note: The seed service references `ApplicationRole` and `RoleManager<ApplicationRole>` at runtime (registered by Users module). The Permissions.csproj does NOT reference Users.csproj — it references Users.Contracts (transitively via Permissions.Contracts). For the seed service to resolve `RoleManager<ApplicationRole>`, the Permissions implementation project needs a reference to Users.csproj. **Alternative**: Add `GetRoleIdByNameAsync(string name)` to `IUserContracts` and avoid the reference. This is the cleaner approach — update `IUserContracts` to include it.

**Revised approach**: Add to `IUserContracts`:
```csharp
Task<string?> GetRoleIdByNameAsync(string roleName);
```

Then the seed service calls `IUserContracts.GetRoleIdByNameAsync("Admin")` and wraps in `RoleId.From(...)`. No direct dependency on Users implementation needed.

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
git commit -m "feat(permissions): add Permissions module with DbContext, PermissionService, seed"
```

---

### Task 3: Create Permissions Module Tests

**Files:**
- Create: `modules/Permissions/tests/Permissions.Tests/Permissions.Tests.csproj`
- Create: `modules/Permissions/tests/Permissions.Tests/Unit/PermissionServiceTests.cs`
- Create: `modules/Permissions/tests/Permissions.Tests/Unit/PermissionServiceRoleTests.cs`
- Create: `modules/Permissions/tests/Permissions.Tests/Unit/PermissionServiceCombinedTests.cs`
- Create: `modules/Permissions/tests/Permissions.Tests/Helpers/TestDbContextFactory.cs`

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

**Step 2: Create TestDbContextFactory helper**

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;

namespace Permissions.Tests.Helpers;

public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public PermissionsDbContext Create()
    {
        var options = new DbContextOptionsBuilder<PermissionsDbContext>()
            .UseSqlite(_connection)
            .Options;

        var dbOptions = Options.Create(new DatabaseOptions());
        var context = new PermissionsDbContext(options, dbOptions);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
```

**Step 3: Write user permission tests**

`Unit/PermissionServiceTests.cs`:
```csharp
using FluentAssertions;
using Permissions.Tests.Helpers;
using SimpleModule.Permissions;
using SimpleModule.Users.Contracts;

namespace Permissions.Tests.Unit;

public class PermissionServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task GetPermissionsForUser_NoPermissions_ReturnsEmpty()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);

        var result = await sut.GetPermissionsForUserAsync(UserId.From("user-1"));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SetPermissionsForUser_ThenGet_ReturnsSetPermissions()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");

        await sut.SetPermissionsForUserAsync(userId, ["Admin.ManageUsers", "Admin.ViewAuditLog"]);

        var result = await sut.GetPermissionsForUserAsync(userId);
        result.Should().HaveCount(2);
        result.Should().Contain("Admin.ManageUsers");
        result.Should().Contain("Admin.ViewAuditLog");
    }

    [Fact]
    public async Task SetPermissionsForUser_ReplacesExisting()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");

        await sut.SetPermissionsForUserAsync(userId, ["Admin.ManageUsers", "Admin.ViewAuditLog"]);
        await sut.SetPermissionsForUserAsync(userId, ["Admin.ManageRoles"]);

        var result = await sut.GetPermissionsForUserAsync(userId);
        result.Should().HaveCount(1);
        result.Should().Contain("Admin.ManageRoles");
    }

    [Fact]
    public async Task SetPermissionsForUser_EmptyList_ClearsAll()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");

        await sut.SetPermissionsForUserAsync(userId, ["Admin.ManageUsers"]);
        await sut.SetPermissionsForUserAsync(userId, []);

        var result = await sut.GetPermissionsForUserAsync(userId);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionsForUser_IsolatedBetweenUsers()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var user1 = UserId.From("user-1");
        var user2 = UserId.From("user-2");

        await sut.SetPermissionsForUserAsync(user1, ["Admin.ManageUsers"]);
        await sut.SetPermissionsForUserAsync(user2, ["Admin.ManageRoles"]);

        var result1 = await sut.GetPermissionsForUserAsync(user1);
        var result2 = await sut.GetPermissionsForUserAsync(user2);

        result1.Should().ContainSingle().Which.Should().Be("Admin.ManageUsers");
        result2.Should().ContainSingle().Which.Should().Be("Admin.ManageRoles");
    }

    public void Dispose() => _factory.Dispose();
}
```

**Step 4: Write role permission tests**

`Unit/PermissionServiceRoleTests.cs`:
```csharp
using FluentAssertions;
using Permissions.Tests.Helpers;
using SimpleModule.Permissions;
using SimpleModule.Permissions.Contracts;

namespace Permissions.Tests.Unit;

public class PermissionServiceRoleTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task GetPermissionsForRole_NoPermissions_ReturnsEmpty()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);

        var result = await sut.GetPermissionsForRoleAsync(RoleId.From("role-1"));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SetPermissionsForRole_ThenGet_ReturnsSetPermissions()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var roleId = RoleId.From("role-admin");

        await sut.SetPermissionsForRoleAsync(roleId, ["Admin.ManageUsers", "Admin.ManageRoles", "Admin.ViewAuditLog"]);

        var result = await sut.GetPermissionsForRoleAsync(roleId);
        result.Should().HaveCount(3);
        result.Should().Contain("Admin.ManageUsers");
        result.Should().Contain("Admin.ManageRoles");
        result.Should().Contain("Admin.ViewAuditLog");
    }

    [Fact]
    public async Task SetPermissionsForRole_ReplacesExisting()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var roleId = RoleId.From("role-admin");

        await sut.SetPermissionsForRoleAsync(roleId, ["Admin.ManageUsers", "Admin.ManageRoles"]);
        await sut.SetPermissionsForRoleAsync(roleId, ["Admin.ViewAuditLog"]);

        var result = await sut.GetPermissionsForRoleAsync(roleId);
        result.Should().ContainSingle().Which.Should().Be("Admin.ViewAuditLog");
    }

    [Fact]
    public async Task SetPermissionsForRole_EmptyList_ClearsAll()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var roleId = RoleId.From("role-admin");

        await sut.SetPermissionsForRoleAsync(roleId, ["Admin.ManageUsers"]);
        await sut.SetPermissionsForRoleAsync(roleId, []);

        var result = await sut.GetPermissionsForRoleAsync(roleId);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionsForRole_IsolatedBetweenRoles()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var role1 = RoleId.From("role-admin");
        var role2 = RoleId.From("role-editor");

        await sut.SetPermissionsForRoleAsync(role1, ["Admin.ManageUsers"]);
        await sut.SetPermissionsForRoleAsync(role2, ["Admin.ViewAuditLog"]);

        var result1 = await sut.GetPermissionsForRoleAsync(role1);
        var result2 = await sut.GetPermissionsForRoleAsync(role2);

        result1.Should().ContainSingle().Which.Should().Be("Admin.ManageUsers");
        result2.Should().ContainSingle().Which.Should().Be("Admin.ViewAuditLog");
    }

    public void Dispose() => _factory.Dispose();
}
```

**Step 5: Write combined permission tests**

`Unit/PermissionServiceCombinedTests.cs`:
```csharp
using FluentAssertions;
using Permissions.Tests.Helpers;
using SimpleModule.Permissions;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace Permissions.Tests.Unit;

public class PermissionServiceCombinedTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    [Fact]
    public async Task GetAllPermissionsForUser_CombinesUserAndRolePermissions()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");
        var roleId = RoleId.From("role-admin");

        await sut.SetPermissionsForUserAsync(userId, ["Admin.ViewAuditLog"]);
        await sut.SetPermissionsForRoleAsync(roleId, ["Admin.ManageUsers", "Admin.ManageRoles"]);

        var result = await sut.GetAllPermissionsForUserAsync(userId, [roleId]);

        result.Should().HaveCount(3);
        result.Should().Contain("Admin.ViewAuditLog");
        result.Should().Contain("Admin.ManageUsers");
        result.Should().Contain("Admin.ManageRoles");
    }

    [Fact]
    public async Task GetAllPermissionsForUser_DeduplicatesOverlapping()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");
        var roleId = RoleId.From("role-admin");

        await sut.SetPermissionsForUserAsync(userId, ["Admin.ManageUsers", "Admin.ViewAuditLog"]);
        await sut.SetPermissionsForRoleAsync(roleId, ["Admin.ManageUsers", "Admin.ManageRoles"]);

        var result = await sut.GetAllPermissionsForUserAsync(userId, [roleId]);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllPermissionsForUser_MultipleRoles_CombinesAll()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");
        var role1 = RoleId.From("role-admin");
        var role2 = RoleId.From("role-editor");

        await sut.SetPermissionsForRoleAsync(role1, ["Admin.ManageUsers"]);
        await sut.SetPermissionsForRoleAsync(role2, ["Admin.ViewAuditLog"]);

        var result = await sut.GetAllPermissionsForUserAsync(userId, [role1, role2]);

        result.Should().HaveCount(2);
        result.Should().Contain("Admin.ManageUsers");
        result.Should().Contain("Admin.ViewAuditLog");
    }

    [Fact]
    public async Task GetAllPermissionsForUser_NoRoles_ReturnsOnlyUserPermissions()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);
        var userId = UserId.From("user-1");

        await sut.SetPermissionsForUserAsync(userId, ["Admin.ViewAuditLog"]);

        var result = await sut.GetAllPermissionsForUserAsync(userId, []);

        result.Should().ContainSingle().Which.Should().Be("Admin.ViewAuditLog");
    }

    [Fact]
    public async Task GetAllPermissionsForUser_NoPermissionsAnywhere_ReturnsEmpty()
    {
        await using var db = _factory.Create();
        var sut = new PermissionService(db);

        var result = await sut.GetAllPermissionsForUserAsync(
            UserId.From("user-1"),
            [RoleId.From("role-1")]);

        result.Should().BeEmpty();
    }

    public void Dispose() => _factory.Dispose();
}
```

**Step 6: Add to solution**

```bash
dotnet slnx add modules/Permissions/tests/Permissions.Tests/Permissions.Tests.csproj --solution-folder /modules/Permissions/
```

**Step 7: Run tests**

Run: `dotnet test modules/Permissions/tests/Permissions.Tests/`
Expected: All 15 tests pass

**Step 8: Commit**

```bash
git add modules/Permissions/tests/ SimpleModule.slnx
git commit -m "test(permissions): add unit tests for PermissionService"
```

---

### Task 4: Create OpenIddict.Contracts Project

**Files:**
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/OpenIddict.Contracts.csproj`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/OpenIddictModuleConstants.cs`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/AuthConstants.cs`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/ConnectRouteConstants.cs`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/ClientConstants.cs`
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/ConfigKeys.cs`
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

**Step 2: Create constants — move from Users.Contracts**

Move all constant classes from `modules/Users/src/Users.Contracts/Constants/` to OpenIddict.Contracts, changing namespace to `SimpleModule.OpenIddict.Contracts`:

`OpenIddictModuleConstants.cs`:
```csharp
namespace SimpleModule.OpenIddict.Contracts;

public static class OpenIddictModuleConstants
{
    public const string ModuleName = "OpenIddict";
}
```

`AuthConstants.cs`:
```csharp
namespace SimpleModule.OpenIddict.Contracts;

public static class AuthConstants
{
    public const string OAuth2Scheme = "oauth2";
    public const string SmartAuthPolicy = "SmartAuth";
    public const string OpenIdScope = "openid";
    public const string ProfileScope = "profile";
    public const string EmailScope = "email";
    public const string RolesScope = "roles";
}
```

`ConnectRouteConstants.cs`:
```csharp
namespace SimpleModule.OpenIddict.Contracts;

public static class ConnectRouteConstants
{
    public const string ConnectAuthorize = "/connect/authorize";
    public const string ConnectToken = "/connect/token";
    public const string ConnectEndSession = "/connect/endsession";
    public const string ConnectUserInfo = "/connect/userinfo";
}
```

`ClientConstants.cs`:
```csharp
namespace SimpleModule.OpenIddict.Contracts;

public static class ClientConstants
{
    public const string ClientId = "simplemodule-client";
    public const string ClientDisplayName = "SimpleModule Client";
    public const string SwaggerCallbackPath = "/swagger/oauth2-redirect.html";
    public const string OAuthCallbackPath = "/oauth-callback";
    public const string PostLogoutRedirectPath = "/";
    public const string DefaultBaseUrl = "https://localhost:5001";
}
```

`ConfigKeys.cs` (OpenIddict-specific keys only):
```csharp
namespace SimpleModule.OpenIddict.Contracts;

public static class ConfigKeys
{
    public const string OpenIddictBaseUrl = "OpenIddict:BaseUrl";
    public const string OpenIddictEncryptionCertPath = "OpenIddict:EncryptionCertificatePath";
    public const string OpenIddictSigningCertPath = "OpenIddict:SigningCertificatePath";
    public const string OpenIddictCertPassword = "OpenIddict:CertificatePassword";
    public const string OpenIddictAdditionalRedirectUris = "OpenIddict:AdditionalRedirectUris";
}
```

`AuthErrorMessages.cs`:
```csharp
namespace SimpleModule.OpenIddict.Contracts;

public static class AuthErrorMessages
{
    public const string OpenIdConnectRequestMissing =
        "The OpenID Connect request cannot be retrieved.";
    public const string UserDetailsMissing = "The user details cannot be retrieved.";
}
```

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
git commit -m "feat(openiddict): add OpenIddict.Contracts with auth constants"
```

---

### Task 5: Create OpenIddict Module Implementation

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
    <ProjectReference Include="..\..\..\..\modules\Users\src\Users\Users.csproj" />
    <ProjectReference Include="..\..\..\..\modules\Permissions\src\Permissions.Contracts\Permissions.Contracts.csproj" />
  </ItemGroup>
</Project>
```

Note: References `Users.csproj` (not just Contracts) because `AuthorizationEndpoint` needs `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`. This is the same pattern Admin uses.

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

Move the OpenIddict configuration from `UsersModule.ConfigureServices`:

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
                    options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
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

Move 3 endpoints from `modules/Users/src/Users/Endpoints/Connect/` to `modules/OpenIddict/src/OpenIddict/Endpoints/Connect/`.

Key changes to `AuthorizationEndpoint`:
- Namespace: `SimpleModule.OpenIddict.Endpoints.Connect`
- Replace direct `UsersDbContext` permission query with `IPermissionContracts`:

```csharp
// Instead of querying UsersDbContext directly:
var permissionContracts = context.RequestServices.GetRequiredService<IPermissionContracts>();
var userId = UserId.From(await userManager.GetUserIdAsync(user));

// Get role IDs for permission lookup
var roles = await userManager.GetRolesAsync(user);
// Need role IDs — get them from the Identity DB
var userDb = context.RequestServices.GetRequiredService<UsersDbContext>();
var roleIds = await userDb.Roles
    .Where(r => roles.Contains(r.Name!))
    .Select(r => RoleId.From(r.Id))
    .ToListAsync();

var allPermissions = await permissionContracts.GetAllPermissionsForUserAsync(userId, roleIds);

foreach (var permission in allPermissions)
{
    identity.AddClaim("permission", permission);
}
```

`LogoutEndpoint` and `UserinfoEndpoint` — just namespace change, no logic changes.

**Step 5: Move OpenIddictSeedService**

Move from `modules/Users/src/Users/Services/OpenIddictSeedService.cs`. Keep only the client application seeding part. Remove `SeedRolesAsync` and `SeedAdminUserAsync` (those stay in Users module as `UserSeedService`).

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using SimpleModule.OpenIddict.Contracts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SimpleModule.OpenIddict.Services;

public partial class OpenIddictSeedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<OpenIddictSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        await SeedClientApplicationAsync(scope, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // SeedClientApplicationAsync — same as current but using OpenIddict.Contracts constants
    // (copy from existing, update namespace references)
}
```

**Step 6: Add to solution**

```bash
dotnet slnx add modules/OpenIddict/src/OpenIddict/OpenIddict.csproj --solution-folder /modules/OpenIddict/
```

**Step 7: Build to verify**

Run: `dotnet build modules/OpenIddict/src/OpenIddict/OpenIddict.csproj`
Expected: Build succeeded

**Step 8: Commit**

```bash
git add modules/OpenIddict/ SimpleModule.slnx
git commit -m "feat(openiddict): add OpenIddict module with DbContext, endpoints, seed service"
```

---

### Task 6: Slim Down Users Module

**Files:**
- Modify: `modules/Users/src/Users/UsersModule.cs` — remove OpenIddict config, PermissionSeedService
- Modify: `modules/Users/src/Users/UsersDbContext.cs` — remove permission DbSets
- Modify: `modules/Users/src/Users/Users.csproj` — remove OpenIddict packages
- Create: `modules/Users/src/Users/Services/UserSeedService.cs` — role + admin user seeding
- Delete: `modules/Users/src/Users/Endpoints/Connect/AuthorizationEndpoint.cs`
- Delete: `modules/Users/src/Users/Endpoints/Connect/LogoutEndpoint.cs`
- Delete: `modules/Users/src/Users/Endpoints/Connect/UserinfoEndpoint.cs`
- Delete: `modules/Users/src/Users/Entities/UserPermission.cs`
- Delete: `modules/Users/src/Users/Entities/RolePermission.cs`
- Delete: `modules/Users/src/Users/Entities/UserPermissionConfiguration.cs`
- Delete: `modules/Users/src/Users/Entities/RolePermissionConfiguration.cs`
- Delete: `modules/Users/src/Users/Services/OpenIddictSeedService.cs`
- Delete: `modules/Users/src/Users/Services/PermissionSeedService.cs`
- Delete: `modules/Users/src/Users.Contracts/Constants/AuthConstants.cs`
- Delete: `modules/Users/src/Users.Contracts/Constants/ConnectRouteConstants.cs`
- Delete: `modules/Users/src/Users.Contracts/Constants/ClientConstants.cs`
- Delete: `modules/Users/src/Users.Contracts/Constants/AuthErrorMessages.cs`
- Modify: `modules/Users/src/Users.Contracts/Constants/ConfigKeys.cs` — keep only SeedAdminPassword

**Step 1: Update UsersModule.cs**

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

        services.AddHostedService<UserSeedService>();
        services.AddSingleton<IEmailSender<ApplicationUser>, ConsoleEmailSender>();
        services.AddScoped<IUserContracts, UserService>();
    }

    // ConfigureMenu — unchanged
}
```

**Step 2: Create UserSeedService**

Extract role + admin user seeding from `OpenIddictSeedService`:

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

    // SeedRolesAsync — same as current OpenIddictSeedService.SeedRolesAsync
    // SeedAdminUserAsync — same as current OpenIddictSeedService.SeedAdminUserAsync
    // LoggerMessage methods — same as current
}
```

**Step 3: Update UsersDbContext**

Remove `UserPermissions` and `RolePermissions` DbSets:

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

Also remove `<ProjectReference Include="..\..\..\..\framework\SimpleModule.Blazor\SimpleModule.Blazor.csproj" />` if no longer needed (check if any remaining code uses Blazor — it was needed for Razor views in Connect endpoints).

Change SDK from `Microsoft.NET.Sdk.Razor` to `Microsoft.NET.Sdk` if no Razor files remain. Check if any `.cshtml`/`.razor` files exist in Users module — if the module has Razor views for Identity pages, keep the Razor SDK.

**Step 5: Update ConfigKeys.cs**

```csharp
namespace SimpleModule.Users.Constants;

public static class ConfigKeys
{
    public const string SeedAdminPassword = "Seed:AdminPassword";
}
```

**Step 6: Delete moved files**

Delete all files listed in the file manifest above.

**Step 7: Build to verify**

Run: `dotnet build modules/Users/src/Users/Users.csproj`
Expected: Build succeeded

**Step 8: Commit**

```bash
git add -A
git commit -m "refactor(users): remove OpenIddict and permission concerns, add UserSeedService"
```

---

### Task 7: Update Admin Module

**Files:**
- Modify: `modules/Admin/src/Admin/Admin.csproj` — add Permissions.Contracts reference
- Modify: `modules/Admin/src/Admin/Endpoints/Admin/AdminUsersEndpoint.cs` — use `IPermissionContracts`
- Modify: `modules/Admin/src/Admin/Endpoints/Admin/AdminRolesEndpoint.cs` — use `IPermissionContracts`

**Step 1: Update Admin.csproj**

Add:
```xml
<ProjectReference Include="..\..\..\..\modules\Permissions\src\Permissions.Contracts\Permissions.Contracts.csproj" />
```

Keep existing `Users.csproj` reference (needed for `UserManager<ApplicationUser>`).

**Step 2: Update AdminUsersEndpoint**

In `POST /{id}/permissions`, replace `UsersDbContext usersDb` with `IPermissionContracts permissionContracts`:

```csharp
async (string id, HttpContext context, IPermissionContracts permissionContracts, AuditService audit) =>
{
    var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    var userId = UserId.From(id);

    var form = await context.Request.ReadFormAsync();
    var newPermissions = form["permissions"]
        .Where(p => !string.IsNullOrEmpty(p))
        .Select(p => p!)
        .ToHashSet();

    var currentPermissions = await permissionContracts.GetPermissionsForUserAsync(userId);

    foreach (var perm in currentPermissions.Where(p => !newPermissions.Contains(p)))
    {
        await audit.LogAsync(id, adminId, "PermissionRevoked", $"Revoked permission {perm}");
    }
    foreach (var perm in newPermissions.Where(p => !currentPermissions.Contains(p)))
    {
        await audit.LogAsync(id, adminId, "PermissionGranted", $"Granted permission {perm}");
    }

    await permissionContracts.SetPermissionsForUserAsync(userId, newPermissions);

    return Results.Redirect($"/admin/users/{id}/edit?tab=roles");
}
```

**Step 3: Update AdminRolesEndpoint**

Replace all `UsersDbContext usersDb` injections with `IPermissionContracts permissionContracts`. Use `RoleId.From(role.Id)` for typed ID access.

In `POST /` (create role):
```csharp
// After role creation:
var permissions = form["permissions"]
    .Where(p => !string.IsNullOrWhiteSpace(p))
    .Select(p => p!)
    .ToList();

if (permissions.Count > 0)
{
    await permissionContracts.SetPermissionsForRoleAsync(RoleId.From(role.Id), permissions);
}
```

In `POST /{id}/permissions`:
```csharp
var currentPermissions = await permissionContracts.GetPermissionsForRoleAsync(RoleId.From(id));
// ... audit diffs ...
await permissionContracts.SetPermissionsForRoleAsync(RoleId.From(id), newPermissions);
```

In `DELETE /{id}`:
```csharp
await permissionContracts.SetPermissionsForRoleAsync(RoleId.From(id), []);
```

Remove all `using SimpleModule.Users;` imports that referenced `UsersDbContext` and permission entities. Add `using SimpleModule.Permissions.Contracts;` and `using SimpleModule.Users.Contracts;`.

**Step 4: Update Admin view endpoints if they reference UsersDbContext for permissions**

Check `modules/Admin/src/Admin/Views/Admin/RolesEndpoint.cs` and `RolesEditEndpoint.cs` — these also query `UsersDbContext.RolePermissions`. Update them to use `IPermissionContracts`.

**Step 5: Build to verify**

Run: `dotnet build modules/Admin/src/Admin/Admin.csproj`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add modules/Admin/
git commit -m "refactor(admin): use IPermissionContracts instead of direct UsersDbContext"
```

---

### Task 8: Update Host Project

**Files:**
- Modify: `template/SimpleModule.Host/SimpleModule.Host.csproj` — add new module references
- Modify: `template/SimpleModule.Host/Program.cs` — update usings

**Step 1: Update Host csproj**

Add:
```xml
<ProjectReference Include="..\..\modules\OpenIddict\src\OpenIddict\OpenIddict.csproj" />
<ProjectReference Include="..\..\modules\Permissions\src\Permissions\Permissions.csproj" />
```

**Step 2: Update Program.cs**

Replace:
```csharp
using SimpleModule.Users.Constants;
```
With:
```csharp
using SimpleModule.OpenIddict.Contracts;
```

The `HostDbContext` `opts => opts.UseOpenIddict()` — evaluate if still needed. If OpenIddict tables are now in `OpenIddictAppDbContext`, the HostDbContext may no longer need `UseOpenIddict()`. However, for EF migrations that need to see all tables, it might still be needed. Check if `HostDbContext` inherits from all module DbContexts or uses a unified migration. Update accordingly.

**Step 3: Build to verify**

Run: `dotnet build template/SimpleModule.Host/SimpleModule.Host.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add template/SimpleModule.Host/
git commit -m "refactor(host): add OpenIddict and Permissions module references"
```

---

### Task 9: Update Test Infrastructure

**Files:**
- Modify: `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs`
- Modify: `tests/SimpleModule.Tests.Shared/SimpleModule.Tests.Shared.csproj`
- Create: `tests/SimpleModule.Tests.Shared/Fakes/FakePermissionContracts.cs`

**Step 1: Update test factory**

Add new DbContext replacements:
```csharp
ReplaceDbContext<PermissionsDbContext>(services);
ReplaceDbContext<OpenIddictAppDbContext>(services, useOpenIddict: true);
```

Update `UsersDbContext` — remove `useOpenIddict: true`:
```csharp
ReplaceDbContext<UsersDbContext>(services);
```

Add usings:
```csharp
using SimpleModule.OpenIddict;
using SimpleModule.Permissions;
```

**Step 2: Create FakePermissionContracts**

```csharp
using SimpleModule.Permissions.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Tests.Shared.Fakes;

public class FakePermissionContracts : IPermissionContracts
{
    private readonly Dictionary<string, HashSet<string>> _userPermissions = new();
    private readonly Dictionary<string, HashSet<string>> _rolePermissions = new();

    public Task<IReadOnlySet<string>> GetPermissionsForUserAsync(UserId userId)
    {
        var key = userId.Value;
        return Task.FromResult<IReadOnlySet<string>>(
            _userPermissions.TryGetValue(key, out var perms)
                ? perms
                : new HashSet<string>());
    }

    public Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(RoleId roleId)
    {
        var key = roleId.Value;
        return Task.FromResult<IReadOnlySet<string>>(
            _rolePermissions.TryGetValue(key, out var perms)
                ? perms
                : new HashSet<string>());
    }

    public async Task<IReadOnlySet<string>> GetAllPermissionsForUserAsync(
        UserId userId,
        IEnumerable<RoleId> roleIds)
    {
        var result = new HashSet<string>();

        var userPerms = await GetPermissionsForUserAsync(userId);
        result.UnionWith(userPerms);

        foreach (var roleId in roleIds)
        {
            var rolePerms = await GetPermissionsForRoleAsync(roleId);
            result.UnionWith(rolePerms);
        }

        return result;
    }

    public Task SetPermissionsForUserAsync(UserId userId, IEnumerable<string> permissions)
    {
        _userPermissions[userId.Value] = new HashSet<string>(permissions);
        return Task.CompletedTask;
    }

    public Task SetPermissionsForRoleAsync(RoleId roleId, IEnumerable<string> permissions)
    {
        _rolePermissions[roleId.Value] = new HashSet<string>(permissions);
        return Task.CompletedTask;
    }
}
```

**Step 3: Update test csproj**

Add references to `Permissions.Contracts`, `Permissions`, `OpenIddict`, and `OpenIddict.Contracts`.

**Step 4: Build tests**

Run: `dotnet build tests/SimpleModule.Tests.Shared/SimpleModule.Tests.Shared.csproj`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add tests/SimpleModule.Tests.Shared/
git commit -m "test: update test infrastructure for module split"
```

---

### Task 10: Create OpenIddict Module Tests

**Files:**
- Create: `modules/OpenIddict/tests/OpenIddict.Tests/OpenIddict.Tests.csproj`
- Create: `modules/OpenIddict/tests/OpenIddict.Tests/Integration/ConnectEndpointTests.cs`
- Create: `modules/OpenIddict/tests/OpenIddict.Tests/Integration/OpenIddictSeedServiceTests.cs`

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
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\OpenIddict\OpenIddict.csproj" />
    <ProjectReference Include="..\..\..\..\tests\SimpleModule.Tests.Shared\SimpleModule.Tests.Shared.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Write Connect endpoint integration tests**

```csharp
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleModule.Tests.Shared.Fixtures;

namespace OpenIddict.Tests.Integration;

public class ConnectEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ConnectEndpointTests(SimpleModuleWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Authorize_Unauthenticated_Redirects()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/connect/authorize?response_type=code&client_id=simplemodule-client&scope=openid&redirect_uri=https://localhost:5001/oauth-callback");

        // Should redirect to login
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Userinfo_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/connect/userinfo");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Userinfo_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/connect/userinfo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EndSession_Returns200OrRedirect()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/connect/endsession");

        // Should either redirect or succeed
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);
    }
}
```

**Step 3: Write seed service tests**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using SimpleModule.Tests.Shared.Fixtures;

namespace OpenIddict.Tests.Integration;

public class OpenIddictSeedServiceTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public OpenIddictSeedServiceTests(SimpleModuleWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SeedService_CreatesClientApplication()
    {
        using var scope = _factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var client = await manager.FindByClientIdAsync("simplemodule-client");

        client.Should().NotBeNull();
    }
}
```

**Step 4: Add to solution and run**

```bash
dotnet slnx add modules/OpenIddict/tests/OpenIddict.Tests/OpenIddict.Tests.csproj --solution-folder /modules/OpenIddict/
dotnet test modules/OpenIddict/tests/OpenIddict.Tests/
```
Expected: All tests pass

**Step 5: Commit**

```bash
git add modules/OpenIddict/tests/ SimpleModule.slnx
git commit -m "test(openiddict): add integration tests for Connect endpoints and seed service"
```

---

### Task 11: Update Existing Admin Tests

**Files:**
- Modify: `modules/Admin/tests/Admin.Tests/Integration/AdminUsersEndpointTests.cs` — remove `UsersDbContext` references if any
- Modify: `modules/Admin/tests/Admin.Tests/Integration/AdminRolesEndpointTests.cs` — update imports

**Step 1: Update test imports**

Replace any `using SimpleModule.Users;` (for `UsersDbContext`) with `using SimpleModule.Permissions.Contracts;` where applicable.

**Step 2: Run existing admin tests**

Run: `dotnet test modules/Admin/tests/Admin.Tests/`
Expected: All tests pass

**Step 3: Commit if changes needed**

```bash
git add modules/Admin/tests/
git commit -m "test(admin): update test imports for module split"
```

---

### Task 12: Add Integration Tests for Permissions in Admin Context

**Files:**
- Create: `modules/Admin/tests/Admin.Tests/Integration/AdminPermissionsTests.cs`

**Step 1: Write tests for permission assignment via Admin endpoints**

```csharp
using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Entities;

namespace Admin.Tests.Integration;

public class AdminPermissionsTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public AdminPermissionsTests(SimpleModuleWebApplicationFactory factory) => _factory = factory;

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.NameIdentifier, "admin-test-id"),
        };
        var claimsValue = string.Join(";", claims.Select(c => $"{c.Type}={c.Value}"));
        client.DefaultRequestHeaders.Add("X-Test-Claims", claimsValue);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        return client;
    }

    [Fact]
    public async Task SetUserPermissions_ValidData_Redirects()
    {
        // Seed a test user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = $"perm-test-{Guid.NewGuid():N}@test.com",
            Email = $"perm-test-{Guid.NewGuid():N}@test.com",
            DisplayName = "Permission Test User",
        };
        await userManager.CreateAsync(user, "TestPass123!");

        var client = CreateAdminClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "permissions", "Admin.ManageUsers" },
        });

        var response = await client.PostAsync($"/admin/users/{user.Id}/permissions", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task SetRolePermissions_ValidData_Redirects()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var role = new ApplicationRole
        {
            Name = $"PermTestRole-{Guid.NewGuid().ToString()[..8]}",
            Description = "Test role for permissions",
        };
        await roleManager.CreateAsync(role);

        var client = CreateAdminClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "permissions", "Admin.ManageUsers" },
        });

        var response = await client.PostAsync($"/admin/roles/{role.Id}/permissions", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task SetUserPermissions_NonExistentUser_Redirects()
    {
        var client = CreateAdminClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "permissions", "Admin.ManageUsers" },
        });

        // The endpoint doesn't currently check user existence — it just sets permissions
        // This test verifies the endpoint works with a non-existent user ID
        var response = await client.PostAsync("/admin/users/nonexistent-id/permissions", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task CreateRoleWithPermissions_ValidData_CreatesRoleAndAssignsPermissions()
    {
        var client = CreateAdminClient();
        var roleName = $"WithPerms-{Guid.NewGuid().ToString()[..8]}";

        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("name", roleName),
            new KeyValuePair<string, string>("description", "Test"),
            new KeyValuePair<string, string>("permissions", "Admin.ManageUsers"),
            new KeyValuePair<string, string>("permissions", "Admin.ViewAuditLog"),
        });

        var response = await client.PostAsync("/admin/roles", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Verify permissions were assigned via IPermissionContracts
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var role = await roleManager.FindByNameAsync(roleName);
        role.Should().NotBeNull();

        var permContracts = scope.ServiceProvider.GetRequiredService<IPermissionContracts>();
        var perms = await permContracts.GetPermissionsForRoleAsync(RoleId.From(role!.Id));
        perms.Should().HaveCount(2);
        perms.Should().Contain("Admin.ManageUsers");
        perms.Should().Contain("Admin.ViewAuditLog");
    }

    [Fact]
    public async Task DeleteRole_ClearsPermissions()
    {
        // Create role with permissions
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var role = new ApplicationRole
        {
            Name = $"DelPermRole-{Guid.NewGuid().ToString()[..8]}",
            Description = "To be deleted",
        };
        await roleManager.CreateAsync(role);

        var permContracts = scope.ServiceProvider.GetRequiredService<IPermissionContracts>();
        await permContracts.SetPermissionsForRoleAsync(
            RoleId.From(role.Id),
            ["Admin.ManageUsers", "Admin.ManageRoles"]);

        var client = CreateAdminClient();
        var response = await client.DeleteAsync($"/admin/roles/{role.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Verify permissions were cleared
        using var scope2 = _factory.Services.CreateScope();
        var permContracts2 = scope2.ServiceProvider.GetRequiredService<IPermissionContracts>();
        var perms = await permContracts2.GetPermissionsForRoleAsync(RoleId.From(role.Id));
        perms.Should().BeEmpty();
    }
}
```

**Step 2: Run all admin tests**

Run: `dotnet test modules/Admin/tests/Admin.Tests/`
Expected: All tests pass

**Step 3: Commit**

```bash
git add modules/Admin/tests/
git commit -m "test(admin): add integration tests for permission operations via IPermissionContracts"
```

---

### Task 13: Final Verification

**Step 1: Full build**

Run: `dotnet build`
Expected: Build succeeded with zero warnings

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
- OpenIddict client is seeded
- Admin role permissions are seeded
- Can authenticate and get permissions in token

**Step 5: Final commit if needed**

```bash
git add -A
git commit -m "chore: final cleanup after OpenIddict/Permissions module split"
```
