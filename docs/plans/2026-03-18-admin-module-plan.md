# Admin Module Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a separate Admin module with full user CRUD (soft delete), role management with permission assignment, security controls, and audit logging — replacing the admin functionality currently in the Users module.

**Architecture:** New `Admin` module that references `Users` directly for Identity access (`UserManager`, `RoleManager`, entities). Owns its own `AdminDbContext` with an `AuditLogEntry` entity. React pages use tabbed UI for edit forms. Permissions grouped by module via `PermissionRegistry`.

**Tech Stack:** .NET 10, ASP.NET Core Identity, EF Core (SQLite/PostgreSQL), Inertia.js, React 19, Vite, @simplemodule/ui (Radix-based), xUnit.v3, FluentAssertions

---

### Task 1: Scaffold Admin.Contracts Project

**Files:**
- Create: `modules/Admin/src/Admin.Contracts/Admin.Contracts.csproj`
- Create: `modules/Admin/src/Admin.Contracts/AdminConstants.cs`
- Create: `modules/Admin/src/Admin.Contracts/AdminPermissions.cs`
- Create: `modules/Admin/src/Admin.Contracts/AuditLogEntryDto.cs`

**Step 1: Create Admin.Contracts.csproj**

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

**Step 2: Create AdminConstants.cs**

```csharp
namespace SimpleModule.Admin.Contracts;

public static class AdminConstants
{
    public const string ModuleName = "Admin";
    public const string RoutePrefix = "/admin";
}
```

**Step 3: Create AdminPermissions.cs**

```csharp
namespace SimpleModule.Admin.Contracts;

public static class AdminPermissions
{
    public const string ManageUsers = "Admin.ManageUsers";
    public const string ManageRoles = "Admin.ManageRoles";
    public const string ViewAuditLog = "Admin.ViewAuditLog";
}
```

**Step 4: Create AuditLogEntryDto.cs**

```csharp
using SimpleModule.Core;

namespace SimpleModule.Admin.Contracts;

[Dto]
public class AuditLogEntryDto
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PerformedByUserId { get; set; } = string.Empty;
    public string PerformedByName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```

**Step 5: Verify it compiles**

Run: `dotnet build modules/Admin/src/Admin.Contracts/Admin.Contracts.csproj`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add modules/Admin/src/Admin.Contracts/
git commit -m "feat(admin): scaffold Admin.Contracts project with constants, permissions, and DTOs"
```

---

### Task 2: Scaffold Admin Project (Module, DbContext, Entity)

**Files:**
- Create: `modules/Admin/src/Admin/Admin.csproj`
- Create: `modules/Admin/src/Admin/AdminModule.cs`
- Create: `modules/Admin/src/Admin/AdminDbContext.cs`
- Create: `modules/Admin/src/Admin/Entities/AuditLogEntry.cs`
- Create: `modules/Admin/src/Admin/Entities/AuditLogEntryConfiguration.cs`
- Create: `modules/Admin/src/Admin/Services/AuditService.cs`

**Step 1: Create Admin.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Database\SimpleModule.Database.csproj" />
    <ProjectReference Include="..\Admin.Contracts\Admin.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\modules\Users\src\Users\Users.csproj" />
  </ItemGroup>
  <Target
    Name="JsBuild"
    BeforeTargets="Build"
    Condition="Exists('package.json') And (Exists('node_modules') Or Exists('$(RepoRoot)node_modules'))"
  >
    <Exec Command="npx vite build" WorkingDirectory="$(MSBuildProjectDirectory)" />
  </Target>
</Project>
```

**Step 2: Create AuditLogEntry entity**

File: `modules/Admin/src/Admin/Entities/AuditLogEntry.cs`

```csharp
namespace SimpleModule.Admin.Entities;

public class AuditLogEntry
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PerformedByUserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
```

**Step 3: Create AuditLogEntryConfiguration**

File: `modules/Admin/src/Admin/Entities/AuditLogEntryConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SimpleModule.Admin.Entities;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.PerformedByUserId).IsRequired();
        builder.Property(e => e.Action).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Details).HasMaxLength(4000);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Timestamp);
    }
}
```

**Step 4: Create AdminDbContext**

File: `modules/Admin/src/Admin/AdminDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Admin.Contracts;
using SimpleModule.Admin.Entities;
using SimpleModule.Database;

namespace SimpleModule.Admin;

public class AdminDbContext(
    DbContextOptions<AdminDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);
        builder.ApplyModuleSchema(AdminConstants.ModuleName, dbOptions.Value);
    }
}
```

**Step 5: Create AuditService**

File: `modules/Admin/src/Admin/Services/AuditService.cs`

```csharp
using SimpleModule.Admin.Entities;

namespace SimpleModule.Admin.Services;

public class AuditService(AdminDbContext db)
{
    public async Task LogAsync(string userId, string performedByUserId, string action, string? details = null)
    {
        db.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = userId,
            PerformedByUserId = performedByUserId,
            Action = action,
            Details = details,
            Timestamp = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
```

**Step 6: Create AdminModule.cs (minimal, menu and permissions)**

File: `modules/Admin/src/Admin/AdminModule.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Admin.Contracts;
using SimpleModule.Admin.Services;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Database;

namespace SimpleModule.Admin;

[Module(AdminConstants.ModuleName)]
public class AdminModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<AdminDbContext>(configuration, AdminConstants.ModuleName);
        services.AddScoped<AuditService>();
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<AdminPermissions>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(new MenuItem
        {
            Label = "Users",
            Url = "/admin/users",
            Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/></svg>""",
            Order = 20,
            Section = MenuSection.Navbar,
        });
        menus.Add(new MenuItem
        {
            Label = "Roles",
            Url = "/admin/roles",
            Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>""",
            Order = 21,
            Section = MenuSection.Navbar,
        });
        menus.Add(new MenuItem
        {
            Label = "Manage Users",
            Url = "/admin/users",
            Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/></svg>""",
            Order = 30,
            Section = MenuSection.UserDropdown,
            Group = "admin",
        });
        menus.Add(new MenuItem
        {
            Label = "Manage Roles",
            Url = "/admin/roles",
            Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>""",
            Order = 31,
            Section = MenuSection.UserDropdown,
            Group = "admin",
        });
    }
}
```

**Step 7: Verify it compiles**

Run: `dotnet build modules/Admin/src/Admin/Admin.csproj`
Expected: Build succeeded

**Step 8: Commit**

```bash
git add modules/Admin/src/Admin/
git commit -m "feat(admin): add Admin module with DbContext, AuditLogEntry entity, and AuditService"
```

---

### Task 3: Wire Admin Module into Host and Solution

**Files:**
- Modify: `template/SimpleModule.Host/SimpleModule.Host.csproj` — add Admin project reference
- Modify: `SimpleModule.slnx` — add Admin projects to solution
- Modify: `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs` — add AdminDbContext

**Step 1: Add project reference to Host csproj**

In `template/SimpleModule.Host/SimpleModule.Host.csproj`, add after the Orders reference:

```xml
<ProjectReference Include="..\..\modules\Admin\src\Admin\Admin.csproj" />
```

**Step 2: Add projects to solution**

In `SimpleModule.slnx`, add a new folder block after the Users folder:

```xml
<Folder Name="/modules/Admin/">
    <Project Path="modules/Admin/src/Admin.Contracts/Admin.Contracts.csproj" />
    <Project Path="modules/Admin/src/Admin/Admin.csproj" />
    <Project Path="modules/Admin/tests/Admin.Tests/Admin.Tests.csproj" />
</Folder>
```

**Step 3: Add AdminDbContext to test factory**

In `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs`:
- Add `using SimpleModule.Admin;`
- Add `ReplaceDbContext<AdminDbContext>(services);` after the existing `ReplaceDbContext` calls

**Step 4: Verify full solution builds**

