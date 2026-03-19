# OpenIddict Table Ownership + Client Management UI — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix OpenIddict table prefixes to use `OpenIddict_` instead of `Users_`, and add a full CRUD UI for managing OAuth2 clients in the OpenIddict module.

**Architecture:** Update the source generator's HostDbContextEmitter to recognize OpenIddict entities (added implicitly by `UseOpenIddict()`) and assign them the correct module prefix. Then add view endpoints, action endpoints, and React pages to the OpenIddict module following the exact Admin module patterns.

**Tech Stack:** .NET 10, Roslyn source generators, OpenIddict EF Core, React 19, Inertia.js, Vite (library mode), @simplemodule/ui components.

---

### Task 1: Fix Source Generator — OpenIddict Table Prefix

**Files:**
- Modify: `framework/SimpleModule.Generator/Emitters/HostDbContextEmitter.cs`

The Identity table prefix loop (lines 240-268) catches all "unprefixed" entities — including OpenIddict entities — and assigns them `Users_`. OpenIddict entities are added implicitly by `UseOpenIddict()` on the HostDbContext options, not via DbSet properties.

The fix: the generator already emits `alreadyPrefixed` checks for each module. OpenIddict is already listed (line 100 in generated output: `if (tableName.StartsWith("OpenIddict_", ...)) alreadyPrefixed = true;`). But since OpenIddict entities have no DbSet in ANY module's DbContext, they never get their own prefix — they just get caught by the fallback Identity prefix.

**Solution:** After the Identity table prefix loop, add an explicit prefix pass for any remaining "OpenIddict*" tables. In the emitter, after the `alreadyPrefixed` block and before closing the `else` block, emit code that re-prefixes any Identity-prefixed OpenIddict tables:

Actually, the cleaner approach: In the Identity table prefix loop, detect entities whose table name contains "OpenIddict" and prefix them with `OpenIddict_` instead of the Identity prefix. Modify lines 240-268 in the emitter.

**Step 1: Modify HostDbContextEmitter.cs**

In the Identity table prefix section (SQLite), change the logic to:
1. If the table name starts with "OpenIddict", prefix with `OpenIddict_` (not the Identity prefix)
2. Otherwise, prefix with the Identity module prefix as before

Replace the Identity table prefix loop code (lines 240-268) with:

```csharp
// For Identity and OpenIddict tables in SQLite, prefix them appropriately
if (identityContext != null)
{
    var identityPrefix = identityContext.Value.ModuleName + "_";
    sb.AppendLine();
    sb.AppendLine("            // Prefix Identity and OpenIddict tables for SQLite");
    sb.AppendLine("            foreach (var entityType in builder.Model.GetEntityTypes())");
    sb.AppendLine("            {");
    sb.AppendLine("                var tableName = entityType.GetTableName();");
    sb.AppendLine(
        $"                if (tableName is not null && !tableName.StartsWith(\"{identityPrefix}\", global::System.StringComparison.Ordinal))"
    );

    sb.AppendLine("                {");
    sb.AppendLine("                    var alreadyPrefixed = false;");
    foreach (var ctx2 in data.DbContexts)
    {
        if (ctx2.FullyQualifiedName == identityContext.Value.FullyQualifiedName)
            continue;
        sb.AppendLine(
            $"                    if (tableName.StartsWith(\"{ctx2.ModuleName}_\", global::System.StringComparison.Ordinal)) alreadyPrefixed = true;"
        );
    }

    // OpenIddict tables get their own prefix instead of being lumped with Identity
    sb.AppendLine(
        "                    if (!alreadyPrefixed && tableName.StartsWith(\"OpenIddict\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("                    {");
    sb.AppendLine("                        entityType.SetTableName(\"OpenIddict_\" + tableName);");
    sb.AppendLine("                        alreadyPrefixed = true;");
    sb.AppendLine("                    }");

    sb.AppendLine(
        $"                    if (!alreadyPrefixed) entityType.SetTableName(\"{identityPrefix}\" + tableName);"
    );
    sb.AppendLine("                }");
    sb.AppendLine("            }");
}
```

Also do the same for the non-SQLite schema section — add OpenIddict schema assignment. After the Identity schema loop (lines 203-215), add:

```csharp
sb.AppendLine();
sb.AppendLine("            // Assign OpenIddict tables to the openiddict schema");
sb.AppendLine("            foreach (var entityType in builder.Model.GetEntityTypes())");
sb.AppendLine("            {");
sb.AppendLine("                var tableName = entityType.GetTableName();");
sb.AppendLine("                if (tableName is not null && tableName.StartsWith(\"OpenIddict\", global::System.StringComparison.Ordinal))");
sb.AppendLine("                    entityType.SetSchema(\"openiddict\");");
sb.AppendLine("            }");
```

**Step 2: Build and verify**

Run: `dotnet build`
Expected: 0 warnings, 0 errors.

Check the generated file:
```bash
cat template/SimpleModule.Host/obj/Debug/net10.0/generated/SimpleModule.Generator/SimpleModule.Generator.ModuleDiscovererGenerator/HostDbContext.g.cs
```
Verify OpenIddict tables get `OpenIddict_` prefix.

**Step 3: Update generator tests**

Update any tests in `tests/SimpleModule.Generator.Tests/` that verify the generated HostDbContext table prefixes. The `EndpointExtensionsEmitterTests` don't test this — check if there are `HostDbContextEmitterTests`.

**Step 4: Reset migration**

```bash
rm template/SimpleModule.Host/Migrations/*
rm template/SimpleModule.Host/app.db
dotnet ef migrations add InitialCreate --project template/SimpleModule.Host --context HostDbContext
```

Verify the migration creates tables with `OpenIddict_` prefix (e.g., `OpenIddict_OpenIddictApplications`).

**Step 5: Verify app starts**

```bash
dotnet run --project template/SimpleModule.Host --no-launch-profile --urls "https://localhost:5001"
```
Check health: `curl -sk https://localhost:5001/health/live`

**Step 6: Commit**

```bash
git add -A
git commit -m "fix(generator): assign OpenIddict tables their own schema/prefix"
```

---

### Task 2: Add OpenIddict Permissions and Menu

**Files:**
- Create: `modules/OpenIddict/src/OpenIddict.Contracts/OpenIddictPermissions.cs`
- Modify: `modules/OpenIddict/src/OpenIddict/OpenIddictModule.cs` — add ConfigurePermissions and ConfigureMenu

**Step 1: Create OpenIddictPermissions**

```csharp
namespace SimpleModule.OpenIddict.Contracts;

public sealed class OpenIddictPermissions
{
    public const string ManageClients = "OpenIddict.ManageClients";
}
```

**Step 2: Update OpenIddictModule**

Add `ConfigurePermissions` and `ConfigureMenu`:

```csharp
public void ConfigurePermissions(PermissionRegistryBuilder builder)
{
    builder.AddPermissions<OpenIddictPermissions>();
}

public void ConfigureMenu(IMenuBuilder menus)
{
    menus.Add(new MenuItem
    {
        Label = "OAuth Clients",
        Url = "/openiddict/clients",
        Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"/></svg>""",
        Order = 22,
        Section = MenuSection.Navbar,
    });
}
```

Add required usings: `SimpleModule.Core.Authorization`, `SimpleModule.Core.Menu`, `SimpleModule.OpenIddict.Contracts`.

**Step 3: Build and commit**

```bash
dotnet build modules/OpenIddict/src/OpenIddict/OpenIddict.csproj
git add modules/OpenIddict/
git commit -m "feat(openiddict): add ManageClients permission and menu item"
```

---

### Task 3: Create Client View Endpoints

**Files:**
- Create: `modules/OpenIddict/src/OpenIddict/Views/OpenIddict/ClientsEndpoint.cs`
- Create: `modules/OpenIddict/src/OpenIddict/Views/OpenIddict/ClientsCreateEndpoint.cs`
- Create: `modules/OpenIddict/src/OpenIddict/Views/OpenIddict/ClientsEditEndpoint.cs`

Follow the Admin module's `RolesEndpoint`/`RolesCreateEndpoint`/`RolesEditEndpoint` pattern exactly.

**Step 1: Create ClientsEndpoint (list page)**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.OpenIddict.Views.OpenIddict;

