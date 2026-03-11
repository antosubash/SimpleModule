# User & Role Management Admin UI Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build React admin pages for managing users (list, edit, lock/unlock, role assignment) and roles (CRUD), served via Inertia from the Users module, gated with Admin role.

**Architecture:** Admin endpoints in `Features/Admin/` using `UserManager`/`RoleManager` directly. React pages in `Pages/Admin/`. Inertia protocol handles server→client data passing. Form submissions POST back to endpoints which redirect via Inertia.

**Tech Stack:** ASP.NET Core Identity, Inertia.js, React 19, Vite, Tailwind CSS

---

### Task 1: Users module Vite config for React pages

The Users module already has a Vite config for building `Scripts/index.ts` → `Users.lib.module.js`. We need a second build for React pages → `Users.pages.js`.

**Files:**
- Modify: `src/modules/Users/src/Users/vite.config.ts`
- Modify: `src/modules/Users/src/Users/package.json`
- Modify: `src/modules/Users/src/Users/Users.csproj`

**Step 1: Update vite.config.ts to support two builds**

Replace `src/modules/Users/src/Users/vite.config.ts` with:

```ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

const buildTarget = process.env.VITE_BUILD_TARGET;

const libConfig = defineConfig({
  build: {
    lib: {
      entry: resolve(__dirname, 'Scripts/index.ts'),
      formats: ['es'],
      fileName: () => 'Users.lib.module.js',
    },
    outDir: 'wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      output: { inlineDynamicImports: true },
    },
  },
});

const pagesConfig = defineConfig({
  plugins: [react()],
  build: {
    lib: {
      entry: resolve(__dirname, 'Pages/index.ts'),
      formats: ['es'],
      fileName: () => 'Users.pages.js',
    },
    outDir: 'wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      external: ['react', 'react-dom', 'react/jsx-runtime'],
      output: { inlineDynamicImports: true },
    },
  },
});

export default buildTarget === 'pages' ? pagesConfig : libConfig;
```

**Step 2: Update package.json to add react peer deps and dual build script**

Replace `src/modules/Users/src/Users/package.json` with:

```json
{
  "private": true,
  "name": "@simplemodule/users",
  "scripts": {
    "build": "vite build && cross-env VITE_BUILD_TARGET=pages vite build",
    "build:lib": "vite build",
    "build:pages": "cross-env VITE_BUILD_TARGET=pages vite build",
    "watch": "cross-env VITE_BUILD_TARGET=pages vite build --watch"
  },
  "dependencies": {
    "qrcode": "^1.5.4"
  },
  "devDependencies": {
    "@types/qrcode": "^1.5.6"
  },
  "peerDependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  }
}
```

**Step 3: Update Users.csproj JsBuild target to run both builds**

In `src/modules/Users/src/Users/Users.csproj`, update the JsBuild target Exec command:

```xml
<Exec Command="npx vite build &amp;&amp; npx cross-env VITE_BUILD_TARGET=pages npx vite build" WorkingDirectory="$(MSBuildProjectDirectory)" />
```

**Step 4: Install cross-env in root devDependencies**

Run: `npm install -D cross-env`

**Step 5: Verify builds**

Run from `src/modules/Users/src/Users/`:
```bash
npx vite build
VITE_BUILD_TARGET=pages npx vite build
```
Expected: `wwwroot/Users.lib.module.js` and `wwwroot/Users.pages.js` both exist.

**Step 6: Commit**

```bash
git add src/modules/Users/src/Users/vite.config.ts src/modules/Users/src/Users/package.json src/modules/Users/src/Users/Users.csproj package.json package-lock.json
git commit -m "feat(users): configure dual Vite builds for lib + React pages"
```

---

### Task 2: Admin Users endpoint (C# backend)

**Files:**
- Create: `src/modules/Users/src/Users/Features/Admin/AdminUsersEndpoint.cs`
- Modify: `src/modules/Users/src/Users/UsersModule.cs`

**Step 1: Create AdminUsersEndpoint.cs**