Run: `dotnet build`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add template/SimpleModule.Host/SimpleModule.Host.csproj SimpleModule.slnx tests/SimpleModule.Tests.Shared/
git commit -m "feat(admin): wire Admin module into Host, solution, and test factory"
```

---

### Task 4: Add DeactivatedAt to ApplicationUser

**Files:**
- Modify: `modules/Users/src/Users/Entities/ApplicationUser.cs` — add `DeactivatedAt` property

**Step 1: Add the property**

In `ApplicationUser.cs`, add:

```csharp
public DateTimeOffset? DeactivatedAt { get; set; }
```

**Step 2: Verify it compiles**

Run: `dotnet build modules/Users/src/Users/Users.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add modules/Users/src/Users/Entities/ApplicationUser.cs
git commit -m "feat(users): add DeactivatedAt property to ApplicationUser for soft delete"
```

---

### Task 5: Remove Admin Code from Users Module

**Files:**
- Delete: `modules/Users/src/Users/Endpoints/Admin/AdminUsersEndpoint.cs`
- Delete: `modules/Users/src/Users/Endpoints/Admin/AdminRolesEndpoint.cs`
- Delete: `modules/Users/src/Users/Views/Admin/UsersEndpoint.cs`
- Delete: `modules/Users/src/Users/Views/Admin/UsersEditEndpoint.cs`
- Delete: `modules/Users/src/Users/Views/Admin/RolesEndpoint.cs`
- Delete: `modules/Users/src/Users/Views/Admin/RolesCreateEndpoint.cs`
- Delete: `modules/Users/src/Users/Views/Admin/RolesEditEndpoint.cs`
- Delete: `modules/Users/src/Users/Pages/Admin/Users.tsx`
- Delete: `modules/Users/src/Users/Pages/Admin/UsersEdit.tsx`
- Delete: `modules/Users/src/Users/Pages/Admin/Roles.tsx`
- Delete: `modules/Users/src/Users/Pages/Admin/RolesCreate.tsx`
- Delete: `modules/Users/src/Users/Pages/Admin/RolesEdit.tsx`
- Modify: `modules/Users/src/Users/Pages/index.ts` — remove Admin page imports/exports
- Modify: `modules/Users/src/Users/UsersModule.cs` — remove admin menu items (Users, Roles navbar + Manage Users, Manage Roles dropdown)
- Delete: `modules/Users/tests/Users.Tests/Integration/AdminUsersEndpointTests.cs` (if exists)
- Delete: `modules/Users/tests/Users.Tests/Integration/AdminRolesEndpointTests.cs` (if exists)

**Step 1: Delete all admin endpoint, view, and page files**

Delete all files listed above.

**Step 2: Update Pages/index.ts**

Remove the Admin imports and exports. The file should only contain Account page mappings:

```typescript
import Disable2fa from './Account/Disable2fa';
import EnableAuthenticator from './Account/EnableAuthenticator';
import GenerateRecoveryCodes from './Account/GenerateRecoveryCodes';
import ResetAuthenticator from './Account/ResetAuthenticator';
import ShowRecoveryCodes from './Account/ShowRecoveryCodes';
import TwoFactorAuthentication from './Account/TwoFactorAuthentication';

export const pages: Record<string, any> = {
  'Users/Account/TwoFactorAuthentication': TwoFactorAuthentication,
  'Users/Account/EnableAuthenticator': EnableAuthenticator,
  'Users/Account/Disable2fa': Disable2fa,
  'Users/Account/ResetAuthenticator': ResetAuthenticator,
  'Users/Account/GenerateRecoveryCodes': GenerateRecoveryCodes,
  'Users/Account/ShowRecoveryCodes': ShowRecoveryCodes,
};
```

**Step 3: Update UsersModule.cs — remove admin menu items**

Remove the 4 menu items: "Users" (navbar), "Roles" (navbar), "Manage Users" (dropdown), "Manage Roles" (dropdown). Keep "Account Settings", "Email", "Security" dropdown items.

**Step 4: Delete admin test files**

Delete `AdminUsersEndpointTests.cs` and `AdminRolesEndpointTests.cs` if they exist.

**Step 5: Verify it compiles**

Run: `dotnet build`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add -A
git commit -m "refactor(users): remove admin UI, endpoints, and menu items — moved to Admin module"
```

---

### Task 6: Admin User View Endpoints (List + Create + Edit)

**Files:**
- Create: `modules/Admin/src/Admin/Views/Admin/UsersEndpoint.cs`
- Create: `modules/Admin/src/Admin/Views/Admin/UsersCreateEndpoint.cs`
- Create: `modules/Admin/src/Admin/Views/Admin/UsersEditEndpoint.cs`
- Create: `modules/Admin/src/Admin/Views/Admin/UsersActivityEndpoint.cs`

**Step 1: Create UsersEndpoint.cs (list view)**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Views.Admin;

public class UsersEndpoint : IViewEndpoint
{
    private const int PageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/users",
                async (
                    UserManager<ApplicationUser> userManager,
                    string? search,
                    int page = 1
                ) =>
                {
                    var query = userManager.Users.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var pattern = $"%{search.Trim()}%";
                        query = query.Where(u =>
                            (u.Email != null && EF.Functions.Like(u.Email, pattern))
                            || EF.Functions.Like(u.DisplayName, pattern)
                            || (u.UserName != null && EF.Functions.Like(u.UserName, pattern))
                        );
                    }

                    var totalCount = await query.CountAsync();
                    var totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
                    page = Math.Clamp(page, 1, Math.Max(1, totalPages));

                    var users = await query
                        .OrderBy(u => u.DisplayName)
                        .Skip((page - 1) * PageSize)
                        .Take(PageSize)
                        .ToListAsync();

                    var userList = new List<object>();
                    foreach (var user in users)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        userList.Add(new
                        {
                            id = user.Id,
                            displayName = user.DisplayName,
                            email = user.Email,
                            emailConfirmed = user.EmailConfirmed,
                            roles = roles.ToList(),
                            isLockedOut = user.LockoutEnd.HasValue
                                && user.LockoutEnd > DateTimeOffset.UtcNow,
                            isDeactivated = user.DeactivatedAt.HasValue,
                            createdAt = user.CreatedAt.ToString("O"),
                        });
                    }

                    return Inertia.Render("Admin/Admin/Users", new
                    {
                        users = userList,
                        search = search ?? "",
                        page,
                        totalPages,
                        totalCount,
                    });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

**Step 2: Create UsersCreateEndpoint.cs**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Views.Admin;