public class ClientsEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/openiddict/clients",
            async (IOpenIddictApplicationManager manager) =>
            {
                var clients = new List<object>();

                await foreach (var app in manager.ListAsync())
                {
                    clients.Add(new
                    {
                        id = await manager.GetIdAsync(app),
                        clientId = await manager.GetClientIdAsync(app),
                        displayName = await manager.GetDisplayNameAsync(app),
                        clientType = await manager.GetClientTypeAsync(app),
                    });
                }

                return Inertia.Render("OpenIddict/OpenIddict/Clients", new { clients });
            }
        ).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

**Step 2: Create ClientsCreateEndpoint**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.OpenIddict.Views.OpenIddict;

public class ClientsCreateEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/openiddict/clients/create",
            () => Inertia.Render("OpenIddict/OpenIddict/ClientsCreate", new { })
        ).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

**Step 3: Create ClientsEditEndpoint**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;

namespace SimpleModule.OpenIddict.Views.OpenIddict;

public class ClientsEditEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/openiddict/clients/{id}/edit",
            async (string id, IOpenIddictApplicationManager manager, string? tab) =>
            {
                var application = await manager.FindByIdAsync(id);
                if (application is null)
                    return Results.NotFound();

                var redirectUris = new List<string>();
                await foreach (var uri in manager.GetRedirectUrisAsync(application))
                    redirectUris.Add(uri);

                var postLogoutUris = new List<string>();
                await foreach (var uri in manager.GetPostLogoutRedirectUrisAsync(application))
                    postLogoutUris.Add(uri);

                var permissions = new List<string>();
                await foreach (var perm in manager.GetPermissionsAsync(application))
                    permissions.Add(perm);

                return Inertia.Render("OpenIddict/OpenIddict/ClientsEdit", new
                {
                    client = new
                    {
                        id = await manager.GetIdAsync(application),
                        clientId = await manager.GetClientIdAsync(application),
                        displayName = await manager.GetDisplayNameAsync(application),
                        clientType = await manager.GetClientTypeAsync(application),
                    },
                    redirectUris,
                    postLogoutUris,
                    permissions,
                    tab = tab ?? "details",
                });
            }
        ).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
```

Note: `IOpenIddictApplicationManager.GetRedirectUrisAsync`, `GetPostLogoutRedirectUrisAsync`, and `GetPermissionsAsync` return `IAsyncEnumerable<string>`. Check the actual API — if they return different types, adjust accordingly. Read the OpenIddict source or use context7 to verify.

**Step 4: Build and commit**

```bash
dotnet build modules/OpenIddict/src/OpenIddict/OpenIddict.csproj
git add modules/OpenIddict/src/OpenIddict/Views/
git commit -m "feat(openiddict): add client management view endpoints"
```

---

### Task 4: Create Client Action Endpoints

**Files:**
- Create: `modules/OpenIddict/src/OpenIddict/Endpoints/OpenIddict/ClientsActionEndpoint.cs`

**Step 1: Create action endpoints**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using SimpleModule.Core;

namespace SimpleModule.OpenIddict.Endpoints.OpenIddict;

public class ClientsActionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/openiddict/clients")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .DisableAntiforgery();

        // POST / — Create client
        group.MapPost("/", async (
            [FromForm] string clientId,
            [FromForm] string displayName,
            [FromForm] string clientType,
            [FromForm] string? clientSecret,
            HttpContext context,
            IOpenIddictApplicationManager manager
        ) =>
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId.Trim(),
                DisplayName = displayName.Trim(),
                ClientType = clientType,
            };

            if (clientType == OpenIddictConstants.ClientTypes.Confidential
                && !string.IsNullOrWhiteSpace(clientSecret))
            {
                descriptor.ClientSecret = clientSecret;
            }

            // Parse redirect URIs from form
            var form = await context.Request.ReadFormAsync();
            foreach (var uri in form["redirectUris"].Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                descriptor.RedirectUris.Add(new Uri(uri!));
            }
            foreach (var uri in form["postLogoutUris"].Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri!));
            }

            // Parse permissions (grant types, scopes, endpoints)
            foreach (var perm in form["permissions"].Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                descriptor.Permissions.Add(perm!);
            }

            var application = await manager.CreateAsync(descriptor);
            var id = await manager.GetIdAsync(application);

            return Results.Redirect($"/openiddict/clients/{id}/edit");
        });

        // POST /{id} — Update client details
        group.MapPost("/{id}", async (
            string id,
            [FromForm] string displayName,
            [FromForm] string clientType,
            IOpenIddictApplicationManager manager
        ) =>
        {
            var application = await manager.FindByIdAsync(id);
            if (application is null)
                return Results.NotFound();

            var descriptor = new OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(descriptor, application);

            descriptor.DisplayName = displayName.Trim();
            descriptor.ClientType = clientType;

            await manager.UpdateAsync(application, descriptor);

            return Results.Redirect($"/openiddict/clients/{id}/edit?tab=details");
        });

        // POST /{id}/uris — Update redirect URIs
        group.MapPost("/{id}/uris", async (
            string id,
            HttpContext context,
            IOpenIddictApplicationManager manager
        ) =>
        {
            var application = await manager.FindByIdAsync(id);
            if (application is null)
                return Results.NotFound();

            var descriptor = new OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(descriptor, application);

            descriptor.RedirectUris.Clear();
            descriptor.PostLogoutRedirectUris.Clear();

            var form = await context.Request.ReadFormAsync();
            foreach (var uri in form["redirectUris"].Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                descriptor.RedirectUris.Add(new Uri(uri!));
            }
            foreach (var uri in form["postLogoutUris"].Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri!));
            }

            await manager.UpdateAsync(application, descriptor);

            return Results.Redirect($"/openiddict/clients/{id}/edit?tab=uris");
        });

        // POST /{id}/permissions — Update permissions
        group.MapPost("/{id}/permissions", async (
            string id,
            HttpContext context,
            IOpenIddictApplicationManager manager
        ) =>
        {
            var application = await manager.FindByIdAsync(id);
            if (application is null)
                return Results.NotFound();

            var descriptor = new OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(descriptor, application);

            descriptor.Permissions.Clear();

            var form = await context.Request.ReadFormAsync();
            foreach (var perm in form["permissions"].Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                descriptor.Permissions.Add(perm!);
            }

            await manager.UpdateAsync(application, descriptor);

            return Results.Redirect($"/openiddict/clients/{id}/edit?tab=permissions");
        });

        // DELETE /{id} — Delete client
        group.MapDelete("/{id}", async (
            string id,
            IOpenIddictApplicationManager manager
        ) =>
        {
            var application = await manager.FindByIdAsync(id);
            if (application is null)
                return Results.NotFound();

            await manager.DeleteAsync(application);

            return Results.Redirect("/openiddict/clients");
        });
    }
}
```

