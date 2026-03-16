# Host Restructure Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rename SimpleModule.Api → SimpleModule.Host and extract reusable concerns into focused packages (SimpleModule.Blazor, @simplemodule/client, @simplemodule/theme-default, Dashboard module).

**Architecture:** Bottom-up extraction — create each package first with code moved from SimpleModule.Api, verify builds, then rename Api → Host as the final step. Each task produces a working build.

**Tech Stack:** .NET 10 (Razor Class Library), React 19, Inertia.js, Vite, Tailwind CSS, npm workspaces.

---

### Task 1: Create SimpleModule.Blazor — Project Setup

**Files:**
- Create: `src/SimpleModule.Blazor/SimpleModule.Blazor.csproj`
- Create: `src/SimpleModule.Blazor/_Imports.razor`

**Step 1: Create the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Create _Imports.razor**

```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Authorization
@using SimpleModule.Blazor
@using SimpleModule.Blazor.Components
```

**Step 3: Add to solution file**

In `SimpleModule.slnx`, add inside `/src/` folder:
```xml
<Project Path="src/SimpleModule.Blazor/SimpleModule.Blazor.csproj" />
```

**Step 4: Add ProjectReference from Api**

In `src/SimpleModule.Api/SimpleModule.Api.csproj`, add:
```xml
<ProjectReference Include="..\SimpleModule.Blazor\SimpleModule.Blazor.csproj" />
```

**Step 5: Build to verify**

Run: `dotnet build src/SimpleModule.Blazor/SimpleModule.Blazor.csproj`
Expected: BUILD SUCCEEDED

**Step 6: Commit**

```bash
git add src/SimpleModule.Blazor/ SimpleModule.slnx src/SimpleModule.Api/SimpleModule.Api.csproj
git commit -m "feat: scaffold SimpleModule.Blazor Razor class library"
```

---

### Task 2: Extract InertiaPageRenderer to SimpleModule.Blazor

**Files:**
- Move: `src/SimpleModule.Api/Inertia/InertiaPageRenderer.cs` → `src/SimpleModule.Blazor/Inertia/InertiaPageRenderer.cs`
- Modify: `src/SimpleModule.Api/Program.cs:66` (update using)

**Step 1: Create the file in Blazor project**

Create `src/SimpleModule.Blazor/Inertia/InertiaPageRenderer.cs`:

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Blazor.Inertia;

public sealed class InertiaPageRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
    : IInertiaPageRenderer
{
    public async Task RenderPageAsync(HttpContext httpContext, string pageJson)
    {
        await using var renderer = new HtmlRenderer(services, loggerFactory);
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<Components.InertiaShell>(
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        ["PageJson"] = pageJson,
                        ["HttpContext"] = httpContext,
                    }
                )
            );
            return output.ToHtmlString();
        });

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync(html);
    }
}
```

Note: This references `Components.InertiaShell` which we'll create in the next task. For now, the renderer references it by type. We'll move InertiaShell in Task 3.

**Step 2: Delete old file**

Delete `src/SimpleModule.Api/Inertia/InertiaPageRenderer.cs`.

**Step 3: Update Program.cs**

In `src/SimpleModule.Api/Program.cs`, change:
- Line 7: `using SimpleModule.Api.Inertia;` → `using SimpleModule.Blazor.Inertia;`

**Step 4: Build to verify**

Run: `dotnet build`
Expected: BUILD SUCCEEDED (will fail until Task 3 completes — defer build check)

---

### Task 3: Extract Shell Components to SimpleModule.Blazor

**Files:**
- Move: `src/SimpleModule.Api/Components/InertiaShell.razor` → `src/SimpleModule.Blazor/Components/InertiaShell.razor`
- Move: `src/SimpleModule.Api/Components/Pages/InertiaPage.razor` → `src/SimpleModule.Blazor/Components/InertiaPage.razor`
- Create: `src/SimpleModule.Blazor/Components/DarkModeScript.razor`

**Step 1: Create InertiaShell.razor in Blazor project**

Create `src/SimpleModule.Blazor/Components/InertiaShell.razor`. This must be adapted — it currently references `SimpleModule.Api.Components.Layout` and `SimpleModule.Api.Components.Pages`. The layout will be passed as a parameter instead of hardcoded:

```razor
@using Microsoft.AspNetCore.Components.Web

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="color-scheme" content="light dark" />
    @if (HeadContent is not null)
    {
        @HeadContent
    }
    <DarkModeScript />
    <script type="importmap">
    {
        "imports": {
            "react": "/js/vendor/react.js",
            "react-dom": "/js/vendor/react-dom.js",
            "react/jsx-runtime": "/js/vendor/react-jsx-runtime.js",
            "react-dom/client": "/js/vendor/react-dom-client.js",
            "@@inertiajs/react": "/js/vendor/inertiajs-react.js"
        }
    }
    </script>
</head>
<body>
    @if (BodyPrefix is not null)
    {
        @BodyPrefix
    }
    <CascadingValue Value="HttpContext">
        <LayoutView Layout="Layout">
            <InertiaPage PageJson="@PageJson" />
        </LayoutView>
    </CascadingValue>
    @if (BodySuffix is not null)
    {
        @BodySuffix
    }
</body>
</html>

@code {
    [Parameter] public string PageJson { get; set; } = "";
    [Parameter] public Microsoft.AspNetCore.Http.HttpContext? HttpContext { get; set; }
    [Parameter, EditorRequired] public Type Layout { get; set; } = default!;
    [Parameter] public RenderFragment? HeadContent { get; set; }
    [Parameter] public RenderFragment? BodyPrefix { get; set; }
    [Parameter] public RenderFragment? BodySuffix { get; set; }
}
```

**Step 2: Create InertiaPage.razor in Blazor project**

Create `src/SimpleModule.Blazor/Components/InertiaPage.razor`:

```razor
<div id="app" data-page="@PageJson"></div>
<script type="module" src="/js/app.js"></script>