Create `src/modules/Users/src/Users/Features/Admin/AdminUsersEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Features.Admin;

public static class AdminUsersEndpoint
{
    private const int PageSize = 20;

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/admin/users")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // List users
        group.MapGet("/", async (
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            string? search,
            int page = 1) =>
        {
            var query = userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(term))
                    || u.DisplayName.ToLower().Contains(term)
                    || (u.UserName != null && u.UserName.ToLower().Contains(term)));
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
                    isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                    createdAt = user.CreatedAt.ToString("O"),
                });
            }

            return Inertia.Render("Users/Admin/Users", new
            {
                users = userList,
                search = search ?? "",
                page,
                totalPages,
                totalCount,
            });
        });

        // Edit user page
        group.MapGet("/{id}/edit", async (
            string id,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            var userRoles = await userManager.GetRolesAsync(user);
            var allRoles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

            return Inertia.Render("Users/Admin/UsersEdit", new
            {
                user = new
                {
                    id = user.Id,
                    displayName = user.DisplayName,
                    email = user.Email,
                    emailConfirmed = user.EmailConfirmed,
                    isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                    createdAt = user.CreatedAt.ToString("O"),
                    lastLoginAt = user.LastLoginAt?.ToString("O"),
                },
                userRoles = userRoles.ToList(),
                allRoles = allRoles.Select(r => new { id = r.Id, name = r.Name, description = r.Description }).ToList(),
            });
        });

        // Update user
        group.MapPost("/{id}", async (
            string id,
            HttpContext context,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            var form = await context.Request.ReadFormAsync();
            user.DisplayName = form["displayName"].ToString();
            user.Email = form["email"].ToString();
            user.EmailConfirmed = form.ContainsKey("emailConfirmed");

            await userManager.UpdateAsync(user);

            return Results.Redirect($"/admin/users/{id}/edit");
        });

        // Set roles
        group.MapPost("/{id}/roles", async (
            string id,
            HttpContext context,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            var form = await context.Request.ReadFormAsync();
            var newRoles = form["roles"].ToArray().Where(r => !string.IsNullOrEmpty(r)).ToList();
            var currentRoles = await userManager.GetRolesAsync(user);

            await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (newRoles.Count > 0)
                await userManager.AddToRolesAsync(user, newRoles!);

            return Results.Redirect($"/admin/users/{id}/edit");
        });

        // Lock account
        group.MapPost("/{id}/lock", async (
            string id,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            await userManager.SetLockoutEnabledAsync(user, true);
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

            return Results.Redirect($"/admin/users/{id}/edit");
        });

        // Unlock account
        group.MapPost("/{id}/unlock", async (
            string id,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Results.NotFound();

            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);

            return Results.Redirect($"/admin/users/{id}/edit");
        });
    }
}
```

**Step 2: Register in UsersModule.cs**

Add to the using block:
```csharp
using SimpleModule.Users.Features.Admin;
```

Add at the end of `ConfigureEndpoints`, before the closing brace:
```csharp
        // Admin endpoints
        AdminUsersEndpoint.Map(endpoints);
```

**Step 3: Verify dotnet build succeeds**

Run: `dotnet build`
Expected: 0 errors

**Step 4: Commit**

```bash
git add src/modules/Users/src/Users/Features/Admin/AdminUsersEndpoint.cs src/modules/Users/src/Users/UsersModule.cs
git commit -m "feat(users): add admin users management endpoints"
```

---

### Task 3: Admin Roles endpoint (C# backend)

**Files:**
- Create: `src/modules/Users/src/Users/Features/Admin/AdminRolesEndpoint.cs`
- Modify: `src/modules/Users/src/Users/UsersModule.cs`

**Step 1: Create AdminRolesEndpoint.cs**