**Step 2: Build and commit**

```bash
dotnet build modules/OpenIddict/src/OpenIddict/OpenIddict.csproj
git add modules/OpenIddict/src/OpenIddict/Endpoints/
git commit -m "feat(openiddict): add client CRUD action endpoints"
```

---

### Task 5: Create React Pages for Client Management

**Files:**
- Create: `modules/OpenIddict/src/OpenIddict/Pages/index.ts`
- Create: `modules/OpenIddict/src/OpenIddict/Pages/OpenIddict/Clients.tsx`
- Create: `modules/OpenIddict/src/OpenIddict/Pages/OpenIddict/ClientsCreate.tsx`
- Create: `modules/OpenIddict/src/OpenIddict/Pages/OpenIddict/ClientsEdit.tsx`
- Create: `modules/OpenIddict/src/OpenIddict/package.json`
- Create: `modules/OpenIddict/src/OpenIddict/vite.config.ts`
- Create: `modules/OpenIddict/src/OpenIddict/tsconfig.json`

Follow the Admin module's pattern exactly for all files.

**Step 1: Create package.json**

```json
{
  "name": "@simplemodule/openiddict",
  "private": true,
  "type": "module",
  "scripts": {
    "build": "vite build"
  },
  "peerDependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  }
}
```

**Step 2: Create vite.config.ts**

```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  build: {
    lib: {
      entry: 'Pages/index.ts',
      formats: ['es'],
      fileName: () => 'OpenIddict.pages.js',
    },
    outDir: 'wwwroot',
    rollupOptions: {
      external: ['react', 'react-dom', 'react/jsx-runtime', '@inertiajs/react'],
    },
  },
});
```

**Step 3: Create tsconfig.json**

```json
{
  "extends": "../../../../tsconfig.json",
  "include": ["Pages/**/*"]
}
```

Check if there's a root tsconfig.json to extend. If not, create a standalone one matching the Admin module's pattern.

**Step 4: Create Pages/index.ts**