public class UsersCreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/users/create",
                async (RoleManager<ApplicationRole> roleManager) =>
                {
                    var allRoles = await roleManager.Roles
                        .OrderBy(r => r.Name)
                        .Select(r => new { id = r.Id, name = r.Name, description = r.Description })
                        .ToListAsync();

                    return Inertia.Render("Admin/Admin/UsersCreate", new { allRoles });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

**Step 3: Create UsersEditEndpoint.cs (tabbed view with all data)**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Admin.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Users;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Views.Admin;

public class UsersEditEndpoint : IViewEndpoint
{
    private const int ActivityPageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/users/{id}/edit",
                async (
                    string id,
                    UserManager<ApplicationUser> userManager,
                    RoleManager<ApplicationRole> roleManager,
                    UsersDbContext usersDb,
                    AdminDbContext adminDb,
                    PermissionRegistry permissionRegistry,
                    string? tab
                ) =>
                {
                    var user = await userManager.FindByIdAsync(id);
                    if (user is null)
                        return Results.NotFound();

                    var userRoles = await userManager.GetRolesAsync(user);
                    var allRoles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

                    // User direct permissions
                    var userPermissions = await usersDb.UserPermissions
                        .Where(p => p.UserId == id)
                        .Select(p => p.Permission)
                        .ToListAsync();

                    // Permission registry grouped by module
                    var permissionsByModule = permissionRegistry.ByModule
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.ToList()
                        );

                    // Activity log (first page)
                    var activityLog = await adminDb.AuditLogEntries
                        .Where(e => e.UserId == id)
                        .OrderByDescending(e => e.Timestamp)
                        .Take(ActivityPageSize)
                        .Select(e => new
                        {
                            e.Id,
                            e.Action,
                            e.Details,
                            e.PerformedByUserId,
                            timestamp = e.Timestamp.ToString("O"),
                        })
                        .ToListAsync();

                    // Resolve performer names for activity entries
                    var performerIds = activityLog
                        .Select(e => e.PerformedByUserId)
                        .Distinct()
                        .ToList();
                    var performers = await userManager.Users
                        .Where(u => performerIds.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

                    var activityWithNames = activityLog.Select(e => new
                    {
                        e.Id,
                        e.Action,
                        e.Details,
                        performedBy = performers.GetValueOrDefault(e.PerformedByUserId, "Unknown"),
                        e.timestamp,
                    });

                    var activityTotal = await adminDb.AuditLogEntries.CountAsync(e => e.UserId == id);

                    return Inertia.Render("Admin/Admin/UsersEdit", new
                    {
                        user = new
                        {
                            id = user.Id,
                            displayName = user.DisplayName,
                            email = user.Email,
                            emailConfirmed = user.EmailConfirmed,
                            twoFactorEnabled = user.TwoFactorEnabled,
                            isLockedOut = user.LockoutEnd.HasValue
                                && user.LockoutEnd > DateTimeOffset.UtcNow,
                            isDeactivated = user.DeactivatedAt.HasValue,
                            accessFailedCount = user.AccessFailedCount,
                            createdAt = user.CreatedAt.ToString("O"),
                            lastLoginAt = user.LastLoginAt?.ToString("O"),
                        },
                        userRoles = userRoles.ToList(),
                        userPermissions,
                        allRoles = allRoles.Select(r => new
                        {
                            id = r.Id,
                            name = r.Name,
                            description = r.Description,
                        }).ToList(),
                        permissionsByModule,
                        activityLog = activityWithNames,
                        activityTotal,
                        tab = tab ?? "details",
                    });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

**Step 4: Create UsersActivityEndpoint.cs (paginated activity partial)**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Views.Admin;

public class UsersActivityEndpoint : IEndpoint
{
    private const int PageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/users/{id}/activity",
                async (
                    string id,
                    AdminDbContext adminDb,
                    UserManager<ApplicationUser> userManager,
                    int page = 1
                ) =>
                {
                    var entries = await adminDb.AuditLogEntries
                        .Where(e => e.UserId == id)
                        .OrderByDescending(e => e.Timestamp)
                        .Skip((page - 1) * PageSize)
                        .Take(PageSize)
                        .ToListAsync();

                    var performerIds = entries.Select(e => e.PerformedByUserId).Distinct().ToList();
                    var performers = await userManager.Users
                        .Where(u => performerIds.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

                    var total = await adminDb.AuditLogEntries.CountAsync(e => e.UserId == id);

                    return Results.Ok(new
                    {
                        entries = entries.Select(e => new
                        {
                            e.Id,
                            e.Action,
                            e.Details,
                            performedBy = performers.GetValueOrDefault(e.PerformedByUserId, "Unknown"),
                            timestamp = e.Timestamp.ToString("O"),
                        }),
                        total,
                        page,
                        totalPages = (int)Math.Ceiling((double)total / PageSize),
                    });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

**Step 5: Verify it compiles**

Run: `dotnet build modules/Admin/src/Admin/Admin.csproj`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add modules/Admin/src/Admin/Views/
git commit -m "feat(admin): add user view endpoints (list, create, edit with tabs, activity)"
```

---

### Task 7: Admin User Action Endpoints

**Files:**
- Create: `modules/Admin/src/Admin/Endpoints/Admin/AdminUsersEndpoint.cs`

This single endpoint class maps all user mutation routes.

**Step 1: Create AdminUsersEndpoint.cs**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Services;
using SimpleModule.Core;
using SimpleModule.Users;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Endpoints.Admin;

public class AdminUsersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/users")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // Create user
        group.MapPost("/", async (
            [FromForm] string email,
            [FromForm] string displayName,
            [FromForm] string password,
            [FromForm] string? emailConfirmed,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName.Trim(),
                EmailConfirmed = emailConfirmed is not null,
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return Results.Redirect("/admin/users/create");

            // Assign roles from form
            var form = await context.Request.ReadFormAsync();
            var roles = form["roles"].Where(r => !string.IsNullOrEmpty(r)).ToList();
            if (roles.Count > 0)
                await userManager.AddToRolesAsync(user, roles!);

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(user.Id, adminId, "UserCreated",
                $"Created user {user.Email}");

            return Results.Redirect($"/admin/users/{user.Id}/edit");
        }).DisableAntiforgery();

        // Update user details
        group.MapPost("/{id}", async (
            string id,
            [FromForm] string displayName,
            [FromForm] string email,
            [FromForm] string? emailConfirmed,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            user.DisplayName = displayName;
            user.Email = email;
            user.EmailConfirmed = emailConfirmed is not null;
            await userManager.UpdateAsync(user);

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(id, adminId, "UserUpdated");

            return Results.Redirect($"/admin/users/{id}/edit?tab=details");
        }).DisableAntiforgery();

        // Set roles
        group.MapPost("/{id}/roles", async (
            string id,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            var form = await context.Request.ReadFormAsync();
            var newRoles = form["roles"].Where(r => !string.IsNullOrEmpty(r)).ToList();
            var currentRoles = await userManager.GetRolesAsync(user);

            var removed = currentRoles.Except(newRoles!).ToList();
            var added = newRoles!.Except(currentRoles).ToList();

            await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (newRoles.Count > 0)
                await userManager.AddToRolesAsync(user, newRoles!);

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            foreach (var role in removed)
                await audit.LogAsync(id, adminId, "RoleRemoved", role);
            foreach (var role in added)
                await audit.LogAsync(id, adminId, "RoleAdded", role);

            return Results.Redirect($"/admin/users/{id}/edit?tab=roles");
        }).DisableAntiforgery();

        // Set direct permissions
        group.MapPost("/{id}/permissions", async (
            string id,
            HttpContext context,
            UsersDbContext usersDb,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            var form = await context.Request.ReadFormAsync();
            var newPermissions = form["permissions"]
                .Where(p => !string.IsNullOrEmpty(p))
                .ToHashSet();

            var currentPermissions = usersDb.UserPermissions
                .Where(p => p.UserId == id)
                .ToList();

            var currentSet = currentPermissions.Select(p => p.Permission).ToHashSet();
            var removed = currentSet.Except(newPermissions).ToList();
            var added = newPermissions.Except(currentSet).ToList();

            usersDb.UserPermissions.RemoveRange(
                currentPermissions.Where(p => removed.Contains(p.Permission)));
            foreach (var perm in added)
                usersDb.UserPermissions.Add(new UserPermission { UserId = id, Permission = perm });
            await usersDb.SaveChangesAsync();

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            foreach (var perm in removed)
                await audit.LogAsync(id, adminId, "PermissionRevoked", perm);
            foreach (var perm in added)
                await audit.LogAsync(id, adminId, "PermissionGranted", perm);

            return Results.Redirect($"/admin/users/{id}/edit?tab=roles");
        }).DisableAntiforgery();

        // Reset password
        group.MapPost("/{id}/reset-password", async (
            string id,
            [FromForm] string newPassword,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
                return Results.Redirect($"/admin/users/{id}/edit?tab=security");

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(id, adminId, "PasswordReset");

            return Results.Redirect($"/admin/users/{id}/edit?tab=security");
        }).DisableAntiforgery();

        // Lock account
        group.MapPost("/{id}/lock", async (
            string id,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            await userManager.SetLockoutEnabledAsync(user, true);
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(id, adminId, "AccountLocked");

            return Results.Redirect($"/admin/users/{id}/edit?tab=security");
        });

        // Unlock account
        group.MapPost("/{id}/unlock", async (
            string id,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(id, adminId, "AccountUnlocked");

            return Results.Redirect($"/admin/users/{id}/edit?tab=security");
        });

        // Force email re-verification
        group.MapPost("/{id}/force-reverify", async (
            string id,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            user.EmailConfirmed = false;
            await userManager.UpdateAsync(user);

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(id, adminId, "EmailReverified");

            return Results.Redirect($"/admin/users/{id}/edit?tab=security");
        });

        // Disable 2FA
        group.MapPost("/{id}/disable-2fa", async (
            string id,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            await userManager.SetTwoFactorEnabledAsync(user, false);
            await userManager.ResetAuthenticatorKeyAsync(user);

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(id, adminId, "TwoFactorDisabled");

            return Results.Redirect($"/admin/users/{id}/edit?tab=security");
        });

        // Deactivate (soft delete)
        group.MapPost("/{id}/deactivate", async (
            string id,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            user.DeactivatedAt = DateTimeOffset.UtcNow;
            await userManager.UpdateAsync(user);

            // Also lock the account
            await userManager.SetLockoutEnabledAsync(user, true);
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(id, adminId, "UserDeactivated");

            return Results.Redirect($"/admin/users/{id}/edit?tab=details");
        });

        // Reactivate
        group.MapPost("/{id}/reactivate", async (
            string id,
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            user.DeactivatedAt = null;
            await userManager.UpdateAsync(user);
            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(id, adminId, "UserReactivated");

            return Results.Redirect($"/admin/users/{id}/edit?tab=details");
        });
    }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build modules/Admin/src/Admin/Admin.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add modules/Admin/src/Admin/Endpoints/
git commit -m "feat(admin): add all user action endpoints (CRUD, roles, permissions, security, soft delete)"
```

---

### Task 8: Admin Role View and Action Endpoints

**Files:**
- Create: `modules/Admin/src/Admin/Views/Admin/RolesEndpoint.cs`
- Create: `modules/Admin/src/Admin/Views/Admin/RolesCreateEndpoint.cs`
- Create: `modules/Admin/src/Admin/Views/Admin/RolesEditEndpoint.cs`
- Create: `modules/Admin/src/Admin/Endpoints/Admin/AdminRolesEndpoint.cs`

**Step 1: Create RolesEndpoint.cs (list view)**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Views.Admin;

public class RolesEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/roles",
                async (
                    RoleManager<ApplicationRole> roleManager,
                    UserManager<ApplicationUser> userManager,
                    UsersDbContext usersDb
                ) =>
                {
                    var roles = await roleManager.Roles
                        .OrderBy(r => r.Name)
                        .ToListAsync();

                    var roleList = new List<object>();
                    foreach (var role in roles)
                    {
                        var usersInRole = role.Name is not null
                            ? await userManager.GetUsersInRoleAsync(role.Name)
                            : [];

                        var permissionCount = await usersDb.RolePermissions
                            .CountAsync(rp => rp.RoleId == role.Id);

                        roleList.Add(new
                        {
                            id = role.Id,
                            name = role.Name,
                            description = role.Description,
                            userCount = usersInRole.Count,
                            permissionCount,
                            createdAt = role.CreatedAt.ToString("O"),
                        });
                    }

                    return Inertia.Render("Admin/Admin/Roles", new { roles = roleList });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

**Step 2: Create RolesCreateEndpoint.cs**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Admin.Views.Admin;

public class RolesCreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/roles/create",
                (PermissionRegistry permissionRegistry) =>
                {
                    var permissionsByModule = permissionRegistry.ByModule
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.ToList()
                        );

                    return Inertia.Render("Admin/Admin/RolesCreate", new { permissionsByModule });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

**Step 3: Create RolesEditEndpoint.cs (tabbed view)**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Users;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Views.Admin;

public class RolesEditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/admin/roles/{id}/edit",
                async (
                    string id,
                    RoleManager<ApplicationRole> roleManager,
                    UserManager<ApplicationUser> userManager,
                    UsersDbContext usersDb,
                    PermissionRegistry permissionRegistry,
                    string? tab
                ) =>
                {
                    var role = await roleManager.FindByIdAsync(id);
                    if (role is null)
                        return Results.NotFound();

                    var usersInRole = role.Name is not null
                        ? await userManager.GetUsersInRoleAsync(role.Name)
                        : [];

                    var rolePermissions = await usersDb.RolePermissions
                        .Where(rp => rp.RoleId == id)
                        .Select(rp => rp.Permission)
                        .ToListAsync();

                    var permissionsByModule = permissionRegistry.ByModule
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.ToList()
                        );

                    return Inertia.Render("Admin/Admin/RolesEdit", new
                    {
                        role = new
                        {
                            id = role.Id,
                            name = role.Name,
                            description = role.Description,
                            createdAt = role.CreatedAt.ToString("O"),
                        },
                        users = usersInRole.Select(u => new
                        {
                            id = u.Id,
                            displayName = u.DisplayName,
                            email = u.Email,
                        }).ToList(),
                        rolePermissions,
                        permissionsByModule,
                        tab = tab ?? "details",
                    });
                }
            )
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

**Step 4: Create AdminRolesEndpoint.cs (action endpoints)**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Admin.Services;
using SimpleModule.Core;
using SimpleModule.Users;
using SimpleModule.Users.Entities;

namespace SimpleModule.Admin.Endpoints.Admin;

public class AdminRolesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/roles")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // Create role with permissions
        group.MapPost("/", async (
            [FromForm] string name,
            [FromForm] string? description,
            HttpContext context,
            RoleManager<ApplicationRole> roleManager,
            UsersDbContext usersDb,
            AuditService audit
        ) =>
        {
            var trimmedName = name.Trim();
            if (string.IsNullOrEmpty(trimmedName))
                return Results.Redirect("/admin/roles/create");

            var role = new ApplicationRole
            {
                Name = trimmedName,
                Description = description?.Trim() is { Length: > 0 } d ? d : null,
            };

            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
                return Results.Redirect("/admin/roles/create");

            // Assign permissions from form
            var form = await context.Request.ReadFormAsync();
            var permissions = form["permissions"].Where(p => !string.IsNullOrEmpty(p)).ToList();
            foreach (var perm in permissions)
                usersDb.RolePermissions.Add(new RolePermission { RoleId = role.Id, Permission = perm! });
            if (permissions.Count > 0)
                await usersDb.SaveChangesAsync();

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(role.Id, adminId, "RoleCreated", $"Created role {trimmedName}");

            return Results.Redirect($"/admin/roles/{role.Id}/edit");
        }).DisableAntiforgery();

        // Update role details
        group.MapPost("/{id}", async (
            string id,
            [FromForm] string name,
            [FromForm] string? description,
            HttpContext context,
            RoleManager<ApplicationRole> roleManager,
            AuditService audit
        ) =>
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role is null)
                return Results.NotFound();

            role.Name = name.Trim();
            role.Description = description?.Trim() is { Length: > 0 } d ? d : null;
            await roleManager.UpdateAsync(role);

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(id, adminId, "RoleUpdated");

            return Results.Redirect($"/admin/roles/{id}/edit?tab=details");
        }).DisableAntiforgery();

        // Set role permissions
        group.MapPost("/{id}/permissions", async (
            string id,
            HttpContext context,
            RoleManager<ApplicationRole> roleManager,
            UsersDbContext usersDb,
            AuditService audit
        ) =>
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role is null)
                return Results.NotFound();

            var form = await context.Request.ReadFormAsync();
            var newPermissions = form["permissions"]
                .Where(p => !string.IsNullOrEmpty(p))
                .ToHashSet();

            var currentPermissions = usersDb.RolePermissions
                .Where(rp => rp.RoleId == id)
                .ToList();

            var currentSet = currentPermissions.Select(p => p.Permission).ToHashSet();
            var removed = currentSet.Except(newPermissions).ToList();
            var added = newPermissions.Except(currentSet).ToList();

            usersDb.RolePermissions.RemoveRange(
                currentPermissions.Where(p => removed.Contains(p.Permission)));
            foreach (var perm in added)
                usersDb.RolePermissions.Add(new RolePermission { RoleId = id, Permission = perm });
            await usersDb.SaveChangesAsync();

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            foreach (var perm in removed)
                await audit.LogAsync(id, adminId, "RolePermissionRemoved", perm);
            foreach (var perm in added)
                await audit.LogAsync(id, adminId, "RolePermissionAdded", perm);

            return Results.Redirect($"/admin/roles/{id}/edit?tab=permissions");
        }).DisableAntiforgery();

        // Delete role
        group.MapDelete("/{id}", async (
            string id,
            HttpContext context,
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            AuditService audit
        ) =>
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role is null)
                return Results.NotFound();

            var usersInRole = role.Name is not null
                ? await userManager.GetUsersInRoleAsync(role.Name)
                : [];
            if (usersInRole.Count > 0)
                return Results.BadRequest(new { error = "Cannot delete role with assigned users" });

            var roleName = role.Name;
            await roleManager.DeleteAsync(role);

            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            await audit.LogAsync(id, adminId, "RoleDeleted", $"Deleted role {roleName}");

            return Results.Redirect("/admin/roles");
        });
    }
}
```

**Step 5: Verify it compiles**

Run: `dotnet build modules/Admin/src/Admin/Admin.csproj`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add modules/Admin/src/Admin/Views/Admin/Roles* modules/Admin/src/Admin/Endpoints/Admin/AdminRolesEndpoint.cs
git commit -m "feat(admin): add role view and action endpoints (CRUD, permissions, delete guard)"
```