Create `src/modules/Users/src/Users/Features/Admin/AdminRolesEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Features.Admin;

public static class AdminRolesEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/admin/roles")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // List roles
        group.MapGet("/", async (
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager) =>
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
                roleList.Add(new
                {
                    id = role.Id,
                    name = role.Name,
                    description = role.Description,
                    userCount = usersInRole.Count,
                    createdAt = role.CreatedAt.ToString("O"),
                });
            }

            return Inertia.Render("Users/Admin/Roles", new { roles = roleList });
        });

        // Create role page
        group.MapGet("/create", () =>
            Inertia.Render("Users/Admin/RolesCreate"));

        // Create role
        group.MapPost("/", async (
            HttpContext context,
            RoleManager<ApplicationRole> roleManager) =>
        {
            var form = await context.Request.ReadFormAsync();
            var name = form["name"].ToString().Trim();
            var description = form["description"].ToString().Trim();

            if (string.IsNullOrEmpty(name))
                return Results.Redirect("/admin/roles/create");

            var role = new ApplicationRole
            {
                Name = name,
                Description = string.IsNullOrEmpty(description) ? null : description,
            };

            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
                return Results.Redirect("/admin/roles/create");

            return Results.Redirect("/admin/roles");
        });

        // Edit role page
        group.MapGet("/{id}/edit", async (
            string id,
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager) =>
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role is null)
                return Results.NotFound();

            var usersInRole = role.Name is not null
                ? await userManager.GetUsersInRoleAsync(role.Name)
                : [];

            return Inertia.Render("Users/Admin/RolesEdit", new
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
            });
        });

        // Update role
        group.MapPost("/{id}", async (
            string id,
            HttpContext context,
            RoleManager<ApplicationRole> roleManager) =>
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role is null)
                return Results.NotFound();

            var form = await context.Request.ReadFormAsync();
            role.Name = form["name"].ToString().Trim();
            var description = form["description"].ToString().Trim();
            role.Description = string.IsNullOrEmpty(description) ? null : description;

            await roleManager.UpdateAsync(role);

            return Results.Redirect($"/admin/roles/{id}/edit");
        });

        // Delete role
        group.MapDelete("/{id}", async (
            string id,
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager) =>
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role is null)
                return Results.NotFound();

            // Don't delete if users are assigned
            var usersInRole = role.Name is not null
                ? await userManager.GetUsersInRoleAsync(role.Name)
                : [];
            if (usersInRole.Count > 0)
                return Results.BadRequest(new { error = "Cannot delete role with assigned users" });

            await roleManager.DeleteAsync(role);

            return Results.Ok();
        });
    }
}
```

**Step 2: Register in UsersModule.cs**

Add after `AdminUsersEndpoint.Map(endpoints);`:
```csharp
        AdminRolesEndpoint.Map(endpoints);
```

**Step 3: Verify dotnet build succeeds**

Run: `dotnet build`
Expected: 0 errors

**Step 4: Commit**

```bash
git add src/modules/Users/src/Users/Features/Admin/AdminRolesEndpoint.cs src/modules/Users/src/Users/UsersModule.cs
git commit -m "feat(users): add admin roles management endpoints"
```

---

### Task 4: React pages — Users list and Users edit

**Files:**
- Create: `src/modules/Users/src/Users/Pages/Admin/Users.tsx`
- Create: `src/modules/Users/src/Users/Pages/Admin/UsersEdit.tsx`

**Step 1: Create Users list page**

Create `src/modules/Users/src/Users/Pages/Admin/Users.tsx`:

```tsx
import { router } from '@inertiajs/react';
import { useState, FormEvent } from 'react';

interface User {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  roles: string[];
  isLockedOut: boolean;
  createdAt: string;
}

interface Props {
  users: User[];
  search: string;
  page: number;
  totalPages: number;
  totalCount: number;
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
    <div className="max-w-6xl mx-auto p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Users</h1>
        <span className="text-gray-500">{totalCount} total</span>
      </div>

      <form onSubmit={handleSearch} className="mb-6 flex gap-2">
        <input
          type="text"
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          placeholder="Search by name or email..."
          className="flex-1 px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <button
          type="submit"
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          Search
        </button>
      </form>

      <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
        <table className="w-full text-left">
          <thead className="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Name</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Email</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Roles</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Status</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Created</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {users.map((user) => (
              <tr key={user.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                <td className="px-4 py-3 font-medium">{user.displayName || '—'}</td>
                <td className="px-4 py-3 text-gray-600 dark:text-gray-400">
                  {user.email}
                  {!user.emailConfirmed && (
                    <span className="ml-2 text-xs text-amber-600 dark:text-amber-400">unverified</span>
                  )}
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-1 flex-wrap">
                    {user.roles.map((role) => (
                      <span
                        key={role}
                        className="px-2 py-0.5 text-xs rounded-full bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300"
                      >
                        {role}
                      </span>
                    ))}
                  </div>
                </td>
                <td className="px-4 py-3">
                  {user.isLockedOut ? (
                    <span className="text-red-600 dark:text-red-400 text-sm">Locked</span>
                  ) : (
                    <span className="text-green-600 dark:text-green-400 text-sm">Active</span>
                  )}
                </td>
                <td className="px-4 py-3 text-sm text-gray-500">
                  {new Date(user.createdAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3">
                  <button
                    onClick={() => router.get(`/admin/users/${user.id}/edit`)}
                    className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 text-sm"
                  >
                    Edit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex justify-center gap-2 mt-6">
          <button
            onClick={() => goToPage(page - 1)}
            disabled={page <= 1}
            className="px-3 py-1 rounded border border-gray-300 dark:border-gray-600 disabled:opacity-50"
          >
            Previous
          </button>
          <span className="px-3 py-1 text-gray-600 dark:text-gray-400">
            Page {page} of {totalPages}
          </span>
          <button
            onClick={() => goToPage(page + 1)}
            disabled={page >= totalPages}
            className="px-3 py-1 rounded border border-gray-300 dark:border-gray-600 disabled:opacity-50"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
```

**Step 2: Create Users edit page**

Create `src/modules/Users/src/Users/Pages/Admin/UsersEdit.tsx`:

```tsx
import { router } from '@inertiajs/react';

interface UserDetail {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  isLockedOut: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

interface Role {
  id: string;
  name: string;
  description: string | null;
}

interface Props {
  user: UserDetail;
  userRoles: string[];
  allRoles: Role[];
}

export default function UsersEdit({ user, userRoles, allRoles }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/admin/users/${user.id}`, formData);
  }

  function handleRolesSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/admin/users/${user.id}/roles`, formData);
  }

  function handleLock() {
    router.post(`/admin/users/${user.id}/lock`);
  }

  function handleUnlock() {
    router.post(`/admin/users/${user.id}/unlock`);
  }

  return (
    <div className="max-w-3xl mx-auto p-8">
      <div className="flex items-center gap-4 mb-6">
        <button
          onClick={() => router.get('/admin/users')}
          className="text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
        >
          &larr; Back
        </button>
        <h1 className="text-3xl font-bold">Edit User</h1>
      </div>

      <div className="mb-6 text-sm text-gray-500 dark:text-gray-400">
        <span>Created: {new Date(user.createdAt).toLocaleString()}</span>
        {user.lastLoginAt && (
          <span className="ml-4">Last login: {new Date(user.lastLoginAt).toLocaleString()}</span>
        )}
      </div>

      {/* User Details Form */}
      <form onSubmit={handleSubmit} className="mb-8 p-6 rounded-lg border border-gray-200 dark:border-gray-700">
        <h2 className="text-lg font-semibold mb-4">Details</h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Display Name</label>
            <input
              type="text"
              name="displayName"
              defaultValue={user.displayName}
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Email</label>
            <input
              type="email"
              name="email"
              defaultValue={user.email}
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              name="emailConfirmed"
              id="emailConfirmed"
              defaultChecked={user.emailConfirmed}
              className="rounded border-gray-300"
            />
            <label htmlFor="emailConfirmed" className="text-sm">Email confirmed</label>
          </div>
          <button
            type="submit"
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Save Details
          </button>
        </div>
      </form>

      {/* Roles Form */}
      <form onSubmit={handleRolesSubmit} className="mb-8 p-6 rounded-lg border border-gray-200 dark:border-gray-700">
        <h2 className="text-lg font-semibold mb-4">Roles</h2>
        <div className="space-y-2 mb-4">
          {allRoles.map((role) => (
            <div key={role.id} className="flex items-center gap-2">
              <input
                type="checkbox"
                name="roles"
                value={role.name ?? ''}
                id={`role-${role.id}`}
                defaultChecked={userRoles.includes(role.name ?? '')}
                className="rounded border-gray-300"
              />
              <label htmlFor={`role-${role.id}`} className="text-sm">
                {role.name}
                {role.description && (
                  <span className="text-gray-500 ml-1">— {role.description}</span>
                )}
              </label>
            </div>
          ))}
          {allRoles.length === 0 && (
            <p className="text-sm text-gray-500">No roles defined.</p>
          )}
        </div>
        <button
          type="submit"
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          Save Roles
        </button>
      </form>

      {/* Lock/Unlock */}
      <div className="p-6 rounded-lg border border-gray-200 dark:border-gray-700">
        <h2 className="text-lg font-semibold mb-4">Account Status</h2>
        {user.isLockedOut ? (
          <div>
            <p className="text-sm text-red-600 dark:text-red-400 mb-3">This account is locked.</p>
            <button
              onClick={handleUnlock}
              className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700"
            >
              Unlock Account
            </button>
          </div>
        ) : (
          <div>
            <p className="text-sm text-green-600 dark:text-green-400 mb-3">This account is active.</p>
            <button
              onClick={handleLock}
              className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700"
            >
              Lock Account
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
```