@code {
    [Parameter] public string PageJson { get; set; } = "";
}
```

**Step 3: Create DarkModeScript.razor**

Create `src/SimpleModule.Blazor/Components/DarkModeScript.razor`:

```razor
<script>
    function applyTheme() {
        var stored = localStorage.getItem('theme');
        var shouldBeDark = stored === 'dark' || (!stored && window.matchMedia('(prefers-color-scheme: dark)').matches);
        if (shouldBeDark) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    }
    applyTheme();
    function toggleTheme() {
        var html = document.documentElement;
        var isDark = html.classList.toggle('dark');
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
    }
    new MutationObserver(function() {
        var stored = localStorage.getItem('theme');
        var shouldBeDark = stored === 'dark' || (!stored && window.matchMedia('(prefers-color-scheme: dark)').matches);
        var isDark = document.documentElement.classList.contains('dark');
        if (shouldBeDark && !isDark) {
            document.documentElement.classList.add('dark');
        }
    }).observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
</script>
```

**Step 4: Delete old files from Api**

Delete:
- `src/SimpleModule.Api/Components/InertiaShell.razor`
- `src/SimpleModule.Api/Components/Pages/InertiaPage.razor`

**Step 5: Update InertiaPageRenderer to use the new InertiaShell**

The renderer in `src/SimpleModule.Blazor/Inertia/InertiaPageRenderer.cs` needs the host's layout type. Update it to accept a configurable shell component type:

Create `src/SimpleModule.Blazor/Inertia/InertiaOptions.cs`:

```csharp
using Microsoft.AspNetCore.Components;

namespace SimpleModule.Blazor.Inertia;

public class InertiaOptions
{
    public Type ShellComponent { get; set; } = typeof(Components.InertiaShell);
}
```

Update `InertiaPageRenderer.cs` to accept options:

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Blazor.Inertia;

public sealed class InertiaPageRenderer(
    IServiceProvider services,
    ILoggerFactory loggerFactory,
    IOptions<InertiaOptions> options)
    : IInertiaPageRenderer
{
    public async Task RenderPageAsync(HttpContext httpContext, string pageJson)
    {
        await using var renderer = new HtmlRenderer(services, loggerFactory);
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync(
                options.Value.ShellComponent,
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        ["PageJson"] = pageJson,
                        ["HttpContext"] = httpContext,
                    }
                )
            );
            return output.ToHtmlString();
        });

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync(html);
    }
}
```

Create `src/SimpleModule.Blazor/ServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Blazor.Inertia;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Blazor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleModuleBlazor(
        this IServiceCollection services,
        Action<InertiaOptions>? configure = null)
    {
        services.AddScoped<IInertiaPageRenderer, InertiaPageRenderer>();
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<InertiaOptions>(_ => { });
        return services;
    }
}
```

**Step 6: Update Api's Program.cs**

Replace:
```csharp
using SimpleModule.Api.Inertia;
...
builder.Services.AddScoped<IInertiaPageRenderer, InertiaPageRenderer>();
```

With:
```csharp
using SimpleModule.Blazor;
...
builder.Services.AddSimpleModuleBlazor();
```

**Step 7: Update Api's InertiaShell usage**

Create a new host-specific `InertiaShell.razor` at `src/SimpleModule.Api/Components/InertiaShell.razor` that wraps the Blazor package's shell with host-specific head content:

```razor
@using SimpleModule.Blazor.Components

<SimpleModule.Blazor.Components.InertiaShell
    PageJson="@PageJson"
    HttpContext="@HttpContext"
    Layout="typeof(Layout.MainLayout)">
    <HeadContent>
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
        <link href="https://fonts.googleapis.com/css2?family=DM+Sans:ital,opsz,wght@0,9..40,300..700;1,9..40,300..700&family=JetBrains+Mono:wght@400;500;600&family=Sora:wght@400;500;600;700;800&display=swap" rel="stylesheet" />
        <link rel="stylesheet" href="/css/app.css" />
        <title>SimpleModule</title>
    </HeadContent>
    <BodyPrefix>
        <div class="bg-mesh"></div>
    </BodyPrefix>
    <BodySuffix>
        <script src="/js/shell.js"></script>
    </BodySuffix>
</SimpleModule.Blazor.Components.InertiaShell>

@code {
    [Parameter] public string PageJson { get; set; } = "";
    [Parameter] public Microsoft.AspNetCore.Http.HttpContext? HttpContext { get; set; }
}
```

**Step 8: Update Api's App.razor to use DarkModeScript**

Update `src/SimpleModule.Api/Components/App.razor` to import DarkModeScript from the Blazor package instead of inlining the script.

**Step 9: Delete the old Inertia directory from Api**

Delete `src/SimpleModule.Api/Inertia/` directory entirely.

**Step 10: Build to verify**

Run: `dotnet build`
Expected: BUILD SUCCEEDED

**Step 11: Commit**

```bash
git add -A
git commit -m "feat: extract shell components and InertiaPageRenderer to SimpleModule.Blazor"
```

---

### Task 4: Extract Composable Nav Components to SimpleModule.Blazor

**Files:**
- Create: `src/SimpleModule.Blazor/Components/ModuleNav.razor`
- Create: `src/SimpleModule.Blazor/Components/UserDropdown.razor`
- Create: `src/SimpleModule.Blazor/Components/DarkModeToggle.razor`
- Modify: `src/SimpleModule.Api/Components/Layout/MainLayout.razor` (use the new components)

**Step 1: Create ModuleNav.razor**

Create `src/SimpleModule.Blazor/Components/ModuleNav.razor`:

```razor
@using SimpleModule.Core.Menu
@inject IMenuRegistry MenuRegistry

<div class="hidden sm:flex items-center gap-1">
    @if (IsAuthenticated)
    {
        @if (AuthenticatedPrefix is not null)
        {
            @AuthenticatedPrefix
        }
        @foreach (var item in NavbarItems.Where(i => i.RequiresAuth))
        {
            <a href="@item.Url" class="@NavLinkClass(item.Url)">@item.Label</a>
        }
        @if (AuthenticatedSuffix is not null)
        {
            @AuthenticatedSuffix
        }
    }
    else
    {
        @foreach (var item in NavbarItems.Where(i => !i.RequiresAuth))
        {
            <a href="@item.Url" class="@NavLinkClass(item.Url)">@item.Label</a>
        }
        @if (AnonymousSuffix is not null)
        {
            @AnonymousSuffix
        }
    }
</div>

@code {
    [CascadingParameter]
    public Microsoft.AspNetCore.Http.HttpContext? HttpContext { get; set; }

    [Parameter] public bool IsAuthenticated { get; set; }
    [Parameter] public RenderFragment? AuthenticatedPrefix { get; set; }
    [Parameter] public RenderFragment? AuthenticatedSuffix { get; set; }
    [Parameter] public RenderFragment? AnonymousSuffix { get; set; }

    private IReadOnlyList<MenuItem> NavbarItems => MenuRegistry.GetItems(MenuSection.Navbar);

    private string NavLinkClass(string path)
    {
        var currentPath = HttpContext?.Request.Path.ToString() ?? "";
        var isActive = currentPath.Equals(path, StringComparison.OrdinalIgnoreCase);
        return isActive
            ? "text-sm font-medium text-primary no-underline px-3 py-1.5 rounded-lg bg-primary-subtle transition-all duration-200"
            : "text-sm text-text-muted no-underline px-3 py-1.5 rounded-lg hover:text-text hover:bg-surface-raised transition-all duration-200";
    }
}
```

**Step 2: Create UserDropdown.razor**

Create `src/SimpleModule.Blazor/Components/UserDropdown.razor`:

```razor
@using SimpleModule.Core.Menu
@inject IMenuRegistry MenuRegistry

<div class="user-dropdown-wrap" id="user-menu">
    <button class="user-dropdown-trigger" onclick="toggleUserMenu()" aria-expanded="false" id="user-menu-btn">
        <span class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white shadow-sm" style="background:linear-gradient(135deg,var(--color-primary),var(--color-accent));">@UserInitial</span>
        <span class="hidden sm:block text-sm font-medium text-text max-w-[140px] truncate">@DisplayName</span>
        <svg class="w-4 h-4 text-text-muted transition-transform duration-200" id="user-menu-chevron" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 9l-7 7-7-7"/></svg>
    </button>
    <div class="user-dropdown" id="user-dropdown">
        <div class="user-dropdown-header">
            <div class="text-sm font-semibold text-text truncate">@DisplayName</div>
            <div class="text-xs text-text-muted truncate mt-0.5">@UserEmail</div>
        </div>
        <div class="user-dropdown-body">
            @{
                string? lastGroup = null;
                foreach (var item in DropdownItems)
                {
                    if (lastGroup is not null && item.Group != lastGroup)
                    {
                        <div class="user-dropdown-divider"></div>
                    }
                    lastGroup = item.Group;
                    <a href="@item.Url" class="user-dropdown-item">
                        @((MarkupString)item.Icon)
                        @item.Label
                    </a>
                }
            }
            <div class="user-dropdown-divider"></div>
            <form method="post" action="@LogoutUrl" @formname="nav-logout" id="nav-logout-form">
                <AntiforgeryToken />
                <button type="submit" class="user-dropdown-item danger w-full text-left bg-transparent border-none" style="font-family:inherit;font-size:inherit;">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/></svg>
                    Log out
                </button>
            </form>
        </div>
    </div>
</div>

@code {
    [Parameter, EditorRequired] public string DisplayName { get; set; } = "";
    [Parameter] public string UserEmail { get; set; } = "";
    [Parameter] public string UserInitial { get; set; } = "U";
    [Parameter] public string LogoutUrl { get; set; } = "/Identity/Account/Logout";

    private IReadOnlyList<MenuItem> DropdownItems => MenuRegistry.GetItems(MenuSection.UserDropdown);
}
```

**Step 3: Create DarkModeToggle.razor**

Create `src/SimpleModule.Blazor/Components/DarkModeToggle.razor`:

```razor
<button onclick="toggleTheme()" class="w-9 h-9 rounded-xl flex items-center justify-center text-text-muted hover:text-text hover:bg-surface-raised transition-all cursor-pointer bg-transparent border-none" title="Toggle dark mode">
    <svg class="dark:hidden w-[18px] h-[18px]" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M21 12.79A9 9 0 1111.21 3a7 7 0 009.79 9.79z"/></svg>
    <svg class="hidden dark:block w-[18px] h-[18px]" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>
</button>
```

**Step 4: Update MainLayout.razor to use composable components**

Replace `src/SimpleModule.Api/Components/Layout/MainLayout.razor` to compose from the new building blocks:

```razor
@using SimpleModule.Core.Menu
@using SimpleModule.Blazor.Components
@inherits LayoutComponentBase

<nav class="sticky top-0 z-50 border-b border-border bg-surface-overlay" style="backdrop-filter:blur(20px);-webkit-backdrop-filter:blur(20px);">
    <div class="max-w-6xl mx-auto flex items-center px-6 py-3">
        <div class="flex items-center gap-6">
            <a href="/" class="flex items-center gap-2.5 no-underline font-bold text-text group" style="font-family:'Sora',sans-serif;">
                <span class="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-bold shadow-md transition-transform duration-200 group-hover:scale-105" style="background:linear-gradient(135deg,var(--color-primary),var(--color-accent));">S</span>
                <span class="text-base">SimpleModule</span>
            </a>
            <ModuleNav IsAuthenticated="@IsAuthenticated">
                <AuthenticatedPrefix>
                    <a href="/" class="@NavLinkClass("/")">Dashboard</a>
                </AuthenticatedPrefix>
                <AuthenticatedSuffix>
                    <a href="/swagger" class="@NavLinkClass("/swagger")">API Docs</a>
                    <a href="/health/live" class="@NavLinkClass("/health/live")">Health</a>
                </AuthenticatedSuffix>
                <AnonymousSuffix>
                    <a href="/swagger" class="text-sm text-text-muted no-underline hover:text-primary transition-colors">API Docs</a>
                </AnonymousSuffix>
            </ModuleNav>
        </div>
        <div class="ml-auto flex items-center gap-3">
            <DarkModeToggle />
            @if (IsAuthenticated)
            {
                <UserDropdown DisplayName="@DisplayName" UserEmail="@UserEmail" UserInitial="@UserInitial" />
            }
            else
            {
                <a href="/Identity/Account/Login" class="btn-ghost btn-sm no-underline">Log in</a>
                <a href="/Identity/Account/Register" class="btn-primary btn-sm no-underline hidden sm:inline-flex">Sign up</a>
            }
        </div>
    </div>
</nav>

<main class="max-w-6xl mx-auto mt-8 mb-16 px-4 sm:px-6">
    @Body
</main>

<script suppress-error="BL9992" src="/js/shell.js"></script>

@code {
    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    private bool IsAuthenticated => HttpContext?.User?.Identity?.IsAuthenticated == true;
    private string DisplayName => HttpContext?.User?.Identity?.Name ?? "User";
    private string UserEmail => HttpContext?.User?.Identity?.Name ?? "";
    private string UserInitial => (HttpContext?.User?.Identity?.Name ?? "U").Substring(0, 1).ToUpper();

    private string NavLinkClass(string path)
    {
        var currentPath = HttpContext?.Request.Path.ToString() ?? "";
        var isActive = currentPath.Equals(path, StringComparison.OrdinalIgnoreCase);
        return isActive
            ? "text-sm font-medium text-primary no-underline px-3 py-1.5 rounded-lg bg-primary-subtle transition-all duration-200"
            : "text-sm text-text-muted no-underline px-3 py-1.5 rounded-lg hover:text-text hover:bg-surface-raised transition-all duration-200";
    }
}
```