---

### Task 9: Frontend Setup (package.json, vite.config, shared components)

**Files:**
- Create: `modules/Admin/src/Admin/package.json`
- Create: `modules/Admin/src/Admin/vite.config.ts`
- Create: `modules/Admin/src/Admin/tsconfig.json`
- Create: `modules/Admin/src/Admin/Pages/components/PermissionGroups.tsx`
- Create: `modules/Admin/src/Admin/Pages/components/ActivityTimeline.tsx`
- Create: `modules/Admin/src/Admin/Pages/components/TabNav.tsx`

**Step 1: Create package.json**

```json
{
  "private": true,
  "name": "@simplemodule/admin",
  "version": "0.0.0",
  "scripts": {
    "build": "vite build",
    "watch": "vite build --watch"
  },
  "peerDependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  }
}
```

**Step 2: Create vite.config.ts**

```typescript
import { resolve } from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react()],
  define: { 'process.env.NODE_ENV': JSON.stringify('production') },
  build: {
    lib: {
      entry: {
        'Admin.pages': resolve(__dirname, 'Pages/index.ts'),
      },
      formats: ['es'],
      fileName: (_format, entryName) => `${entryName}.js`,
    },
    outDir: 'wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      external: ['react', 'react-dom', 'react/jsx-runtime', '@inertiajs/react'],
    },
  },
});
```