```typescript
import Clients from './OpenIddict/Clients';
import ClientsCreate from './OpenIddict/ClientsCreate';
import ClientsEdit from './OpenIddict/ClientsEdit';

export const pages: Record<string, any> = {
  'OpenIddict/OpenIddict/Clients': Clients,
  'OpenIddict/OpenIddict/ClientsCreate': ClientsCreate,
  'OpenIddict/OpenIddict/ClientsEdit': ClientsEdit,
};
```

**Step 5: Create Clients.tsx (list page)**

Follow the `Roles.tsx` pattern:

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

interface Client {
  id: string;
  clientId: string;
  displayName: string | null;
  clientType: string | null;
}

interface Props {
  clients: Client[];
}

export default function Clients({ clients }: Props) {
  function handleDelete(id: string, clientId: string) {
    if (!confirm(`Delete client "${clientId}"?`)) return;
    router.delete(`/openiddict/clients/${id}`);
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-extrabold tracking-tight">OAuth Clients</h1>
          <p className="text-text-muted text-sm mt-1">{clients.length} registered clients</p>
        </div>
        <Button onClick={() => router.get('/openiddict/clients/create')}>Create Client</Button>
      </div>

      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Client ID</TableHead>
            <TableHead>Display Name</TableHead>
            <TableHead>Type</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {clients.map((client) => (
            <TableRow key={client.id}>
              <TableCell className="font-mono text-sm">{client.clientId}</TableCell>
              <TableCell>{client.displayName || '\u2014'}</TableCell>
              <TableCell>
                <Badge variant={client.clientType === 'public' ? 'info' : 'secondary'}>
                  {client.clientType || 'unknown'}
                </Badge>
              </TableCell>
              <TableCell>
                <div className="flex gap-3">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => router.get(`/openiddict/clients/${client.id}/edit`)}
                  >
                    Edit
                  </Button>
                  <Button
                    variant="danger"
                    size="sm"
                    onClick={() => handleDelete(client.id, client.clientId)}
                  >
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

**Step 6: Create ClientsCreate.tsx**

```tsx
import { router } from '@inertiajs/react';
import { Button, Card, CardContent, Input, Label } from '@simplemodule/ui';
import { useState } from 'react';

export default function ClientsCreate() {
  const [clientType, setClientType] = useState('public');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/openiddict/clients', new FormData(e.currentTarget));
  }

  return (
    <div className="max-w-xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/openiddict/clients"
          className="text-text-muted hover:text-text transition-colors no-underline"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Create Client</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Register a new OAuth2/OIDC client application</p>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <Label htmlFor="clientId">Client ID</Label>
              <Input id="clientId" name="clientId" required placeholder="my-app" />
            </div>
            <div>
              <Label htmlFor="displayName">Display Name</Label>
              <Input id="displayName" name="displayName" placeholder="My Application" />
            </div>
            <div>
              <Label htmlFor="clientType">Client Type</Label>
              <select
                id="clientType"
                name="clientType"
                value={clientType}
                onChange={(e) => setClientType(e.target.value)}
                className="w-full rounded-md border border-border bg-surface px-3 py-2 text-sm"
              >
                <option value="public">Public</option>
                <option value="confidential">Confidential</option>
              </select>
            </div>
            {clientType === 'confidential' && (
              <div>
                <Label htmlFor="clientSecret">Client Secret</Label>
                <Input id="clientSecret" name="clientSecret" type="password" />
              </div>
            )}
            <Button type="submit">Create Client</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
```

**Step 7: Create ClientsEdit.tsx**

Follow `RolesEdit.tsx` pattern with 3 tabs (Details, URIs, Permissions):

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
} from '@simplemodule/ui';
import { useState } from 'react';

interface ClientDetail {
  id: string;
  clientId: string;
  displayName: string | null;
  clientType: string | null;
}

interface Props {
  client: ClientDetail;
  redirectUris: string[];
  postLogoutUris: string[];
  permissions: string[];
  tab: string;
}

function TabNav({ tabs, activeTab, baseUrl }: { tabs: { id: string; label: string }[]; activeTab: string; baseUrl: string }) {
  return (
    <div className="flex gap-1 mb-4 border-b border-border">
      {tabs.map((t) => (
        <button
          key={t.id}
          type="button"
          onClick={() => router.get(`${baseUrl}?tab=${t.id}`)}
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === t.id
              ? 'border-accent text-accent'
              : 'border-transparent text-text-muted hover:text-text'
          }`}
        >
          {t.label}
        </button>
      ))}
    </div>
  );
}

const tabs = [
  { id: 'details', label: 'Details' },
  { id: 'uris', label: 'Redirect URIs' },
  { id: 'permissions', label: 'Permissions' },
];

export default function ClientsEdit({ client, redirectUris, postLogoutUris, permissions, tab }: Props) {
  return (
    <div className="max-w-3xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/openiddict/clients"
          className="text-text-muted hover:text-text transition-colors no-underline"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Edit Client</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Client ID: <code className="text-xs bg-surface-hover px-1.5 py-0.5 rounded">{client.clientId}</code>
      </p>

      <TabNav tabs={tabs} activeTab={tab} baseUrl={`/openiddict/clients/${client.id}/edit`} />

      {tab === 'details' && <DetailsTab client={client} />}
      {tab === 'uris' && <UrisTab clientId={client.id} redirectUris={redirectUris} postLogoutUris={postLogoutUris} />}
      {tab === 'permissions' && <PermissionsTab clientId={client.id} permissions={permissions} />}
    </div>
  );
}

function DetailsTab({ client }: { client: ClientDetail }) {
  return (
    <Card>
      <CardContent className="p-6">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            router.post(`/openiddict/clients/${client.id}`, new FormData(e.currentTarget));
          }}
          className="space-y-4"
        >
          <div>
            <Label htmlFor="displayName">Display Name</Label>
            <Input id="displayName" name="displayName" defaultValue={client.displayName ?? ''} />
          </div>
          <div>
            <Label htmlFor="clientType">Client Type</Label>
            <select
              id="clientType"
              name="clientType"
              defaultValue={client.clientType ?? 'public'}
              className="w-full rounded-md border border-border bg-surface px-3 py-2 text-sm"
            >
              <option value="public">Public</option>
              <option value="confidential">Confidential</option>
            </select>
          </div>
          <Button type="submit">Save</Button>
        </form>
      </CardContent>
    </Card>
  );
}

function UrisTab({ clientId, redirectUris, postLogoutUris }: { clientId: string; redirectUris: string[]; postLogoutUris: string[] }) {
  const [redirectList, setRedirectList] = useState(redirectUris.length > 0 ? redirectUris : ['']);
  const [postLogoutList, setPostLogoutList] = useState(postLogoutUris.length > 0 ? postLogoutUris : ['']);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Redirect URIs</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            router.post(`/openiddict/clients/${clientId}/uris`, new FormData(e.currentTarget));
          }}
          className="space-y-6"
        >
          <div className="space-y-2">
            <Label>Redirect URIs</Label>
            {redirectList.map((uri, i) => (
              <div key={i} className="flex gap-2">
                <Input name="redirectUris" defaultValue={uri} placeholder="https://example.com/callback" />
                {redirectList.length > 1 && (
                  <Button type="button" variant="ghost" size="sm" onClick={() => setRedirectList(redirectList.filter((_, j) => j !== i))}>
                    Remove
                  </Button>
                )}
              </div>
            ))}
            <Button type="button" variant="ghost" size="sm" onClick={() => setRedirectList([...redirectList, ''])}>
              + Add URI
            </Button>
          </div>

          <div className="space-y-2">
            <Label>Post-Logout Redirect URIs</Label>
            {postLogoutList.map((uri, i) => (
              <div key={i} className="flex gap-2">
                <Input name="postLogoutUris" defaultValue={uri} placeholder="https://example.com/" />
                {postLogoutList.length > 1 && (
                  <Button type="button" variant="ghost" size="sm" onClick={() => setPostLogoutList(postLogoutList.filter((_, j) => j !== i))}>
                    Remove
                  </Button>
                )}
              </div>
            ))}
            <Button type="button" variant="ghost" size="sm" onClick={() => setPostLogoutList([...postLogoutList, ''])}>
              + Add URI
            </Button>
          </div>

          <Button type="submit">Save URIs</Button>
        </form>
      </CardContent>
    </Card>
  );
}

function PermissionsTab({ clientId, permissions }: { clientId: string; permissions: string[] }) {
  const allPermissions = [
    { group: 'Endpoints', items: ['ept:authorization', 'ept:token', 'ept:end_session', 'ept:revocation', 'ept:introspection'] },
    { group: 'Grant Types', items: ['gt:authorization_code', 'gt:refresh_token', 'gt:client_credentials', 'gt:implicit'] },
    { group: 'Response Types', items: ['rst:code', 'rst:token'] },
    { group: 'Scopes', items: ['scp:openid', 'scp:profile', 'scp:email', 'scp:roles'] },
  ];

  return (
    <Card>
      <CardHeader>
        <CardTitle>Client Permissions</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            router.post(`/openiddict/clients/${clientId}/permissions`, new FormData(e.currentTarget));
          }}
          className="space-y-6"
        >
          {allPermissions.map((group) => (
            <div key={group.group}>
              <h4 className="text-sm font-semibold mb-2">{group.group}</h4>
              <div className="space-y-1">
                {group.items.map((perm) => (
                  <label key={perm} className="flex items-center gap-2 text-sm cursor-pointer">
                    <input
                      type="checkbox"
                      name="permissions"
                      value={perm}
                      defaultChecked={permissions.includes(perm)}
                      className="rounded border-border"
                    />
                    <code className="text-xs">{perm}</code>
                  </label>
                ))}
              </div>
            </div>
          ))}
          <Button type="submit">Save Permissions</Button>
        </form>
      </CardContent>
    </Card>
  );
}
```

Note: The permission values use OpenIddict's internal constants (e.g., `ept:authorization` = `OpenIddictConstants.Permissions.Endpoints.Authorization`). Check the actual OpenIddict permission string format — the view endpoint should map them so the React code uses the actual strings.

**Step 8: Update OpenIddict.csproj**

Add the Vite build target (same as Admin module):

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
```

Change the SDK from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Razor` and add the JsBuild target:

```xml
<Target
  Name="JsBuild"
  BeforeTargets="Build"
  Condition="Exists('package.json') And (Exists('node_modules') Or Exists('$(RepoRoot)node_modules'))"
>
  <Exec Command="npx vite build" WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

**Step 9: Install dependencies and build**

```bash
npm install
npm run check
dotnet build modules/OpenIddict/src/OpenIddict/OpenIddict.csproj
```

**Step 10: Commit**

```bash
git add modules/OpenIddict/src/OpenIddict/Pages/ modules/OpenIddict/src/OpenIddict/package.json modules/OpenIddict/src/OpenIddict/vite.config.ts modules/OpenIddict/src/OpenIddict/tsconfig.json
git commit -m "feat(openiddict): add React pages for client management"
```

---

### Task 6: Integration Tests

**Files:**
- Modify: `modules/OpenIddict/tests/OpenIddict.Tests/Integration/ConnectEndpointTests.cs` — add client management tests

**Step 1: Add client CRUD integration tests**

Add tests to verify:
- `GET /openiddict/clients` — returns 200 for admin
- `GET /openiddict/clients` — returns 401 for unauthenticated
- `POST /openiddict/clients` — creates client, redirects
- `GET /openiddict/clients/{id}/edit` — returns 200 for existing client
- `DELETE /openiddict/clients/{id}` — deletes client, redirects

**Step 2: Run tests**

```bash
dotnet test modules/OpenIddict/tests/OpenIddict.Tests/
```

**Step 3: Commit**

```bash
git add modules/OpenIddict/tests/
git commit -m "test(openiddict): add client management integration tests"
```

---

### Task 7: Final Verification

**Step 1: Full build**

Run: `dotnet build`
Expected: 0 warnings, 0 errors.

**Step 2: Full test suite**

Run: `dotnet test`
Expected: All tests pass.

**Step 3: Reset migration (if not done in Task 1)**

```bash
rm template/SimpleModule.Host/Migrations/*
rm template/SimpleModule.Host/app.db
dotnet ef migrations add InitialCreate --project template/SimpleModule.Host --context HostDbContext
```

Verify OpenIddict tables use `OpenIddict_` prefix.

**Step 4: Run app and verify with Playwright**

```bash
dotnet run --project template/SimpleModule.Host
```

Verify:
- Navigate to `/openiddict/clients` — see the seeded client
- Click Create — form works
- Click Edit — tabbed view loads
- Delete works

**Step 5: Commit**

```bash
git add -A
git commit -m "chore: final cleanup for OpenIddict table prefix and client UI"
```