**Step 5: Build to verify**

Run: `dotnet build`
Expected: BUILD SUCCEEDED

**Step 6: Commit**

```bash
git add -A
git commit -m "feat: extract ModuleNav, UserDropdown, DarkModeToggle to SimpleModule.Blazor"
```

---

### Task 5: Create @simplemodule/client npm Package

**Files:**
- Create: `src/SimpleModule.Client/package.json`
- Create: `src/SimpleModule.Client/src/vite-plugin-vendor.ts`
- Create: `src/SimpleModule.Client/src/resolve-page.ts`
- Create: `src/SimpleModule.Client/src/index.ts`
- Modify: `src/SimpleModule.Api/ClientApp/app.tsx` (slim down)
- Modify: `src/SimpleModule.Api/ClientApp/vite.config.ts` (slim down)
- Modify: `package.json` (add workspace)

**Step 1: Create package.json**

Create `src/SimpleModule.Client/package.json`:

```json
{
  "private": true,
  "name": "@simplemodule/client",
  "type": "module",
  "main": "src/index.ts",
  "peerDependencies": {
    "@inertiajs/react": "^2.0.0",
    "esbuild": "*",
    "react": "^19.0.0",
    "react-dom": "^19.0.0",
    "vite": "^6.0.0"
  }
}
```

**Step 2: Create resolve-page.ts**

Create `src/SimpleModule.Client/src/resolve-page.ts`:

```typescript
export async function resolvePage(name: string) {
  const moduleName = name.split('/')[0];
  const mod = await import(
    /* @vite-ignore */
    `/_content/${moduleName}/${moduleName}.pages.js`
  );
  const page = mod.pages[name];
  return page.default ? page : { default: page };
}
```

**Step 3: Create vite-plugin-vendor.ts**

Create `src/SimpleModule.Client/src/vite-plugin-vendor.ts`:

```typescript
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import path from 'node:path';
import * as esbuild from 'esbuild';
import type { Plugin } from 'vite';

export interface VendorEntry {
  pkg: string;
  file: string;
  externals: string[];
}

export const defaultVendors: VendorEntry[] = [
  { pkg: 'react', file: 'react', externals: [] },
  { pkg: 'react-dom', file: 'react-dom', externals: ['react'] },
  { pkg: 'react/jsx-runtime', file: 'react-jsx-runtime', externals: ['react'] },
  { pkg: 'react-dom/client', file: 'react-dom-client', externals: ['react', 'react-dom'] },
  {
    pkg: '@inertiajs/react',
    file: 'inertiajs-react',
    externals: ['react', 'react-dom', 'react/jsx-runtime', 'react-dom/client'],
  },
];

export function vendorPaths(
  vendors: VendorEntry[] = defaultVendors,
  prefix = '/js/vendor',
): Record<string, string> {
  return Object.fromEntries(vendors.map((v) => [v.pkg, `${prefix}/${v.file}.js`]));
}

function getExportNames(require_: NodeRequire, pkg: string): string[] {
  try {
    return Object.keys(require_(pkg)).filter((k) => k !== 'default' && k !== '__esModule');
  } catch {
    return [];
  }
}

export function vendorBuildPlugin(options?: {
  vendors?: VendorEntry[];
  outDir: string;
}): Plugin {
  const vendors = options?.vendors ?? defaultVendors;

  return {
    name: 'build-vendors',
    apply: 'build',
    async buildStart() {
      const outDir = options?.outDir ?? path.resolve(process.cwd(), '../wwwroot/js/vendor');
      const require_ = createRequire(import.meta.url);

      if (vendors.every((v) => existsSync(path.join(outDir, `${v.file}.js`)))) return;
      mkdirSync(outDir, { recursive: true });

      for (const v of vendors) {
        const outfile = path.join(outDir, `${v.file}.js`);

        await esbuild.build({
          entryPoints: [v.pkg],
          bundle: true,
          format: 'esm',
          platform: 'browser',
          external: v.externals,
          outfile,
          logLevel: 'warning',
        });

        let code = readFileSync(outfile, 'utf-8');

        const imports: string[] = [];
        for (let i = 0; i < v.externals.length; i++) {
          const ext = v.externals[i];
          const re = new RegExp(
            `__require\\("${ext.replace(/[.*+?^${}()|[\]\\/]/g, '\\$&')}"\\)`,
            'g',
          );
          if (re.test(code)) {
            imports.push(`import * as __ext${i} from "${ext}";`);
            code = code.replace(re, `__ext${i}`);
          }
        }
        if (imports.length) code = `${imports.join('\n')}\n${code}`;

        const exportNames = getExportNames(require_, v.pkg);
        if (exportNames.length) {
          const match = code.match(/export\s+default\s+(.+?)\s*;\s*$/m);
          if (match) {
            const named = exportNames.map((e) => `  ${e}`).join(',\n');
            code = code.replace(
              match[0],
              `var __mod = ${match[1]};\nexport default __mod;\nexport var {\n${named}\n} = __mod;\n`,
            );
          }
        }

        writeFileSync(outfile, code);
      }
    },
  };
}
```