**Step 3: Create tsconfig.json**

Copy from Users module or create minimal one matching project conventions.

**Step 4: Create TabNav.tsx**

```tsx
import { router } from '@inertiajs/react';

interface Tab {
  id: string;
  label: string;
}

interface TabNavProps {
  tabs: Tab[];
  activeTab: string;
  baseUrl: string;
}

export function TabNav({ tabs, activeTab, baseUrl }: TabNavProps) {
  return (
    <div className="border-b border-border mb-6">
      <nav className="flex gap-0 -mb-px">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => router.get(baseUrl, { tab: tab.id }, { preserveState: true })}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              activeTab === tab.id
                ? 'border-primary text-primary'
                : 'border-transparent text-text-muted hover:text-text hover:border-border'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </nav>
    </div>
  );
}
```

**Step 5: Create PermissionGroups.tsx**

```tsx
interface PermissionGroupsProps {
  permissionsByModule: Record<string, string[]>;
  selected: string[];
  namePrefix: string;
}

export function PermissionGroups({ permissionsByModule, selected, namePrefix }: PermissionGroupsProps) {
  const modules = Object.entries(permissionsByModule);

  if (modules.length === 0) {
    return <p className="text-sm text-text-muted">No permissions registered.</p>;
  }

  return (
    <div className="space-y-4">
      {modules.map(([moduleName, permissions]) => {
        const allSelected = permissions.every((p) => selected.includes(p));

        return (
          <div key={moduleName} className="border border-border rounded-lg p-4">
            <div className="flex items-center justify-between mb-3">
              <h4 className="font-medium text-sm">{moduleName}</h4>
              <label className="flex items-center gap-1.5 text-xs text-text-muted cursor-pointer">
                <input
                  type="checkbox"
                  checked={allSelected}
                  onChange={(e) => {
                    const checkboxes = e.currentTarget
                      .closest('.border')
                      ?.querySelectorAll<HTMLInputElement>(`input[name="${namePrefix}"]`);
                    checkboxes?.forEach((cb) => {
                      cb.checked = e.currentTarget.checked;
                    });
                  }}
                  className="h-4 w-4 rounded border border-border bg-surface accent-primary"
                />
                Select all
              </label>
            </div>
            <div className="flex flex-wrap gap-x-6 gap-y-2">
              {permissions.map((perm) => {
                const shortName = perm.includes('.') ? perm.split('.').pop() : perm;
                return (
                  <label key={perm} className="flex items-center gap-1.5 text-sm cursor-pointer">
                    <input
                      type="checkbox"
                      name={namePrefix}
                      value={perm}
                      defaultChecked={selected.includes(perm)}
                      className="h-4 w-4 rounded border border-border bg-surface accent-primary"
                    />
                    {shortName}
                  </label>
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
}
```

**Step 6: Create ActivityTimeline.tsx**

```tsx
interface ActivityEntry {
  id: number;
  action: string;
  details: string | null;
  performedBy: string;
  timestamp: string;
}

interface ActivityTimelineProps {
  entries: ActivityEntry[];
  total: number;
  onLoadMore?: () => void;
}

const actionLabels: Record<string, string> = {
  UserCreated: 'User created',
  UserUpdated: 'Details updated',
  UserDeactivated: 'Account deactivated',
  UserReactivated: 'Account reactivated',
  RoleAdded: 'Role added',
  RoleRemoved: 'Role removed',
  PermissionGranted: 'Permission granted',
  PermissionRevoked: 'Permission revoked',
  PasswordReset: 'Password reset',
  AccountLocked: 'Account locked',
  AccountUnlocked: 'Account unlocked',
  EmailReverified: 'Email re-verification required',
  TwoFactorDisabled: '2FA disabled',
};

function timeAgo(timestamp: string): string {
  const seconds = Math.floor((Date.now() - new Date(timestamp).getTime()) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days}d ago`;
  return new Date(timestamp).toLocaleDateString();
}

export function ActivityTimeline({ entries, total, onLoadMore }: ActivityTimelineProps) {
  if (entries.length === 0) {
    return <p className="text-sm text-text-muted">No activity recorded.</p>;
  }

  return (
    <div>
      <div className="space-y-0">
        {entries.map((entry) => (
          <div key={entry.id} className="flex gap-3 py-3 border-b border-border last:border-0">
            <div className="flex-1">
              <p className="text-sm font-medium">
                {actionLabels[entry.action] ?? entry.action}
                {entry.details && (
                  <span className="text-text-muted font-normal"> — {entry.details}</span>
                )}
              </p>
              <p className="text-xs text-text-muted mt-0.5">
                by {entry.performedBy} &middot; {timeAgo(entry.timestamp)}
              </p>
            </div>
          </div>
        ))}
      </div>
      {entries.length < total && onLoadMore && (
        <button
          type="button"
          onClick={onLoadMore}
          className="mt-4 text-sm text-primary hover:underline"
        >
          Load more ({total - entries.length} remaining)
        </button>
      )}
    </div>
  );
}
```

**Step 7: Run npm install to register the new workspace**

Run: `npm install`
Expected: workspace `@simplemodule/admin` resolved

**Step 8: Commit**

```bash
git add modules/Admin/src/Admin/package.json modules/Admin/src/Admin/vite.config.ts modules/Admin/src/Admin/tsconfig.json modules/Admin/src/Admin/Pages/components/
git commit -m "feat(admin): add frontend setup with TabNav, PermissionGroups, and ActivityTimeline components"
```

---

### Task 10: Users List Page (React)

**Files:**
- Create: `modules/Admin/src/Admin/Pages/Admin/Users.tsx`

**Step 1: Create Users.tsx**

```tsx
import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Input,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { type FormEvent, useState } from 'react';

interface User {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  roles: string[];
  isLockedOut: boolean;
  isDeactivated: boolean;
  createdAt: string;
}

interface Props {
  users: User[];
  search: string;
  page: number;
  totalPages: number;
  totalCount: number;
}

function userStatus(user: User) {
  if (user.isDeactivated) return { label: 'Deactivated', variant: 'secondary' as const };
  if (user.isLockedOut) return { label: 'Locked', variant: 'danger' as const };
  return { label: 'Active', variant: 'success' as const };
}