**Step 3: Verify TypeScript compiles**

Run from repo root: `npx tsc --noEmit`
Expected: No errors

**Step 4: Commit**

```bash
git add src/modules/Users/src/Users/Pages/Admin/Users.tsx src/modules/Users/src/Users/Pages/Admin/UsersEdit.tsx
git commit -m "feat(users): add admin users list and edit React pages"
```

---

### Task 5: React pages — Roles list, create, edit

**Files:**
- Create: `src/modules/Users/src/Users/Pages/Admin/Roles.tsx`
- Create: `src/modules/Users/src/Users/Pages/Admin/RolesCreate.tsx`
- Create: `src/modules/Users/src/Users/Pages/Admin/RolesEdit.tsx`

**Step 1: Create Roles list page**

Create `src/modules/Users/src/Users/Pages/Admin/Roles.tsx`:

```tsx
import { router } from '@inertiajs/react';

interface Role {
  id: string;
  name: string;
  description: string | null;
  userCount: number;
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
    <div className="max-w-4xl mx-auto p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Roles</h1>
        <button
          onClick={() => router.get('/admin/roles/create')}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          Create Role
        </button>
      </div>

      <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
        <table className="w-full text-left">
          <thead className="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Name</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Description</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Users</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Created</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {roles.map((role) => (
              <tr key={role.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                <td className="px-4 py-3 font-medium">{role.name}</td>
                <td className="px-4 py-3 text-gray-600 dark:text-gray-400">
                  {role.description || '—'}
                </td>
                <td className="px-4 py-3">
                  <span className="px-2 py-0.5 text-xs rounded-full bg-gray-100 dark:bg-gray-800">
                    {role.userCount}
                  </span>
                </td>
                <td className="px-4 py-3 text-sm text-gray-500">
                  {new Date(role.createdAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-3">
                    <button
                      onClick={() => router.get(`/admin/roles/${role.id}/edit`)}
                      className="text-blue-600 hover:text-blue-800 dark:text-blue-400 text-sm"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDelete(role.id, role.name)}
                      className="text-red-600 hover:text-red-800 dark:text-red-400 text-sm"
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
```

**Step 2: Create Roles create page**

Create `src/modules/Users/src/Users/Pages/Admin/RolesCreate.tsx`:

```tsx
import { router } from '@inertiajs/react';

export default function RolesCreate() {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/admin/roles', formData);
  }

  return (
    <div className="max-w-xl mx-auto p-8">
      <div className="flex items-center gap-4 mb-6">
        <button
          onClick={() => router.get('/admin/roles')}
          className="text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
        >
          &larr; Back
        </button>
        <h1 className="text-3xl font-bold">Create Role</h1>
      </div>

      <form onSubmit={handleSubmit} className="p-6 rounded-lg border border-gray-200 dark:border-gray-700">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Name</label>
            <input
              type="text"
              name="name"
              required
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Description</label>
            <input
              type="text"
              name="description"
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <button
            type="submit"
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Create
          </button>
        </div>
      </form>
    </div>
  );
}
```

**Step 3: Create Roles edit page**

Create `src/modules/Users/src/Users/Pages/Admin/RolesEdit.tsx`:

```tsx
import { router } from '@inertiajs/react';

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
}

export default function RolesEdit({ role, users }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/admin/roles/${role.id}`, formData);
  }

  return (
    <div className="max-w-xl mx-auto p-8">
      <div className="flex items-center gap-4 mb-6">
        <button
          onClick={() => router.get('/admin/roles')}
          className="text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
        >
          &larr; Back
        </button>
        <h1 className="text-3xl font-bold">Edit Role</h1>
      </div>

      <form onSubmit={handleSubmit} className="mb-8 p-6 rounded-lg border border-gray-200 dark:border-gray-700">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Name</label>
            <input
              type="text"
              name="name"
              defaultValue={role.name}
              required
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Description</label>
            <input
              type="text"
              name="description"
              defaultValue={role.description ?? ''}
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div className="text-sm text-gray-500">
            Created: {new Date(role.createdAt).toLocaleString()}
          </div>
          <button
            type="submit"
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Save
          </button>
        </div>
      </form>

      <div className="p-6 rounded-lg border border-gray-200 dark:border-gray-700">
        <h2 className="text-lg font-semibold mb-4">Assigned Users ({users.length})</h2>
        {users.length === 0 ? (
          <p className="text-sm text-gray-500">No users assigned to this role.</p>
        ) : (
          <ul className="space-y-2">
            {users.map((user) => (
              <li key={user.id} className="flex justify-between items-center py-2">
                <div>
                  <span className="font-medium">{user.displayName || '—'}</span>
                  <span className="text-gray-500 ml-2 text-sm">{user.email}</span>
                </div>
                <button
                  onClick={() => router.get(`/admin/users/${user.id}/edit`)}
                  className="text-blue-600 hover:text-blue-800 dark:text-blue-400 text-sm"
                >
                  Edit
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
```

**Step 4: Commit**

```bash
git add src/modules/Users/src/Users/Pages/Admin/
git commit -m "feat(users): add admin roles list, create, and edit React pages"
```

---

### Task 6: Page registry and final build verification

**Files:**
- Create: `src/modules/Users/src/Users/Pages/index.ts`

**Step 1: Create page registry**

Create `src/modules/Users/src/Users/Pages/index.ts`:

```ts
import Users from './Admin/Users';
import UsersEdit from './Admin/UsersEdit';
import Roles from './Admin/Roles';
import RolesCreate from './Admin/RolesCreate';
import RolesEdit from './Admin/RolesEdit';

export const pages: Record<string, any> = {
  'Users/Admin/Users': Users,
  'Users/Admin/UsersEdit': UsersEdit,
  'Users/Admin/Roles': Roles,
  'Users/Admin/RolesCreate': RolesCreate,
  'Users/Admin/RolesEdit': RolesEdit,
};
```

**Step 2: Run full build**

```bash
npm install
dotnet build
```
Expected: 0 errors. Vite builds produce `Users.lib.module.js` and `Users.pages.js` in `wwwroot/`.

**Step 3: Commit**

```bash
git add src/modules/Users/src/Users/Pages/index.ts
git commit -m "feat(users): add page registry for admin React pages"
```

---

## Summary

| Task | Description |
|------|-------------|
| 1 | Dual Vite config (lib + pages builds) |
| 2 | Admin users endpoints (list, edit, update, roles, lock/unlock) |
| 3 | Admin roles endpoints (list, create, edit, update, delete) |
| 4 | React pages: Users list + Users edit |
| 5 | React pages: Roles list, create, edit |
| 6 | Page registry + final build verification |