**Step 4: Create index.ts**

Create `src/SimpleModule.Client/src/index.ts`:

```typescript
export { resolvePage } from './resolve-page';
export {
  vendorBuildPlugin,
  vendorPaths,
  defaultVendors,
  type VendorEntry,
} from './vite-plugin-vendor';
```

**Step 5: Update root package.json workspaces**

In root `package.json`, add the new workspace:

```json
"workspaces": [
  "src/modules/*/src/*",
  "src/SimpleModule.Api/ClientApp",
  "src/SimpleModule.Client"
]
```

**Step 6: Slim down ClientApp/app.tsx**

Replace `src/SimpleModule.Api/ClientApp/app.tsx`:

```typescript
import { resolvePage } from '@simplemodule/client';
import { createInertiaApp } from '@inertiajs/react';
import { createRoot } from 'react-dom/client';

createInertiaApp({
  resolve: resolvePage,
  setup({ el, App, props }) {
    createRoot(el).render(<App {...props} />);
  },
});
```

**Step 7: Slim down ClientApp/vite.config.ts**

Replace `src/SimpleModule.Api/ClientApp/vite.config.ts`:

```typescript
import path from 'node:path';
import react from '@vitejs/plugin-react';
import { defaultVendors, vendorBuildPlugin, vendorPaths } from '@simplemodule/client';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [
    vendorBuildPlugin({
      outDir: path.resolve(__dirname, '../wwwroot/js/vendor'),
    }),
    react(),
  ],
  build: {
    outDir: path.resolve(__dirname, '../wwwroot/js'),
    emptyOutDir: false,
    rollupOptions: {
      input: path.resolve(__dirname, 'app.tsx'),
      external: defaultVendors.map((v) => v.pkg),
      output: {
        entryFileNames: 'app.js',
        paths: vendorPaths(),
      },
    },
  },
});
```

**Step 8: Install dependencies and build**

Run: `npm install`
Run: `npm run check`
Expected: No lint errors

**Step 9: Commit**

```bash
git add -A
git commit -m "feat: extract @simplemodule/client npm package with Vite vendor plugin and page resolver"
```

---

### Task 6: Create @simplemodule/theme-default npm Package

**Files:**
- Create: `src/SimpleModule.Theme.Default/package.json`
- Create: `src/SimpleModule.Theme.Default/theme.css`
- Modify: `src/SimpleModule.Api/Styles/app.css` (slim down to import)

**Step 1: Create package.json**

Create `src/SimpleModule.Theme.Default/package.json`:

```json
{
  "private": true,
  "name": "@simplemodule/theme-default",
  "main": "theme.css"
}
```

**Step 2: Move CSS**

Copy the entire contents of `src/SimpleModule.Api/Styles/app.css` (all 543 lines) to `src/SimpleModule.Theme.Default/theme.css`.

Remove the `@source` directive from the theme file (line 2: `@source "../../modules/";`) — that stays in the host.

Also remove the `@import "tailwindcss";` line — that stays in the host (the host's Tailwind build processes this).

So `theme.css` starts at the `@theme {` block (line 9 of the original) and goes to end of file.

**Step 3: Slim down host's app.css**

Replace `src/SimpleModule.Api/Styles/app.css`:

```css
@import "tailwindcss";
@import "@simplemodule/theme-default/theme.css";
@source "../../modules/";
```

**Step 4: Add workspace to root package.json**

In root `package.json`, update workspaces:

```json
"workspaces": [
  "src/modules/*/src/*",
  "src/SimpleModule.Api/ClientApp",
  "src/SimpleModule.Client",
  "src/SimpleModule.Theme.Default"
]
```

**Step 5: Install and build**

Run: `npm install`
Run: `dotnet build src/SimpleModule.Api/SimpleModule.Api.csproj`
Expected: BUILD SUCCEEDED (Tailwind resolves the import via node_modules)

**Step 6: Commit**

```bash
git add -A
git commit -m "feat: extract @simplemodule/theme-default npm package with design system CSS"
```

---

### Task 7: Create Dashboard Module — Scaffold

**Files:**
- Create: `src/modules/Dashboard/src/Dashboard/Dashboard.csproj`
- Create: `src/modules/Dashboard/src/Dashboard/DashboardModule.cs`
- Create: `src/modules/Dashboard/src/Dashboard/DashboardConstants.cs`
- Create: `src/modules/Dashboard/src/Dashboard/package.json`
- Create: `src/modules/Dashboard/src/Dashboard/vite.config.ts`
- Create: `src/modules/Dashboard/src/Dashboard/Pages/index.ts`

**Step 1: Create Dashboard.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\..\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
  <Target Name="JsBuild" BeforeTargets="Build" Condition="Exists('package.json') And (Exists('node_modules') Or Exists('$(RepoRoot)node_modules'))">
    <Exec Command="npx vite build" WorkingDirectory="$(MSBuildProjectDirectory)" />
  </Target>
</Project>
```

**Step 2: Create DashboardConstants.cs**

```csharp
namespace SimpleModule.Dashboard;

internal static class DashboardConstants
{
    public const string ModuleName = "Dashboard";
    public const string RoutePrefix = "";
}
```

Note: RoutePrefix is empty string — the dashboard serves the root `/` route.

**Step 3: Create DashboardModule.cs**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;

namespace SimpleModule.Dashboard;

[Module(DashboardConstants.ModuleName, RoutePrefix = DashboardConstants.RoutePrefix)]
public class DashboardModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/",
            (HttpContext context) =>
            {
                var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
                var displayName = context.User?.Identity?.Name ?? "User";
                var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

                return Inertia.Render(
                    "Dashboard/Home",
                    new { isAuthenticated, displayName, isDevelopment }
                );
            }
        );
    }
}
```

**Step 4: Create package.json**

```json
{
  "private": true,
  "name": "@simplemodule/dashboard",
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

**Step 5: Create vite.config.ts**

```typescript
import { resolve } from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react()],
  build: {
    lib: {
      entry: resolve(__dirname, 'Pages/index.ts'),
      formats: ['es'],
      fileName: () => 'Dashboard.pages.js',
    },
    outDir: 'wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      external: ['react', 'react-dom', 'react/jsx-runtime', '@inertiajs/react'],
      output: { inlineDynamicImports: true },
    },
  },
});
```

**Step 6: Create Pages/index.ts (placeholder)**

```typescript
import Home from './Home';