export default function Users({ users, search, page, totalPages, totalCount }: Props) {
  const [searchValue, setSearchValue] = useState(search);

  function handleSearch(e: FormEvent) {
    e.preventDefault();
    router.get('/admin/users', { search: searchValue, page: 1 }, { preserveState: true });
  }

  function goToPage(p: number) {
    router.get('/admin/users', { search: searchValue, page: p }, { preserveState: true });
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-extrabold tracking-tight">Users</h1>
          <p className="text-text-muted text-sm mt-1">{totalCount} total users</p>
        </div>
        <Button onClick={() => router.get('/admin/users/create')}>Create User</Button>
      </div>

      <form onSubmit={handleSearch} className="mb-6 flex gap-2">
        <Input
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          placeholder="Search by name or email..."
          className="flex-1"
        />
        <Button type="submit">Search</Button>
      </form>

      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            <TableHead>Email</TableHead>
            <TableHead>Roles</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Created</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => {
            const status = userStatus(user);
            return (
              <TableRow key={user.id}>
                <TableCell className="font-medium">{user.displayName || '\u2014'}</TableCell>
                <TableCell className="text-text-secondary">
                  {user.email}
                  {!user.emailConfirmed && (
                    <Badge variant="warning" className="ml-2">unverified</Badge>
                  )}
                </TableCell>
                <TableCell>
                  <div className="flex gap-1 flex-wrap">
                    {user.roles.map((role) => (
                      <Badge key={role} variant="info">{role}</Badge>
                    ))}
                  </div>
                </TableCell>
                <TableCell>
                  <Badge variant={status.variant}>{status.label}</Badge>
                </TableCell>
                <TableCell className="text-sm text-text-muted">
                  {new Date(user.createdAt).toLocaleDateString()}
                </TableCell>
                <TableCell>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => router.get(`/admin/users/${user.id}/edit`)}
                  >
                    Edit
                  </Button>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>

      {totalPages > 1 && (
        <div className="flex justify-center gap-2 mt-6">
          <Button variant="secondary" size="sm" onClick={() => goToPage(page - 1)} disabled={page <= 1}>
            Previous
          </Button>
          <span className="px-3 py-1 text-text-muted text-sm">Page {page} of {totalPages}</span>
          <Button variant="secondary" size="sm" onClick={() => goToPage(page + 1)} disabled={page >= totalPages}>
            Next
          </Button>
        </div>
      )}
    </div>
  );
}
```

**Step 2: Commit**

```bash
git add modules/Admin/src/Admin/Pages/Admin/Users.tsx
git commit -m "feat(admin): add Users list page with search, pagination, status badges, and create button"
```

---

### Task 11: User Create Page (React)

**Files:**
- Create: `modules/Admin/src/Admin/Pages/Admin/UsersCreate.tsx`

**Step 1: Create UsersCreate.tsx**

```tsx
import { router } from '@inertiajs/react';
import { Button, Card, CardContent, Input, Label } from '@simplemodule/ui';

interface Role {
  id: string;
  name: string;
  description: string | null;
}

interface Props {
  allRoles: Role[];
}

export default function UsersCreate({ allRoles }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/admin/users', formData);
  }

  return (
    <div className="max-w-xl">
      <div className="flex items-center gap-3 mb-1">
        <a href="/admin/users" className="text-text-muted hover:text-text transition-colors no-underline">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Create User</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new user account</p>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="displayName">Display Name</Label>
              <Input id="displayName" name="displayName" required />
            </div>
            <div>
              <Label htmlFor="email">Email</Label>
              <Input id="email" name="email" type="email" required />
            </div>
            <div>
              <Label htmlFor="password">Password</Label>
              <Input id="password" name="password" type="password" required />
            </div>
            <div>
              <Label htmlFor="confirmPassword">Confirm Password</Label>
              <Input id="confirmPassword" name="confirmPassword" type="password" required />
            </div>
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                name="emailConfirmed"
                id="emailConfirmed"
                className="h-5 w-5 rounded-md border border-border bg-surface accent-primary"
              />
              <Label htmlFor="emailConfirmed" className="mb-0">Email confirmed</Label>
            </div>

            {allRoles.length > 0 && (
              <div>
                <Label>Roles</Label>
                <div className="space-y-2 mt-1">
                  {allRoles.map((role) => (
                    <div key={role.id} className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        name="roles"
                        value={role.name ?? ''}
                        id={`role-${role.id}`}
                        className="h-5 w-5 rounded-md border border-border bg-surface accent-primary"
                      />
                      <Label htmlFor={`role-${role.id}`} className="mb-0">
                        {role.name}
                        {role.description && (
                          <span className="text-text-muted ml-1">&mdash; {role.description}</span>
                        )}
                      </Label>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <Button type="submit">Create User</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
```

**Step 2: Commit**

```bash
git add modules/Admin/src/Admin/Pages/Admin/UsersCreate.tsx
git commit -m "feat(admin): add User Create page with password, roles, and email confirmed"
```

---

### Task 12: User Edit Page with Tabs (React)

**Files:**
- Create: `modules/Admin/src/Admin/Pages/Admin/UsersEdit.tsx`

**Step 1: Create UsersEdit.tsx**

```tsx
import { router } from '@inertiajs/react';
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, Input, Label } from '@simplemodule/ui';
import { useState } from 'react';
import { ActivityTimeline } from '../components/ActivityTimeline';
import { PermissionGroups } from '../components/PermissionGroups';
import { TabNav } from '../components/TabNav';

interface UserDetail {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  isLockedOut: boolean;
  isDeactivated: boolean;
  accessFailedCount: number;
  createdAt: string;
  lastLoginAt: string | null;
}

interface Role {
  id: string;
  name: string;
  description: string | null;
}

interface ActivityEntry {
  id: number;
  action: string;
  details: string | null;
  performedBy: string;
  timestamp: string;
}

interface Props {
  user: UserDetail;
  userRoles: string[];
  userPermissions: string[];
  allRoles: Role[];
  permissionsByModule: Record<string, string[]>;
  activityLog: ActivityEntry[];
  activityTotal: number;
  tab: string;
}

const tabs = [
  { id: 'details', label: 'Details' },
  { id: 'roles', label: 'Roles & Permissions' },
  { id: 'security', label: 'Security' },
  { id: 'activity', label: 'Activity' },
];

export default function UsersEdit({
  user,
  userRoles,
  userPermissions,
  allRoles,
  permissionsByModule,
  activityLog,
  activityTotal,
  tab,
}: Props) {
  const [activityEntries, setActivityEntries] = useState(activityLog);
  const [activityPage, setActivityPage] = useState(1);

  async function loadMoreActivity() {
    const nextPage = activityPage + 1;
    const res = await fetch(`/admin/users/${user.id}/activity?page=${nextPage}`);
    const data = await res.json();
    setActivityEntries((prev) => [...prev, ...data.entries]);
    setActivityPage(nextPage);
  }

  return (
    <div className="max-w-3xl">
      <div className="flex items-center gap-3 mb-1">
        <a href="/admin/users" className="text-text-muted hover:text-text transition-colors no-underline">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Edit User</h1>
        {user.isDeactivated && <Badge variant="secondary">Deactivated</Badge>}
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Created: {new Date(user.createdAt).toLocaleString()}
        {user.lastLoginAt && (
          <span className="ml-4">Last login: {new Date(user.lastLoginAt).toLocaleString()}</span>
        )}
      </p>

      <TabNav tabs={tabs} activeTab={tab} baseUrl={`/admin/users/${user.id}/edit`} />

      {tab === 'details' && (
        <>
          <Card className="mb-6">
            <CardHeader>
              <CardTitle>Details</CardTitle>
            </CardHeader>
            <CardContent>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  router.post(`/admin/users/${user.id}`, new FormData(e.currentTarget));
                }}
                className="space-y-4"
              >
                <div>
                  <Label htmlFor="displayName">Display Name</Label>
                  <Input id="displayName" name="displayName" defaultValue={user.displayName} />
                </div>
                <div>
                  <Label htmlFor="email">Email</Label>
                  <Input id="email" name="email" type="email" defaultValue={user.email} />
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    name="emailConfirmed"
                    id="emailConfirmed"
                    defaultChecked={user.emailConfirmed}
                    className="h-5 w-5 rounded-md border border-border bg-surface accent-primary"
                  />
                  <Label htmlFor="emailConfirmed" className="mb-0">Email confirmed</Label>
                </div>
                <Button type="submit">Save Details</Button>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Account Status</CardTitle>
            </CardHeader>
            <CardContent>
              {user.isDeactivated ? (
                <div>
                  <p className="text-sm text-text-muted mb-3">This account has been deactivated.</p>
                  <Button variant="outline" onClick={() => router.post(`/admin/users/${user.id}/reactivate`)}>
                    Reactivate Account
                  </Button>
                </div>
              ) : (
                <div>
                  <p className="text-sm text-text-muted mb-3">Deactivating will lock the account and prevent login.</p>
                  <Button variant="danger" onClick={() => {
                    if (confirm('Deactivate this user? They will not be able to log in.'))
                      router.post(`/admin/users/${user.id}/deactivate`);
                  }}>
                    Deactivate Account
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>
        </>
      )}

      {tab === 'roles' && (
        <>
          <Card className="mb-6">
            <CardHeader>
              <CardTitle>Roles</CardTitle>
            </CardHeader>
            <CardContent>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  router.post(`/admin/users/${user.id}/roles`, new FormData(e.currentTarget));
                }}
              >
                <div className="space-y-2 mb-4">
                  {allRoles.map((role) => (
                    <div key={role.id} className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        name="roles"
                        value={role.name ?? ''}
                        id={`role-${role.id}`}
                        defaultChecked={userRoles.includes(role.name ?? '')}
                        className="h-5 w-5 rounded-md border border-border bg-surface accent-primary"
                      />
                      <Label htmlFor={`role-${role.id}`} className="mb-0">
                        {role.name}
                        {role.description && (
                          <span className="text-text-muted ml-1">&mdash; {role.description}</span>
                        )}
                      </Label>
                    </div>
                  ))}
                  {allRoles.length === 0 && (
                    <p className="text-sm text-text-muted">No roles defined.</p>
                  )}
                </div>
                <Button type="submit">Save Roles</Button>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Direct Permissions</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-text-muted mb-4">
                These permissions are granted directly to this user, bypassing role assignments.
              </p>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  router.post(`/admin/users/${user.id}/permissions`, new FormData(e.currentTarget));
                }}
              >
                <PermissionGroups
                  permissionsByModule={permissionsByModule}
                  selected={userPermissions}
                  namePrefix="permissions"
                />
                <Button type="submit" className="mt-4">Save Permissions</Button>
              </form>
            </CardContent>
          </Card>
        </>
      )}

      {tab === 'security' && (
        <>
          <Card className="mb-6">
            <CardHeader>
              <CardTitle>Reset Password</CardTitle>
            </CardHeader>
            <CardContent>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  const formData = new FormData(e.currentTarget);
                  if (formData.get('newPassword') !== formData.get('confirmPassword')) {
                    alert('Passwords do not match.');
                    return;
                  }
                  router.post(`/admin/users/${user.id}/reset-password`, formData);
                }}
                className="space-y-4"
              >
                <div>
                  <Label htmlFor="newPassword">New Password</Label>
                  <Input id="newPassword" name="newPassword" type="password" required />
                </div>
                <div>
                  <Label htmlFor="confirmPassword">Confirm Password</Label>
                  <Input id="confirmPassword" name="confirmPassword" type="password" required />
                </div>
                <Button type="submit">Reset Password</Button>
              </form>
            </CardContent>
          </Card>

          <Card className="mb-6">
            <CardHeader>
              <CardTitle>Account Lock</CardTitle>
            </CardHeader>
            <CardContent>
              {user.isLockedOut ? (
                <div>
                  <p className="text-sm text-danger mb-3">This account is locked.</p>
                  <Button variant="outline" onClick={() => router.post(`/admin/users/${user.id}/unlock`)}>
                    Unlock Account
                  </Button>
                </div>
              ) : (
                <div>
                  <p className="text-sm text-success mb-3">This account is active.</p>
                  <Button variant="danger" onClick={() => router.post(`/admin/users/${user.id}/lock`)}>
                    Lock Account
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>

          <Card className="mb-6">
            <CardHeader>
              <CardTitle>Email Verification</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-text-muted mb-3">
                Status: {user.emailConfirmed ? 'Verified' : 'Not verified'}
              </p>
              {user.emailConfirmed && (
                <Button variant="outline" onClick={() => {
                  if (confirm('Force this user to re-verify their email?'))
                    router.post(`/admin/users/${user.id}/force-reverify`);
                }}>
                  Force Re-verification
                </Button>
              )}
            </CardContent>
          </Card>

          <Card className="mb-6">
            <CardHeader>
              <CardTitle>Two-Factor Authentication</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-text-muted mb-3">
                Status: {user.twoFactorEnabled ? 'Enabled' : 'Not enabled'}
              </p>
              {user.twoFactorEnabled && (
                <Button variant="danger" onClick={() => {
                  if (confirm('Disable 2FA and reset the authenticator for this user?'))
                    router.post(`/admin/users/${user.id}/disable-2fa`);
                }}>
                  Disable 2FA
                </Button>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Login Info</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-text-muted">Failed login attempts:</span>
                  <span className="ml-2 font-medium">{user.accessFailedCount}</span>
                </div>
                <div>
                  <span className="text-text-muted">Last login:</span>
                  <span className="ml-2 font-medium">
                    {user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'Never'}
                  </span>
                </div>
              </div>
            </CardContent>
          </Card>
        </>
      )}

      {tab === 'activity' && (
        <Card>
          <CardHeader>
            <CardTitle>Activity Log</CardTitle>
          </CardHeader>
          <CardContent>
            <ActivityTimeline
              entries={activityEntries}
              total={activityTotal}
              onLoadMore={loadMoreActivity}
            />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
```

**Step 2: Commit**

```bash
git add modules/Admin/src/Admin/Pages/Admin/UsersEdit.tsx
git commit -m "feat(admin): add User Edit page with Details, Roles & Permissions, Security, and Activity tabs"
```

---

### Task 13: Roles List, Create, and Edit Pages (React)

**Files:**
- Create: `modules/Admin/src/Admin/Pages/Admin/Roles.tsx`
- Create: `modules/Admin/src/Admin/Pages/Admin/RolesCreate.tsx`
- Create: `modules/Admin/src/Admin/Pages/Admin/RolesEdit.tsx`

**Step 1: Create Roles.tsx**

```tsx
import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';

interface Role {
  id: string;
  name: string;
  description: string | null;
  userCount: number;
  permissionCount: number;
  createdAt: string;
}

interface Props {
  roles: Role[];
}

export default function Roles({ roles }: Props) {
  function handleDelete(id: string, name: string) {
    if (!confirm(`Delete role "${name}"?`)) return;
    router.delete(`/admin/roles/${id}`, {
      onError: () => alert('Cannot delete role with assigned users.'),
    });
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-extrabold tracking-tight">Roles</h1>
          <p className="text-text-muted text-sm mt-1">Manage application roles</p>
        </div>
        <Button onClick={() => router.get('/admin/roles/create')}>Create Role</Button>
      </div>

      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            <TableHead>Description</TableHead>
            <TableHead>Users</TableHead>
            <TableHead>Permissions</TableHead>
            <TableHead>Created</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {roles.map((role) => (
            <TableRow key={role.id}>
              <TableCell className="font-medium">{role.name}</TableCell>
              <TableCell className="text-text-secondary">{role.description || '\u2014'}</TableCell>
              <TableCell><Badge variant="info">{role.userCount}</Badge></TableCell>
              <TableCell><Badge variant="secondary">{role.permissionCount}</Badge></TableCell>
              <TableCell className="text-sm text-text-muted">
                {new Date(role.createdAt).toLocaleDateString()}
              </TableCell>
              <TableCell>
                <div className="flex gap-3">
                  <Button variant="ghost" size="sm" onClick={() => router.get(`/admin/roles/${role.id}/edit`)}>
                    Edit
                  </Button>
                  <Button variant="danger" size="sm" onClick={() => handleDelete(role.id, role.name)}>
                    Delete
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
```

**Step 2: Create RolesCreate.tsx**

```tsx
import { router } from '@inertiajs/react';
import { Button, Card, CardContent, Input, Label } from '@simplemodule/ui';
import { PermissionGroups } from '../components/PermissionGroups';

interface Props {
  permissionsByModule: Record<string, string[]>;
}

export default function RolesCreate({ permissionsByModule }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/admin/roles', new FormData(e.currentTarget));
  }

  return (
    <div className="max-w-xl">
      <div className="flex items-center gap-3 mb-1">
        <a href="/admin/roles" className="text-text-muted hover:text-text transition-colors no-underline">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Create Role</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new application role with permissions</p>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <Label htmlFor="name">Name</Label>
              <Input id="name" name="name" required />
            </div>
            <div>
              <Label htmlFor="description">Description</Label>
              <Input id="description" name="description" />
            </div>
            <div>
              <Label>Permissions</Label>
              <div className="mt-2">
                <PermissionGroups
                  permissionsByModule={permissionsByModule}
                  selected={[]}
                  namePrefix="permissions"
                />
              </div>
            </div>
            <Button type="submit">Create Role</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
```

**Step 3: Create RolesEdit.tsx**

```tsx
import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Input,
  Label,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { PermissionGroups } from '../components/PermissionGroups';
import { TabNav } from '../components/TabNav';

interface RoleDetail {
  id: string;
  name: string;
  description: string | null;
  createdAt: string;
}

interface UserSummary {
  id: string;
  displayName: string;
  email: string;
}

interface Props {
  role: RoleDetail;
  users: UserSummary[];
  rolePermissions: string[];
  permissionsByModule: Record<string, string[]>;
  tab: string;
}

const tabs = [
  { id: 'details', label: 'Details' },
  { id: 'permissions', label: 'Permissions' },
  { id: 'users', label: 'Users' },
];

export default function RolesEdit({ role, users, rolePermissions, permissionsByModule, tab }: Props) {
  return (
    <div className="max-w-3xl">
      <div className="flex items-center gap-3 mb-1">
        <a href="/admin/roles" className="text-text-muted hover:text-text transition-colors no-underline">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Edit Role</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Created: {new Date(role.createdAt).toLocaleString()}
      </p>

      <TabNav tabs={tabs} activeTab={tab} baseUrl={`/admin/roles/${role.id}/edit`} />

      {tab === 'details' && (
        <Card>
          <CardContent className="p-6">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(`/admin/roles/${role.id}`, new FormData(e.currentTarget));
              }}
              className="space-y-4"
            >
              <div>
                <Label htmlFor="name">Name</Label>
                <Input id="name" name="name" defaultValue={role.name} required />
              </div>
              <div>
                <Label htmlFor="description">Description</Label>
                <Input id="description" name="description" defaultValue={role.description ?? ''} />
              </div>
              <Button type="submit">Save</Button>
            </form>
          </CardContent>
        </Card>
      )}

      {tab === 'permissions' && (
        <Card>
          <CardHeader>
            <CardTitle>Role Permissions</CardTitle>
          </CardHeader>
          <CardContent>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(`/admin/roles/${role.id}/permissions`, new FormData(e.currentTarget));
              }}
            >
              <PermissionGroups
                permissionsByModule={permissionsByModule}
                selected={rolePermissions}
                namePrefix="permissions"
              />
              <Button type="submit" className="mt-4">Save Permissions</Button>
            </form>
          </CardContent>
        </Card>
      )}

      {tab === 'users' && (
        <Card>
          <CardHeader>
            <CardTitle>Assigned Users ({users.length})</CardTitle>
          </CardHeader>
          <CardContent>
            {users.length === 0 ? (
              <p className="text-sm text-text-muted">No users assigned to this role.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Email</TableHead>
                    <TableHead />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.map((u) => (
                    <TableRow key={u.id}>
                      <TableCell className="font-medium">{u.displayName || '\u2014'}</TableCell>
                      <TableCell className="text-text-muted">{u.email}</TableCell>
                      <TableCell>
                        <Button variant="ghost" size="sm" onClick={() => router.get(`/admin/users/${u.id}/edit`)}>
                          Edit
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
```

**Step 4: Commit**

```bash
git add modules/Admin/src/Admin/Pages/Admin/Roles.tsx modules/Admin/src/Admin/Pages/Admin/RolesCreate.tsx modules/Admin/src/Admin/Pages/Admin/RolesEdit.tsx
git commit -m "feat(admin): add Roles list, create (with permissions), and tabbed edit pages"
```

---

### Task 14: Page Registry and Build Verification

**Files:**
- Create: `modules/Admin/src/Admin/Pages/index.ts`

**Step 1: Create Pages/index.ts**

```typescript
import Roles from './Admin/Roles';
import RolesCreate from './Admin/RolesCreate';
import RolesEdit from './Admin/RolesEdit';
import Users from './Admin/Users';
import UsersCreate from './Admin/UsersCreate';
import UsersEdit from './Admin/UsersEdit';

export const pages: Record<string, any> = {
  'Admin/Admin/Users': Users,
  'Admin/Admin/UsersCreate': UsersCreate,
  'Admin/Admin/UsersEdit': UsersEdit,
  'Admin/Admin/Roles': Roles,
  'Admin/Admin/RolesCreate': RolesCreate,
  'Admin/Admin/RolesEdit': RolesEdit,
};
```

**Step 2: Build frontend**

Run: `npm run build --workspace=@simplemodule/admin`
Expected: Build succeeds, `Admin.pages.js` created in `modules/Admin/src/Admin/wwwroot/`

**Step 3: Build full solution**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add modules/Admin/src/Admin/Pages/index.ts
git commit -m "feat(admin): add page registry and verify full build"
```

---

### Task 15: Admin Tests Scaffold

**Files:**
- Create: `modules/Admin/tests/Admin.Tests/Admin.Tests.csproj`
- Create: `modules/Admin/tests/Admin.Tests/Integration/AdminUsersEndpointTests.cs`
- Create: `modules/Admin/tests/Admin.Tests/Integration/AdminRolesEndpointTests.cs`

**Step 1: Create Admin.Tests.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <ProjectReference Include="..\..\src\Admin\Admin.csproj" />
    <ProjectReference Include="..\..\..\..\tests\SimpleModule.Tests.Shared\SimpleModule.Tests.Shared.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Create AdminUsersEndpointTests.cs**

Write integration tests covering:
- GET `/admin/users` — returns user list (authenticated as Admin role)
- GET `/admin/users` — returns 401/403 for non-admin
- GET `/admin/users?search=test` — filters results
- GET `/admin/users/create` — returns create form view
- POST `/admin/users` — creates user
- GET `/admin/users/{id}/edit` — returns edit view with tabs
- POST `/admin/users/{id}` — updates user details
- POST `/admin/users/{id}/roles` — sets user roles
- POST `/admin/users/{id}/permissions` — sets user permissions
- POST `/admin/users/{id}/lock` — locks account
- POST `/admin/users/{id}/unlock` — unlocks account
- POST `/admin/users/{id}/reset-password` — resets password
- POST `/admin/users/{id}/deactivate` — soft deletes
- POST `/admin/users/{id}/reactivate` — restores

Each test should use `SimpleModuleWebApplicationFactory`, `CreateAuthenticatedClient` with Admin role claim, and seed test users via `UserManager` in a scope.

**Step 3: Create AdminRolesEndpointTests.cs**

Write integration tests covering:
- GET `/admin/roles` — returns role list
- GET `/admin/roles/create` — returns create form with permissions
- POST `/admin/roles` — creates role with permissions
- GET `/admin/roles/{id}/edit` — returns edit view with tabs
- POST `/admin/roles/{id}` — updates role details
- POST `/admin/roles/{id}/permissions` — sets role permissions
- DELETE `/admin/roles/{id}` — deletes role (and blocked when users assigned)

**Step 4: Run tests**

Run: `dotnet test modules/Admin/tests/Admin.Tests/`
Expected: All tests pass

**Step 5: Commit**

```bash
git add modules/Admin/tests/
git commit -m "test(admin): add integration tests for user and role admin endpoints"
```

---

### Task 16: Final Verification and Cleanup

**Step 1: Run full test suite**

Run: `dotnet test`
Expected: All tests pass (including existing tests that may have referenced old admin endpoints)

**Step 2: Run frontend lint**

Run: `npm run check`
Expected: No lint errors

**Step 3: Run the app and verify**

Run: `dotnet run --project template/SimpleModule.Host`
Visit `https://localhost:5001/admin/users` — should render the new Admin module's Users page
Visit `https://localhost:5001/admin/roles` — should render the new Admin module's Roles page

**Step 4: Commit any final fixes**

```bash
git add -A
git commit -m "chore(admin): final cleanup and verification"
```