export const pages: Record<string, any> = {
  'Dashboard/Home': Home,
};
```

**Step 7: Add to solution and Api csproj**

In `SimpleModule.slnx`, add a Dashboard folder:
```xml
<Folder Name="/modules/Dashboard/">
    <Project Path="src/modules/Dashboard/src/Dashboard/Dashboard.csproj" />
</Folder>
```

In `src/SimpleModule.Api/SimpleModule.Api.csproj`, add:
```xml
<ProjectReference Include="..\modules\Dashboard\src\Dashboard\Dashboard.csproj" />
```

**Step 8: Build .NET to verify scaffold**

Run: `dotnet build src/modules/Dashboard/src/Dashboard/Dashboard.csproj`
Expected: BUILD SUCCEEDED (React page not yet created, but csproj compiles)

**Step 9: Commit**

```bash
git add -A
git commit -m "feat: scaffold Dashboard module"
```

---

### Task 8: Create Dashboard Module — React Pages

**Files:**
- Create: `src/modules/Dashboard/src/Dashboard/Pages/Home.tsx`
- Delete: `src/SimpleModule.Api/Components/Pages/Home.razor`

**Step 1: Create Home.tsx**

Convert the Blazor `Home.razor` to a React component. Create `src/modules/Dashboard/src/Dashboard/Pages/Home.tsx`:

```tsx
interface HomeProps {
  isAuthenticated: boolean;
  displayName: string;
  isDevelopment: boolean;
}

function DashboardView({ displayName }: { displayName: string }) {
  return (
    <>
      <div className="mb-6">
        <h1
          className="text-2xl font-extrabold tracking-tight"
          style={{ fontFamily: "'Sora', sans-serif" }}
        >
          Welcome back, <span className="gradient-text">{displayName}</span>
        </h1>
        <p className="text-text-muted text-sm mt-1">Here&apos;s your development dashboard</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
        <a href="/Identity/Account/Manage" className="dash-card no-underline group">
          <div className="flex items-center gap-3 mb-3">
            <span className="w-9 h-9 rounded-xl flex items-center justify-center text-primary bg-primary-subtle">
              <svg className="w-[18px] h-[18px]" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
            </span>
            <span className="text-sm font-semibold text-text group-hover:text-primary transition-colors">Account</span>
          </div>
          <p className="text-xs text-text-muted">Manage your profile and security settings</p>
        </a>
        <a href="/swagger" className="dash-card no-underline group">
          <div className="flex items-center gap-3 mb-3">
            <span className="w-9 h-9 rounded-xl flex items-center justify-center text-accent bg-success-bg">
              <svg className="w-[18px] h-[18px]" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" /></svg>
            </span>
            <span className="text-sm font-semibold text-text group-hover:text-primary transition-colors">API Docs</span>
          </div>
          <p className="text-xs text-text-muted">Explore endpoints and test requests</p>
        </a>
        <a href="/health/live" className="dash-card no-underline group">
          <div className="flex items-center gap-3 mb-3">
            <span className="w-9 h-9 rounded-xl flex items-center justify-center text-info bg-info-bg">
              <svg className="w-[18px] h-[18px]" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" /></svg>
            </span>
            <span className="text-sm font-semibold text-text group-hover:text-primary transition-colors">Health</span>
          </div>
          <p className="text-xs text-text-muted">Check system status and diagnostics</p>
        </a>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <UserInfoPanel />
        <TokenTester />
      </div>

      <ApiTester />
    </>
  );
}

function UserInfoPanel() {
  // Client-side fetch for user info
  const [info, setInfo] = React.useState<Record<string, string> | null>(null);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    fetch('/api/users/me')
      .then((res) => {
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        return res.json();
      })
      .then(setInfo)
      .catch((e) => setError(e.message));
  }, []);

  return (
    <div className="panel">
      <h2 className="panel-title">User Info</h2>
      <div className="glass-card p-5">
        {error ? (
          <span className="text-danger text-sm">Failed to load: {error}</span>
        ) : !info ? (
          <div className="text-text-muted text-sm flex items-center gap-2">
            Loading user info<span className="spinner" />
          </div>
        ) : (
          <>
            <InfoRow label="Name" value={info.displayName || info.name || '-'} />
            <InfoRow label="Email" value={info.email || '-'} />
            <InfoRow label="ID" value={info.id || '-'} mono />
            {info.roles && (
              <InfoRow
                label="Roles"
                value={Array.isArray(info.roles) ? info.roles.join(', ') : info.roles}
              />
            )}
          </>
        )}
      </div>
    </div>
  );
}

function InfoRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex justify-between items-center py-3 text-sm border-b border-border last:border-b-0">
      <span className="text-text-muted text-xs uppercase tracking-wide">{label}</span>
      <span className={mono ? 'font-mono text-xs text-text-secondary' : 'font-medium text-text'}>
        {value}
      </span>
    </div>
  );
}

function TokenTester() {
  const [token, setToken] = React.useState<string | null>(null);
  const [loading, setLoading] = React.useState(false);

  React.useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const code = params.get('code');
    const state = params.get('state');
    if (!code) return;

    window.history.replaceState({}, '', '/');
    const verifier = sessionStorage.getItem('pkce_verifier');
    const savedState = sessionStorage.getItem('pkce_state');
    if (state !== savedState) return;

    sessionStorage.removeItem('pkce_verifier');
    sessionStorage.removeItem('pkce_state');

    fetch('/connect/token', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'authorization_code',
        client_id: 'simplemodule-client',
        code,
        redirect_uri: `${window.location.origin}/oauth-callback`,
        code_verifier: verifier || '',
      }),
    })
      .then((res) => res.json())
      .then((data) => setToken(data.access_token))
      .catch(() => {});
  }, []);

  async function startOAuth() {
    setLoading(true);
    const arr = new Uint8Array(32);
    crypto.getRandomValues(arr);
    const verifier = btoa(String.fromCharCode(...arr))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');

    const hash = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier));
    const challenge = btoa(String.fromCharCode(...new Uint8Array(hash)))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');

    const state = crypto.randomUUID();
    sessionStorage.setItem('pkce_verifier', verifier);
    sessionStorage.setItem('pkce_state', state);

    const params = new URLSearchParams({
      response_type: 'code',
      client_id: 'simplemodule-client',
      redirect_uri: `${window.location.origin}/oauth-callback`,
      scope: 'openid profile email',
      state,
      code_challenge: challenge,
      code_challenge_method: 'S256',
    });

    window.location.href = `/connect/authorize?${params}`;
  }

  const claims = React.useMemo(() => {
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
      return Object.entries(payload).map(([key, val]) => ({
        key,
        value:
          key === 'exp' || key === 'iat' || key === 'nbf'
            ? new Date((val as number) * 1000).toLocaleString()
            : typeof val === 'object'
              ? JSON.stringify(val)
              : String(val),
      }));
    } catch {
      return null;
    }
  }, [token]);

  return (
    <div className="panel">
      <h2 className="panel-title">Token Tester</h2>
      <div className="glass-card p-5">
        <h3 className="text-sm font-semibold mb-1">OAuth2 Authorization Code + PKCE</h3>
        <p className="text-xs text-text-muted mb-4">
          Obtain an access token using the{' '}
          <code className="bg-surface-raised px-1.5 py-0.5 rounded text-xs font-mono">
            simplemodule-client
          </code>{' '}
          application.
        </p>
        <button className="btn-primary btn-sm" onClick={startOAuth} disabled={loading}>
          {loading ? (
            <>
              Authorizing<span className="spinner" />
            </>
          ) : (
            'Get Access Token'
          )}
        </button>
        {token && (
          <>
            <div className="code-block break-all max-h-30 mt-4">{token}</div>
            {claims && (
              <>
                <h3 className="text-sm font-semibold mt-4 mb-2">Decoded Claims</h3>
                <div className="overflow-auto rounded-xl border border-border">
                  <table>
                    <thead>
                      <tr>
                        <th>Claim</th>
                        <th>Value</th>
                      </tr>
                    </thead>
                    <tbody>
                      {claims.map((c) => (
                        <tr key={c.key}>
                          <td>{c.key}</td>
                          <td>{c.value}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </>
        )}
      </div>
    </div>
  );
}

function ApiTester() {
  const [status, setStatus] = React.useState<{ ok: boolean; code: string; text: string } | null>(
    null,
  );
  const [response, setResponse] = React.useState('Click an endpoint above to make a request.');
  const [loading, setLoading] = React.useState(false);

  async function callApi(url: string) {
    setLoading(true);
    setStatus(null);
    setResponse('');

    try {
      const token = sessionStorage.getItem('access_token');
      const headers: Record<string, string> = {};
      if (token) headers.Authorization = `Bearer ${token}`;

      const res = await fetch(url, { headers });
      setStatus({ ok: res.ok, code: String(res.status), text: `${res.statusText} — ${url}` });

      const text = await res.text();
      try {
        setResponse(JSON.stringify(JSON.parse(text), null, 2));
      } catch {
        setResponse(text);
      }
    } catch (e: any) {
      setStatus({ ok: false, code: 'Error', text: e.message });
      setResponse(e.message);
    } finally {
      setLoading(false);
    }
  }

  const endpoints = ['/api/users/me', '/api/users', '/api/products', '/api/orders'];

  return (
    <div className="panel mt-2">
      <h2 className="panel-title">API Tester</h2>
      <div className="glass-card p-5">
        <h3 className="text-sm font-semibold mb-3">Call Protected Endpoints</h3>
        <div className="flex gap-2 flex-wrap mb-4">
          {endpoints.map((url) => (
            <button key={url} className="btn-outline btn-sm" onClick={() => callApi(url)}>
              GET {url}
            </button>
          ))}
        </div>
        <div className="text-xs text-text-muted mb-2 flex items-center gap-2">
          {loading && (
            <>
              Calling...<span className="spinner" />
            </>
          )}
          {status && (
            <>
              <span className={status.ok ? 'badge-success' : 'badge-danger'}>{status.code}</span>
              {' '}{status.text}
            </>
          )}
        </div>
        <div className="code-block whitespace-pre-wrap max-h-50">{response}</div>
      </div>
    </div>
  );
}

function LandingView({ isDevelopment }: { isDevelopment: boolean }) {
  return (
    <div className="flex items-center justify-center min-h-[calc(100vh-16rem)]">
      <div className="text-center max-w-lg mx-auto">
        <div
          className="w-16 h-16 rounded-2xl mx-auto mb-6 flex items-center justify-center text-white text-2xl font-bold shadow-lg"
          style={{ background: 'linear-gradient(135deg,var(--color-primary),var(--color-accent))' }}
        >
          S
        </div>
        <h1
          className="text-4xl font-extrabold mb-3 tracking-tight"
          style={{ fontFamily: "'Sora', sans-serif" }}
        >
          <span className="gradient-text">SimpleModule</span>
        </h1>
        <p className="text-text-muted text-base mb-8 max-w-sm mx-auto leading-relaxed">
          Modular monolith framework for .NET &mdash; AOT&#8209;compatible, zero&nbsp;reflection
        </p>

        <div className="flex gap-3 justify-center flex-wrap">
          <a href="/Identity/Account/Login" className="btn-primary btn-lg no-underline">
            Get Started
          </a>
          <a href="/Identity/Account/Register" className="btn-secondary btn-lg no-underline">
            Create Account
          </a>
        </div>

        {isDevelopment && (
          <div className="alert-warning mt-6 text-left text-xs">
            <strong className="block mb-1 text-warning">Quick Start (Development Only)</strong>
            Email:{' '}
            <code className="bg-warning-bg px-1.5 py-0.5 rounded text-xs font-mono font-medium">
              admin@simplemodule.dev
            </code>
            &nbsp; Password:{' '}
            <code className="bg-warning-bg px-1.5 py-0.5 rounded text-xs font-mono font-medium">
              Admin123!
            </code>
          </div>
        )}

        <div className="flex gap-5 justify-center mt-8 text-sm">
          <a
            href="/swagger"
            className="text-text-muted no-underline hover:text-primary transition-colors"
          >
            API Docs
          </a>
          <span className="text-border">&middot;</span>
          <a
            href="/health/live"
            className="text-text-muted no-underline hover:text-primary transition-colors"
          >
            Health Check
          </a>
        </div>
      </div>
    </div>
  );
}

import React from 'react';

export default function Home({ isAuthenticated, displayName, isDevelopment }: HomeProps) {
  return isAuthenticated ? (
    <DashboardView displayName={displayName} />
  ) : (
    <LandingView isDevelopment={isDevelopment} />
  );
}
```

**Step 2: Delete Home.razor from Api**

Delete `src/SimpleModule.Api/Components/Pages/Home.razor`.

**Step 3: Update Routes.razor**

Remove the `@page "/"` route from the Blazor router since Dashboard module now handles it via Inertia. The `Home.razor` page had `@page "/"`, but since we deleted it, Routes.razor should still work — it only routes to Blazor pages that exist. The Dashboard module's endpoint will handle `/` via the Inertia middleware.

**Step 4: Build and verify**

Run: `dotnet build`
Run: `npm install && npm run check`
Expected: BUILD SUCCEEDED, no lint errors

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: create Dashboard module with React pages, remove Home.razor"
```

---

### Task 9: Rename SimpleModule.Api → SimpleModule.Host

**Files:**
- Rename: `src/SimpleModule.Api/` → `src/SimpleModule.Host/`
- Rename: `src/SimpleModule.Api/SimpleModule.Api.csproj` → `src/SimpleModule.Host/SimpleModule.Host.csproj`
- Modify: `SimpleModule.slnx` (update project path)
- Modify: all `ProjectReference` paths that reference SimpleModule.Api
- Modify: `src/SimpleModule.Host/Program.cs` (update namespace usings)
- Modify: `src/SimpleModule.Host/Components/_Imports.razor`
- Modify: `src/SimpleModule.Host/Components/App.razor`
- Modify: `src/SimpleModule.Host/Components/Routes.razor`
- Modify: `src/SimpleModule.Host/Components/Layout/MainLayout.razor`
- Modify: `src/SimpleModule.Host/Properties/launchSettings.json`
- Modify: root `package.json` (update workspace path)
- Modify: `CLAUDE.md`

**Step 1: Rename directory**

```bash
git mv src/SimpleModule.Api src/SimpleModule.Host
```

**Step 2: Rename csproj**

```bash
git mv src/SimpleModule.Host/SimpleModule.Api.csproj src/SimpleModule.Host/SimpleModule.Host.csproj
```

**Step 3: Update namespaces in csproj**

In `SimpleModule.Host.csproj`, no namespace changes needed — the csproj doesn't declare a root namespace, so it defaults to the project name.

**Step 4: Update solution file**

In `SimpleModule.slnx`, change:
```xml
<Project Path="src/SimpleModule.Api/SimpleModule.Api.csproj" />
```
to:
```xml
<Project Path="src/SimpleModule.Host/SimpleModule.Host.csproj" />
```

**Step 5: Update test project references**

Check `tests/SimpleModule.Tests.Shared/` and any test projects that reference SimpleModule.Api — update their `ProjectReference` paths.

The csproj has `<InternalsVisibleTo Include="SimpleModule.Tests.Shared" />`. Find any test project that references `SimpleModule.Api.csproj` and update the path.

**Step 6: Update _Imports.razor**

```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Authorization
@using SimpleModule.Host.Components
@using SimpleModule.Host.Components.Layout
```

**Step 7: Update Program.cs usings**

Change `using SimpleModule.Api.Components;` → `using SimpleModule.Host.Components;`
Change `using SimpleModule.Api.Inertia;` → already updated to `using SimpleModule.Blazor.Inertia;` in Task 3.

**Step 8: Update App.razor namespace reference**

If `App.razor` references `SimpleModule.Api` anywhere, update to `SimpleModule.Host`.

**Step 9: Update Routes.razor**

Update any `typeof(App).Assembly` reference — it should still work since `App` is resolved via `_Imports.razor`.

**Step 10: Update launchSettings.json**

Update application name reference if present (usually just the profile name).

**Step 11: Update root package.json workspaces**

Change `"src/SimpleModule.Api/ClientApp"` → `"src/SimpleModule.Host/ClientApp"`.

**Step 12: Update CLAUDE.md**

Replace all occurrences of `SimpleModule.Api` with `SimpleModule.Host` throughout the file.

**Step 13: Update biome.json if needed**

Check if `biome.json` references `SimpleModule.Api` in any paths.

**Step 14: Build and verify**

Run: `dotnet build`
Run: `npm install`
Run: `npm run check`
Expected: BUILD SUCCEEDED, no lint errors

**Step 15: Run tests**

Run: `dotnet test`
Expected: All tests pass

**Step 16: Commit**

```bash
git add -A
git commit -m "refactor: rename SimpleModule.Api to SimpleModule.Host"
```

---

### Task 10: Final Verification and Cleanup

**Step 1: Full build**

Run: `dotnet build`
Expected: BUILD SUCCEEDED

**Step 2: Run all tests**

Run: `dotnet test`
Expected: All tests pass

**Step 3: Lint check**

Run: `npm run check`
Expected: No errors

**Step 4: Verify the app runs**

Run: `dotnet run --project src/SimpleModule.Host`
Expected: App starts on https://localhost:5001

**Step 5: Verify in browser**

- Navigate to `https://localhost:5001/` — should show landing page (now served by Dashboard module via Inertia)
- Log in — should show dashboard
- Navigate to `/products/browse` — should work (modules unchanged)
- Check dark mode toggle works
- Check user dropdown works

**Step 6: Update CLAUDE.md build commands**

Ensure all references match the new structure:
```bash
dotnet run --project src/SimpleModule.Host
```

**Step 7: Final commit if any cleanup needed**

```bash
git add -A
git commit -m "chore: final cleanup after host restructure"
```
