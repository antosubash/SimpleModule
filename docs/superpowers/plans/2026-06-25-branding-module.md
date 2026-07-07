# Branding Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a Branding module that lets an admin customize a SimpleModule app's appearance — app name, logo, favicon, primary color (light+dark), custom CSS, a configurable top bar, and a configurable footer — applied across the whole app with no color flash.

**Architecture:** Branding stores values as `Application`-scoped Settings (no new DbContext). A `BrandingService` resolves them into a `BrandingDto`. Values reach the UI two ways: (1) an Inertia shared prop `branding` (set by a module middleware) drives React-rendered chrome (name, logo, top bar, footer); (2) a small generic framework extension — `IInertiaHeadContributor` + a `<!--HEAD_CONTRIBUTIONS-->` placeholder — injects `--color-primary` overrides, custom CSS, and a favicon `<link>` into the document `<head>` server-side (no flash). Logo/favicon are uploaded via the existing FileStorage module and served by Branding's own anonymous asset endpoint.

**Tech Stack:** .NET 10, ASP.NET minimal APIs, Roslyn source-gen module discovery, EF-less (settings-backed), React 19 + Inertia.js, Vite library mode, `@simplemodule/ui`, xUnit.v3 + FluentAssertions.

## Global Constraints

- Source generator/module discovery is compile-time: every new endpoint/DTO must build cleanly. `TreatWarningsAsErrors` is ON (`AnalysisMode=All`) — no warnings.
- **No Claude/AI attribution** in commits or source; do not append session URLs to commits.
- Module main project: `Microsoft.NET.Sdk.StaticWebAssets`, `net10.0`, `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. Contracts project: `Microsoft.NET.Sdk`, `net10.0`, references `SimpleModule.Core` only.
- Every `IViewEndpoint` with `Inertia.Render("Branding/X", …)` MUST have a matching `Pages/index.ts` entry (`npm run validate-pages`).
- C#: file-scoped namespaces, `var`, interfaces `IFoo`, private fields `_camelCase`. Biome for TS: single quotes, semicolons, 2-space, trailing commas, 100-col.
- Default primary hex: light `#059669`, dark `#34d399`.
- Settings keys (verbatim): `branding.app_name`, `branding.logo_file_id`, `branding.favicon_file_id`, `branding.color_primary`, `branding.color_primary_dark`, `branding.custom_css`, `branding.topbar`, `branding.footer`. All `SettingScope.Application`, `Group = "Branding"`.
- Asset routes: serve `GET /api/branding/assets/{kind}` (anonymous), upload `POST /api/branding/assets/{kind}` (admin). Admin API: `GET`/`PUT /api/branding`. View: `GET /branding`. Permission: `Branding.Manage`.

---

## Task 1: Contracts project (DTOs, keys, defaults, interface)

**Files:**
- Create: `modules/Branding/src/SimpleModule.Branding.Contracts/SimpleModule.Branding.Contracts.csproj`
- Create: `modules/Branding/src/SimpleModule.Branding.Contracts/BrandingSettingKeys.cs`
- Create: `modules/Branding/src/SimpleModule.Branding.Contracts/BrandingDefaults.cs`
- Create: `modules/Branding/src/SimpleModule.Branding.Contracts/BrandingLink.cs`
- Create: `modules/Branding/src/SimpleModule.Branding.Contracts/TopBarConfig.cs`
- Create: `modules/Branding/src/SimpleModule.Branding.Contracts/FooterConfig.cs`
- Create: `modules/Branding/src/SimpleModule.Branding.Contracts/BrandingDto.cs`
- Create: `modules/Branding/src/SimpleModule.Branding.Contracts/BrandingEditModel.cs`
- Create: `modules/Branding/src/SimpleModule.Branding.Contracts/IBrandingContracts.cs`
- Modify: `SimpleModule.slnx` (add Contracts project)

**Interfaces:**
- Produces: `IBrandingContracts` (`GetBrandingAsync()→BrandingDto`, `GetCustomCssAsync()→string`, `GetEditableAsync()→BrandingEditModel`, `UpdateAsync(BrandingEditModel)→Task`); DTOs `BrandingDto`, `BrandingEditModel`, `TopBarConfig`, `FooterConfig`, `BrandingLink`; static `BrandingSettingKeys`, `BrandingDefaults`.

- [ ] **Step 1: Create the Contracts csproj**

```xml
<!-- modules/Branding/src/SimpleModule.Branding.Contracts/SimpleModule.Branding.Contracts.csproj -->
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

- [ ] **Step 2: Add keys + defaults**

```csharp
// BrandingSettingKeys.cs
namespace SimpleModule.Branding.Contracts;

public static class BrandingSettingKeys
{
    public const string AppName = "branding.app_name";
    public const string LogoFileId = "branding.logo_file_id";
    public const string FaviconFileId = "branding.favicon_file_id";
    public const string ColorPrimary = "branding.color_primary";
    public const string ColorPrimaryDark = "branding.color_primary_dark";
    public const string CustomCss = "branding.custom_css";
    public const string TopBar = "branding.topbar";
    public const string Footer = "branding.footer";
}
```

```csharp
// BrandingDefaults.cs
namespace SimpleModule.Branding.Contracts;

public static class BrandingDefaults
{
    public const string AppName = "SimpleModule";
    public const string ColorPrimary = "#059669";
    public const string ColorPrimaryDark = "#34d399";
}
```

- [ ] **Step 3: Add DTOs**

```csharp
// BrandingLink.cs
using SimpleModule.Core;

namespace SimpleModule.Branding.Contracts;

[Dto]
public class BrandingLink
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
```

```csharp
// TopBarConfig.cs
using System.Collections.Generic;
using SimpleModule.Core;

namespace SimpleModule.Branding.Contracts;

[Dto]
public class TopBarConfig
{
    public bool Enabled { get; set; }
    public string Message { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#059669";
    public string TextColor { get; set; } = "#ffffff";
    public List<BrandingLink> Links { get; set; } = [];
    public bool Dismissible { get; set; } = true;
}
```

```csharp
// FooterConfig.cs
using System.Collections.Generic;
using SimpleModule.Core;

namespace SimpleModule.Branding.Contracts;

[Dto]
public class FooterConfig
{
    public bool Enabled { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<BrandingLink> Links { get; set; } = [];
    public bool ShowCopyright { get; set; } = true;
}
```

```csharp
// BrandingDto.cs  (shared-prop + head-contributor face)
using SimpleModule.Core;

namespace SimpleModule.Branding.Contracts;

[Dto]
public class BrandingDto
{
    public string AppName { get; set; } = BrandingDefaults.AppName;
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string ColorPrimary { get; set; } = BrandingDefaults.ColorPrimary;
    public string ColorPrimaryDark { get; set; } = BrandingDefaults.ColorPrimaryDark;
    public TopBarConfig TopBar { get; set; } = new();
    public FooterConfig Footer { get; set; } = new();
}
```

```csharp
// BrandingEditModel.cs  (admin form face — includes ids + custom css)
using SimpleModule.Core;

namespace SimpleModule.Branding.Contracts;

[Dto]
public class BrandingEditModel
{
    public string AppName { get; set; } = BrandingDefaults.AppName;
    public string? LogoFileId { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconFileId { get; set; }
    public string? FaviconUrl { get; set; }
    public string ColorPrimary { get; set; } = BrandingDefaults.ColorPrimary;
    public string ColorPrimaryDark { get; set; } = BrandingDefaults.ColorPrimaryDark;
    public string CustomCss { get; set; } = string.Empty;
    public TopBarConfig TopBar { get; set; } = new();
    public FooterConfig Footer { get; set; } = new();
}
```

- [ ] **Step 4: Add the contract interface**

```csharp
// IBrandingContracts.cs
using System.Threading.Tasks;

namespace SimpleModule.Branding.Contracts;

public interface IBrandingContracts
{
    Task<BrandingDto> GetBrandingAsync();
    Task<string> GetCustomCssAsync();
    Task<BrandingEditModel> GetEditableAsync();
    Task UpdateAsync(BrandingEditModel model);
}
```

- [ ] **Step 5: Register the project in `SimpleModule.slnx`**

Add a `<Project Path="modules/Branding/src/SimpleModule.Branding.Contracts/SimpleModule.Branding.Contracts.csproj" />` entry alongside the other module contracts projects (match the existing XML grouping/indentation in `SimpleModule.slnx`).

- [ ] **Step 6: Build to verify it compiles**

Run: `dotnet build modules/Branding/src/SimpleModule.Branding.Contracts/SimpleModule.Branding.Contracts.csproj`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 7: Commit**

```bash
git add modules/Branding/src/SimpleModule.Branding.Contracts SimpleModule.slnx
git commit -m "feat(branding): add contracts project (DTOs, keys, interface)"
```

---

## Task 2: Framework — `IInertiaHeadContributor` + renderer head injection

**Files:**
- Create: `framework/SimpleModule.Core/Inertia/IInertiaHeadContributor.cs`
- Modify: `framework/SimpleModule.Hosting/Inertia/HtmlFileInertiaPageRenderer.cs`
- Modify: `template/SimpleModule.Host/wwwroot/index.html` (add placeholder)
- Test: `framework/SimpleModule.Hosting.Tests/Inertia/HeadContributorTests.cs` (create; if no such test project exists, add the test to the closest existing hosting/integration test project — search `tests/` and `framework/**/*.Tests`)

**Interfaces:**
- Produces: `IInertiaHeadContributor { ValueTask<string?> GetHeadHtmlAsync(HttpContext context); }`. Renderer replaces `<!--HEAD_CONTRIBUTIONS-->` per request with the concatenation of all registered contributors' non-empty output; empty string when none.

- [ ] **Step 1: Write the failing integration test**

Use the shared web factory. Register a fake contributor, request a full HTML page, assert the marker appears; on the default app assert the literal placeholder is gone.

```csharp
// HeadContributorTests.cs
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Inertia;
using SimpleModule.Tests.Shared;
using Xunit;

public class HeadContributorTests
{
    private sealed class MarkerContributor : IInertiaHeadContributor
    {
        public ValueTask<string?> GetHeadHtmlAsync(HttpContext context) =>
            ValueTask.FromResult<string?>("<style>/*HEAD_MARKER*/</style>");
    }

    [Fact]
    public async Task RenderedPage_IncludesContributorOutput_AndNoRawPlaceholder()
    {
        await using var factory = new SimpleModuleWebApplicationFactory();
        using var client = factory
            .WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                    s.AddScoped<IInertiaHeadContributor, MarkerContributor>()
                )
            )
            .CreateClient();

        var html = await client.GetStringAsync("/");

        html.Should().Contain("/*HEAD_MARKER*/");
        html.Should().NotContain("<!--HEAD_CONTRIBUTIONS-->");
    }
}
```

> If `SimpleModuleWebApplicationFactory` requires a different construction (check an existing integration test like `modules/Admin/tests/.../Integration/AdminRolesEndpointTests.cs`), mirror that. The factory may be injected via `[Collection(TestCollections.Integration)]`; if a fresh instance can't take service overrides, instead add the fake contributor through the factory's existing override hook used by other tests, or assert the empty-placeholder behavior only and verify the contributor path in Task 6's branding integration test.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~HeadContributorTests"`
Expected: FAIL — `<!--HEAD_CONTRIBUTIONS-->` not present yet (the marker isn't injected; the page also lacks the placeholder).

- [ ] **Step 3: Add the interface**

```csharp
// framework/SimpleModule.Core/Inertia/IInertiaHeadContributor.cs
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Inertia;

/// <summary>
/// Implement and register (scoped) to contribute raw HTML into the document &lt;head&gt;
/// of every full server-rendered Inertia page. Output replaces the
/// &lt;!--HEAD_CONTRIBUTIONS--&gt; placeholder (just before &lt;/head&gt;) per request.
/// Contributors are responsible for their own escaping.
/// </summary>
public interface IInertiaHeadContributor
{
    ValueTask<string?> GetHeadHtmlAsync(HttpContext context);
}
```

- [ ] **Step 4: Add the placeholder to `index.html`**

In `template/SimpleModule.Host/wwwroot/index.html`, insert the placeholder on its own line immediately before `</head>` (after the `<script type="importmap">…</script>` block):

```html
    <!--HEAD_CONTRIBUTIONS-->
</head>
```

- [ ] **Step 5: Inject contributions in the renderer**

In `HtmlFileInertiaPageRenderer.cs`:

1. Add a constant next to the others:
```csharp
private const string HeadContributionsPlaceholder = "<!--HEAD_CONTRIBUTIONS-->";
```
2. Change `RenderPageAsync` to `async Task`, build the head HTML, and replace the placeholder in `before` before writing. Replace the current method body with:

```csharp
public async Task RenderPageAsync(HttpContext httpContext, string pageJson)
{
    var nonce = httpContext.RequestServices.GetRequiredService<ICspNonce>().Value;
    var useViteDev =
        _isDevelopment && httpContext.Items.ContainsKey(DevToolsConstants.ViteDevServerKey);

    var before = useViteDev ? _beforePlaceholderViteDev : _beforePlaceholder;
    var after = useViteDev ? _afterPlaceholderViteDev : _afterPlaceholder;
    var devScript =
        _isDevelopment && !useViteDev
            ? "<script nonce=\"" + nonce + "\">" + LiveReloadClientScript + "</script>"
            : "";

    var headHtml = await BuildHeadContributionsAsync(httpContext);
    before = before.Replace(HeadContributionsPlaceholder, headHtml, StringComparison.Ordinal);

    httpContext.Response.ContentType = "text/html; charset=utf-8";
    await httpContext.Response.WriteAsync(
        string.Concat(
            before.Replace(NoncePlaceholder, nonce, StringComparison.Ordinal),
            $"<script data-page=\"app\" type=\"application/json\" nonce=\"{nonce}\">{pageJson}</script>",
            devScript,
            after.Replace(NoncePlaceholder, nonce, StringComparison.Ordinal)
        )
    );
}

private static async Task<string> BuildHeadContributionsAsync(HttpContext httpContext)
{
    var contributors = httpContext.RequestServices.GetServices<IInertiaHeadContributor>();
    StringBuilder? sb = null;
    foreach (var contributor in contributors)
    {
        var html = await contributor.GetHeadHtmlAsync(httpContext);
        if (string.IsNullOrEmpty(html))
            continue;
        (sb ??= new StringBuilder()).Append(html);
    }
    return sb?.ToString() ?? string.Empty;
}
```
3. Add `using SimpleModule.Core.Inertia;` if not already present (the file already uses `SimpleModule.Core.Inertia`). `System.Text`, `Microsoft.Extensions.DependencyInjection` are already imported.

> Note: the constructor must NOT replace `HeadContributionsPlaceholder` (only `DEPLOY_VERSION` and `MODULE_CSS_LINKS` are replaced at startup). Leaving it untouched is correct — it survives into `_beforePlaceholder` and the ViteDev variant.

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~HeadContributorTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add framework/SimpleModule.Core/Inertia/IInertiaHeadContributor.cs \
        framework/SimpleModule.Hosting/Inertia/HtmlFileInertiaPageRenderer.cs \
        template/SimpleModule.Host/wwwroot/index.html \
        framework/SimpleModule.Hosting.Tests
git commit -m "feat(core): per-request IInertiaHeadContributor head injection"
```

---

## Task 3: Branding main project — module, service, permissions, settings, menu

**Files:**
- Create: `modules/Branding/src/SimpleModule.Branding/SimpleModule.Branding.csproj`
- Create: `modules/Branding/src/SimpleModule.Branding/BrandingPermissions.cs`
- Create: `modules/Branding/src/SimpleModule.Branding/BrandingService.cs`
- Create: `modules/Branding/src/SimpleModule.Branding/BrandingModule.cs`
- Modify: `SimpleModule.slnx`; `template/SimpleModule.Host/SimpleModule.Host.csproj`
- Create test project: `modules/Branding/tests/SimpleModule.Branding.Tests/SimpleModule.Branding.Tests.csproj`
- Create: `modules/Branding/tests/SimpleModule.Branding.Tests/FakeSettings.cs`
- Test: `modules/Branding/tests/SimpleModule.Branding.Tests/BrandingServiceTests.cs`

**Interfaces:**
- Consumes: `IBrandingContracts`, DTOs, keys, defaults (Task 1); `ISettingsContracts` (`GetSettingAsync<T>`, `SetManyAsync`); `IInertiaHeadContributor` (Task 2 — referenced in Task 6).
- Produces: `BrandingService : IBrandingContracts`; `BrandingPermissions.Manage = "Branding.Manage"`; `BrandingModule` registering the service + settings + permission + admin menu item.

- [ ] **Step 1: Create the main csproj**

```xml
<!-- modules/Branding/src/SimpleModule.Branding/SimpleModule.Branding.csproj -->
<Project Sdk="Microsoft.NET.Sdk.StaticWebAssets">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Description>Branding module for SimpleModule. Customize app name, logo, favicon, colors, custom CSS, top bar, and footer.</Description>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\SimpleModule.Branding.Contracts\SimpleModule.Branding.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\modules\Settings\src\SimpleModule.Settings.Contracts\SimpleModule.Settings.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\modules\FileStorage\src\SimpleModule.FileStorage.Contracts\SimpleModule.FileStorage.Contracts.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add permissions**

```csharp
// BrandingPermissions.cs
using SimpleModule.Core.Authorization;

namespace SimpleModule.Branding;

public sealed class BrandingPermissions : IModulePermissions
{
    public const string Manage = "Branding.Manage";
}
```

- [ ] **Step 3: Write the failing service unit test (+ fake settings)**

```csharp
// FakeSettings.cs  — dictionary-backed ISettingsContracts for unit tests
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Branding.Tests;

public sealed class FakeSettings : ISettingsContracts
{
    private readonly Dictionary<string, JsonElement> _app = [];

    public Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null) =>
        Task.FromResult(_app.TryGetValue(key, out var v) ? v.Deserialize<T>() : default);

    public Task<string?> GetSettingAsync(string key, SettingScope scope, string? userId = null) =>
        Task.FromResult(_app.TryGetValue(key, out var v) ? v.GetString() : null);

    public Task SetSettingAsync(string key, JsonElement value, SettingScope scope, string? userId = null)
    {
        _app[key] = value;
        return Task.CompletedTask;
    }

    public Task SetManyAsync(IReadOnlyList<BulkSettingUpdate> updates)
    {
        foreach (var u in updates)
            _app[u.Key] = u.Value;
        return Task.CompletedTask;
    }

    // Unused members — throw so accidental reliance is caught.
    public Task<string?> ResolveUserSettingAsync(string key, string userId) => throw new System.NotSupportedException();
    public Task<JsonElement?> ResolveUserSettingElementAsync(string key, string userId) => throw new System.NotSupportedException();
    public Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null) => throw new System.NotSupportedException();
    public Task ResetToDefaultAsync(string key, SettingScope scope, string? userId = null) => throw new System.NotSupportedException();
    public Task<System.Collections.Generic.IEnumerable<SettingValueDto>> GetSettingValuesAsync(SettingsFilter? filter = null) => throw new System.NotSupportedException();
    public Task<SettingValueDto?> GetSettingValueAsync(string key, SettingScope scope, string? userId = null) => throw new System.NotSupportedException();
}
```

> Copy the exact `ISettingsContracts` member list from `modules/Settings/src/SimpleModule.Settings.Contracts/ISettingsContracts.cs` at implementation time and match every signature — the list above mirrors the researched interface but must compile against the real one.

```csharp
// BrandingServiceTests.cs
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core.Settings;
using Xunit;

public class BrandingServiceTests
{
    [Fact]
    public async Task GetBrandingAsync_ReturnsDefaults_WhenNothingStored()
    {
        var svc = new BrandingService(new FakeSettings());

        var dto = await svc.GetBrandingAsync();

        dto.AppName.Should().Be(BrandingDefaults.AppName);
        dto.ColorPrimary.Should().Be(BrandingDefaults.ColorPrimary);
        dto.LogoUrl.Should().BeNull();
        dto.TopBar.Enabled.Should().BeFalse();
        dto.Footer.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Update_Then_Get_RoundTrips()
    {
        var settings = new FakeSettings();
        var svc = new BrandingService(settings);

        await svc.UpdateAsync(new BrandingEditModel
        {
            AppName = "Acme",
            ColorPrimary = "#112233",
            CustomCss = ".x{color:red}",
            TopBar = new TopBarConfig { Enabled = true, Message = "Hi" },
            Footer = new FooterConfig { Enabled = true, Text = "© Acme" },
        });

        var dto = await svc.GetBrandingAsync();
        dto.AppName.Should().Be("Acme");
        dto.ColorPrimary.Should().Be("#112233");
        dto.TopBar.Message.Should().Be("Hi");
        dto.Footer.Text.Should().Be("© Acme");
        (await svc.GetCustomCssAsync()).Should().Be(".x{color:red}");
    }

    [Fact]
    public async Task GetBrandingAsync_BuildsLogoUrl_WhenFileIdSet()
    {
        var settings = new FakeSettings();
        await settings.SetSettingAsync(BrandingSettingKeys.LogoFileId,
            JsonSerializer.SerializeToElement("abc-123"), SettingScope.Application);
        var svc = new BrandingService(settings);

        var dto = await svc.GetBrandingAsync();

        dto.LogoUrl.Should().Be("/api/branding/assets/logo?v=abc-123");
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~BrandingServiceTests"`
Expected: FAIL — `BrandingService` does not exist yet (won't compile until Step 5/6 wire the test project).

- [ ] **Step 5: Create the test project csproj + FakeSettings**

```xml
<!-- modules/Branding/tests/SimpleModule.Branding.Tests/SimpleModule.Branding.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\SimpleModule.Branding\SimpleModule.Branding.csproj" />
    <ProjectReference Include="..\..\src\SimpleModule.Branding.Contracts\SimpleModule.Branding.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\modules\Settings\src\SimpleModule.Settings.Contracts\SimpleModule.Settings.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\modules\FileStorage\src\SimpleModule.FileStorage.Contracts\SimpleModule.FileStorage.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\modules\Permissions\src\SimpleModule.Permissions.Contracts\SimpleModule.Permissions.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\tests\SimpleModule.Tests.Shared\SimpleModule.Tests.Shared.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 6: Implement `BrandingService`**

```csharp
// BrandingService.cs
using System.Text.Json;
using System.Threading.Tasks;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Branding;

public sealed class BrandingService(ISettingsContracts settings) : IBrandingContracts
{
    public async Task<BrandingDto> GetBrandingAsync()
    {
        var logoId = await settings.GetSettingAsync<string>(BrandingSettingKeys.LogoFileId, SettingScope.Application);
        var faviconId = await settings.GetSettingAsync<string>(BrandingSettingKeys.FaviconFileId, SettingScope.Application);

        return new BrandingDto
        {
            AppName = await Str(BrandingSettingKeys.AppName, BrandingDefaults.AppName),
            ColorPrimary = await Str(BrandingSettingKeys.ColorPrimary, BrandingDefaults.ColorPrimary),
            ColorPrimaryDark = await Str(BrandingSettingKeys.ColorPrimaryDark, BrandingDefaults.ColorPrimaryDark),
            LogoUrl = AssetUrl("logo", logoId),
            FaviconUrl = AssetUrl("favicon", faviconId),
            TopBar = await Json<TopBarConfig>(BrandingSettingKeys.TopBar) ?? new TopBarConfig(),
            Footer = await Json<FooterConfig>(BrandingSettingKeys.Footer) ?? new FooterConfig(),
        };
    }

    public async Task<string> GetCustomCssAsync() =>
        await Str(BrandingSettingKeys.CustomCss, string.Empty);

    public async Task<BrandingEditModel> GetEditableAsync()
    {
        var logoId = await settings.GetSettingAsync<string>(BrandingSettingKeys.LogoFileId, SettingScope.Application);
        var faviconId = await settings.GetSettingAsync<string>(BrandingSettingKeys.FaviconFileId, SettingScope.Application);

        return new BrandingEditModel
        {
            AppName = await Str(BrandingSettingKeys.AppName, BrandingDefaults.AppName),
            ColorPrimary = await Str(BrandingSettingKeys.ColorPrimary, BrandingDefaults.ColorPrimary),
            ColorPrimaryDark = await Str(BrandingSettingKeys.ColorPrimaryDark, BrandingDefaults.ColorPrimaryDark),
            CustomCss = await Str(BrandingSettingKeys.CustomCss, string.Empty),
            LogoFileId = logoId,
            LogoUrl = AssetUrl("logo", logoId),
            FaviconFileId = faviconId,
            FaviconUrl = AssetUrl("favicon", faviconId),
            TopBar = await Json<TopBarConfig>(BrandingSettingKeys.TopBar) ?? new TopBarConfig(),
            Footer = await Json<FooterConfig>(BrandingSettingKeys.Footer) ?? new FooterConfig(),
        };
    }

    public async Task UpdateAsync(BrandingEditModel model)
    {
        var updates = new System.Collections.Generic.List<BulkSettingUpdate>
        {
            Str(BrandingSettingKeys.AppName, model.AppName ?? BrandingDefaults.AppName),
            Str(BrandingSettingKeys.ColorPrimary, model.ColorPrimary ?? BrandingDefaults.ColorPrimary),
            Str(BrandingSettingKeys.ColorPrimaryDark, model.ColorPrimaryDark ?? BrandingDefaults.ColorPrimaryDark),
            Str(BrandingSettingKeys.CustomCss, model.CustomCss ?? string.Empty),
            JsonUpdate(BrandingSettingKeys.TopBar, model.TopBar ?? new TopBarConfig()),
            JsonUpdate(BrandingSettingKeys.Footer, model.Footer ?? new FooterConfig()),
        };
        if (model.LogoFileId is not null)
            updates.Add(Str(BrandingSettingKeys.LogoFileId, model.LogoFileId));
        if (model.FaviconFileId is not null)
            updates.Add(Str(BrandingSettingKeys.FaviconFileId, model.FaviconFileId));

        await settings.SetManyAsync(updates);
    }

    private async Task<string> Str(string key, string fallback)
    {
        var v = await settings.GetSettingAsync<string>(key, SettingScope.Application);
        return string.IsNullOrWhiteSpace(v) ? fallback : v;
    }

    private Task<T?> Json<T>(string key) =>
        settings.GetSettingAsync<T>(key, SettingScope.Application);

    private static string? AssetUrl(string kind, string? fileId) =>
        string.IsNullOrWhiteSpace(fileId) ? null : $"/api/branding/assets/{kind}?v={fileId}";

    private static BulkSettingUpdate Str(string key, string value) => new()
    {
        Key = key,
        Scope = SettingScope.Application,
        Value = JsonSerializer.SerializeToElement(value),
    };

    private static BulkSettingUpdate JsonUpdate<T>(string key, T value) => new()
    {
        Key = key,
        Scope = SettingScope.Application,
        Value = JsonSerializer.SerializeToElement(value),
    };
}
```

- [ ] **Step 7: Implement `BrandingModule` (settings/permissions/menu; service registration)**

```csharp
// BrandingModule.cs
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;

namespace SimpleModule.Branding;

[Module("Branding", ViewPrefix = "/branding")]
public class BrandingModule : IModule
{
    private const string Icon =
        """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M7 21a4 4 0 01-4-4V5a2 2 0 012-2h4a2 2 0 012 2v12a4 4 0 01-4 4zm0 0h12a2 2 0 002-2v-4a2 2 0 00-2-2h-2.343M11 7.343l1.657-1.657a2 2 0 012.828 0l2.829 2.829a2 2 0 010 2.828l-8.486 8.485M7 17h.01"/></svg>""";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IBrandingContracts, BrandingService>();
        // IInertiaHeadContributor + middleware are added in Task 6.
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder) =>
        builder.AddPermissions<BrandingPermissions>();

    public void ConfigureMenu(IMenuBuilder menus) =>
        menus.Add(new MenuItem
        {
            Label = "Branding",
            Url = "/branding",
            Icon = Icon,
            Order = 86,
            Section = MenuSection.AppSidebar,
            Roles = ["Admin"],
            RequiredPermission = BrandingPermissions.Manage,
        });

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(Def(BrandingSettingKeys.AppName, "Application name", SettingType.Text, JsonSerializer.Serialize(BrandingDefaults.AppName), order: 0))
            .Add(Def(BrandingSettingKeys.ColorPrimary, "Primary color (light)", SettingType.Color, JsonSerializer.Serialize(BrandingDefaults.ColorPrimary), order: 1))
            .Add(Def(BrandingSettingKeys.ColorPrimaryDark, "Primary color (dark)", SettingType.Color, JsonSerializer.Serialize(BrandingDefaults.ColorPrimaryDark), order: 2))
            .Add(Def(BrandingSettingKeys.CustomCss, "Custom CSS", SettingType.MultilineText, "\"\"", order: 3))
            .Add(Def(BrandingSettingKeys.LogoFileId, "Logo file id", SettingType.Text, "\"\"", order: 4))
            .Add(Def(BrandingSettingKeys.FaviconFileId, "Favicon file id", SettingType.Text, "\"\"", order: 5))
            .Add(Def(BrandingSettingKeys.TopBar, "Top bar", SettingType.Json, JsonSerializer.Serialize(new TopBarConfig()), order: 6))
            .Add(Def(BrandingSettingKeys.Footer, "Footer", SettingType.Json, JsonSerializer.Serialize(new FooterConfig()), order: 7));
    }

    private static SettingDefinition Def(string key, string name, SettingType type, string defaultValue, int order) =>
        new()
        {
            Key = key,
            DisplayName = name,
            Group = "Branding",
            Scope = SettingScope.Application,
            Type = type,
            DefaultValue = defaultValue,
            Order = order,
        };
}
```

- [ ] **Step 8: Register projects in `SimpleModule.slnx` and Host csproj**

Add to `SimpleModule.slnx`: the main project and the test project. Add to `template/SimpleModule.Host/SimpleModule.Host.csproj` a `<ProjectReference Include="..\..\modules\Branding\src\SimpleModule.Branding\SimpleModule.Branding.csproj" />` alongside the other module references.

- [ ] **Step 9: Run tests to verify they pass + build host**

Run: `dotnet test --filter "FullyQualifiedName~BrandingServiceTests"` → PASS.
Run: `dotnet build template/SimpleModule.Host/SimpleModule.Host.csproj` → Build succeeded (module discovered by source generator).

- [ ] **Step 10: Commit**

```bash
git add modules/Branding SimpleModule.slnx template/SimpleModule.Host/SimpleModule.Host.csproj
git commit -m "feat(branding): module, settings, permission, menu, BrandingService"
```

---

## Task 4: Admin API + asset endpoints + view endpoint

**Files:**
- Create: `modules/Branding/src/SimpleModule.Branding/Endpoints/Branding/GetBrandingEndpoint.cs`
- Create: `modules/Branding/src/SimpleModule.Branding/Endpoints/Branding/UpdateBrandingEndpoint.cs`
- Create: `modules/Branding/src/SimpleModule.Branding/Endpoints/Branding/UploadAssetEndpoint.cs`
- Create: `modules/Branding/src/SimpleModule.Branding/Endpoints/Branding/AssetEndpoint.cs`
- Create: `modules/Branding/src/SimpleModule.Branding/Pages/ManageEndpoint.cs`
- Create: `modules/Branding/src/SimpleModule.Branding/Pages/index.ts`
- Test: `modules/Branding/tests/SimpleModule.Branding.Tests/Integration/BrandingEndpointsTests.cs`

**Interfaces:**
- Consumes: `IBrandingContracts`, `ISettingsContracts`, `IFileStorageContracts`, `BrandingPermissions`, settings keys.
- Produces: routes `GET/PUT /api/branding`, `POST /api/branding/assets/{kind}`, `GET /api/branding/assets/{kind}`, `GET /branding`.

- [ ] **Step 1: Write the failing integration tests**

```csharp
// Integration/BrandingEndpointsTests.cs
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Tests.Shared;
using Xunit;

[Collection(TestCollections.Integration)]
public class BrandingEndpointsTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;
    public BrandingEndpointsTests(SimpleModuleWebApplicationFactory factory) => _factory = factory;

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var claims = $"{ClaimTypes.Role}=Admin;{ClaimTypes.NameIdentifier}=admin-branding-test";
        client.DefaultRequestHeaders.Add("X-Test-Claims", claims);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        return client;
    }

    [Fact]
    public async Task Get_Requires_Permission()
    {
        var anon = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var res = await anon.GetAsync("/api/branding");
        res.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Put_Then_Get_RoundTrips()
    {
        var client = AdminClient();
        var model = new BrandingEditModel { AppName = "Acme Co", ColorPrimary = "#123456" };

        var put = await client.PutAsJsonAsync("/api/branding", model);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var got = await client.GetFromJsonAsync<BrandingEditModel>("/api/branding");
        got!.AppName.Should().Be("Acme Co");
        got.ColorPrimary.Should().Be("#123456");
    }

    [Fact]
    public async Task Asset_Serve_Returns404_WhenUnset()
    {
        var anon = _factory.CreateClient();
        var res = await anon.GetAsync("/api/branding/assets/logo");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ManageView_Requires_Permission_And_RendersForAdmin()
    {
        var admin = AdminClient();
        var res = await admin.GetAsync("/branding");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

> The exact unauthorized status depends on the app's auth fallback (cookie redirect vs 401/403). `BeOneOf(...)` keeps the test robust; tighten after observing actual behavior. If the integration collection requires a token-seeded user with the `Branding.Manage` permission rather than the `Admin` role, follow the seeding pattern used by `modules/Settings` or `modules/FileStorage` integration tests. Admin role bypasses permission checks (per `MenuItem`/permission semantics), so `Role=Admin` should satisfy `RequirePermission` — verify against an existing admin-gated endpoint test.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~BrandingEndpointsTests"`
Expected: FAIL — endpoints/routes don't exist (404).

- [ ] **Step 3: Implement the admin GET/PUT endpoints**

```csharp
// Endpoints/Branding/GetBrandingEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Branding.Endpoints.Branding;

public class GetBrandingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/branding", async (IBrandingContracts branding) =>
                TypedResults.Ok(await branding.GetEditableAsync()))
            .RequirePermission(BrandingPermissions.Manage);
}
```

```csharp
// Endpoints/Branding/UpdateBrandingEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Branding.Endpoints.Branding;

public class UpdateBrandingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/branding", async (BrandingEditModel model, IBrandingContracts branding) =>
            {
                await branding.UpdateAsync(model);
                return TypedResults.Ok();
            })
            .RequirePermission(BrandingPermissions.Manage)
            .DisableAntiforgery();
}
```

- [ ] **Step 4: Implement the upload endpoint**

```csharp
// Endpoints/Branding/UploadAssetEndpoint.cs
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Security; // GetUserId() — verify namespace in FileStorage UploadEndpoint
using SimpleModule.Core.Settings;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Branding.Endpoints.Branding;

public class UploadAssetEndpoint : IEndpoint
{
    private const long MaxBytes = 2 * 1024 * 1024;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/branding/assets/{kind}", async Task<IResult> (
                string kind,
                IFormFile? file,
                HttpContext context,
                IFileStorageContracts files,
                ISettingsContracts settings) =>
            {
                if (kind is not ("logo" or "favicon"))
                    return TypedResults.BadRequest("Invalid asset kind.");
                if (file is null || file.Length == 0)
                    return TypedResults.BadRequest("A file is required.");
                if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return TypedResults.BadRequest("Only image files are allowed.");
                if (file.Length > MaxBytes)
                    return TypedResults.BadRequest("File too large (max 2 MB).");

                var userId = context.User.GetUserId();
                await using var stream = file.OpenReadStream();
                var stored = await files.UploadFileAsync(stream, file.FileName, file.ContentType, "branding", userId);

                var key = kind == "logo" ? BrandingSettingKeys.LogoFileId : BrandingSettingKeys.FaviconFileId;
                var fileId = stored.Id.ToString();
                await settings.SetSettingAsync(key, JsonSerializer.SerializeToElement(fileId), SettingScope.Application);

                return TypedResults.Ok(new { fileId, url = $"/api/branding/assets/{kind}?v={fileId}" });
            })
            .RequirePermission(BrandingPermissions.Manage)
            .DisableAntiforgery();
}
```

> Verify `context.User.GetUserId()`'s namespace by reading `modules/FileStorage/.../Endpoints/Files/UploadEndpoint.cs` (it uses the same call) and copy its `using`. `stored.Id.ToString()` must yield a value `Guid.TryParse` can round-trip in the serve endpoint — confirm `FileStorageId.ToString()` returns the bare GUID (read `FileStorageId`); if it returns a wrapped form, store `stored.Id.Value.ToString()` instead and adjust `AssetUrl`/serve parsing to match.

- [ ] **Step 5: Implement the anonymous serve endpoint**

```csharp
// Endpoints/Branding/AssetEndpoint.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Branding.Endpoints.Branding;

public class AssetEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/branding/assets/{kind}", async Task<IResult> (
                string kind,
                HttpContext context,
                ISettingsContracts settings,
                IFileStorageContracts files) =>
            {
                if (kind is not ("logo" or "favicon"))
                    return Results.NotFound();

                var key = kind == "logo" ? BrandingSettingKeys.LogoFileId : BrandingSettingKeys.FaviconFileId;
                var idStr = await settings.GetSettingAsync<string>(key, SettingScope.Application);
                if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var guid))
                    return Results.NotFound();

                var file = await files.GetFileByIdAsync(FileStorageId.From(guid));
                if (file is null)
                    return Results.NotFound();

                var stream = await files.DownloadFileAsync(file);
                if (stream is null)
                    return Results.NotFound();

                context.Response.Headers.CacheControl = "public, max-age=3600";
                return Results.File(stream, file.ContentType);
            })
            .AllowAnonymous();
}
```

> `FileStorageId.From(Guid)` — confirm the exact factory on `FileStorageId` (strongly-typed id; the repo uses value converters for it). If the factory differs (e.g. `FileStorageId.FromGuid`, or it wraps a `long`), adjust both this parse and the stored representation in Task 4 Step 4 so they round-trip.

- [ ] **Step 6: Implement the view endpoint + Pages/index.ts**

```csharp
// Pages/ManageEndpoint.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Branding.Pages;

public class ManageEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/branding", async (IBrandingContracts branding) =>
                Inertia.Render("Branding/Manage", new { branding = await branding.GetEditableAsync() }))
            .RequirePermission(BrandingPermissions.Manage);
}
```

```ts
// Pages/index.ts
export const pages: Record<string, unknown> = {
  'Branding/Manage': () => import('./Manage'),
};
```

> `Manage.tsx` is created in Task 6; `validate-pages` is only run after that. To keep the build green now, you may create a minimal placeholder `Pages/Manage.tsx` (`export default function Manage(){return null}`) and flesh it out in Task 6, or defer creating `Pages/index.ts` until Task 6. Prefer the placeholder so the C# endpoint and registry stay in sync.

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~BrandingEndpointsTests"`
Expected: PASS (all four).

- [ ] **Step 8: Commit**

```bash
git add modules/Branding/src/SimpleModule.Branding/Endpoints modules/Branding/src/SimpleModule.Branding/Pages modules/Branding/tests
git commit -m "feat(branding): admin API, asset upload/serve, manage view endpoint"
```

---

## Task 5: Shared-prop middleware + head contributor

**Files:**
- Create: `modules/Branding/src/SimpleModule.Branding/BrandingSharedDataMiddleware.cs`
- Create: `modules/Branding/src/SimpleModule.Branding/BrandingHeadContributor.cs`
- Modify: `modules/Branding/src/SimpleModule.Branding/BrandingModule.cs` (register contributor in `ConfigureServices`; add `ConfigureMiddleware`)
- Test: `modules/Branding/tests/SimpleModule.Branding.Tests/Integration/BrandingRenderingTests.cs`

**Interfaces:**
- Consumes: `IBrandingContracts`, `InertiaSharedData`, `IInertiaHeadContributor` (Task 2).
- Produces: `branding` shared Inertia prop on every page; `<head>` `<style>`/`<link rel=icon>` injection when non-default.

- [ ] **Step 1: Write the failing integration tests**

```csharp
// Integration/BrandingRenderingTests.cs
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Tests.Shared;
using Xunit;

[Collection(TestCollections.Integration)]
public class BrandingRenderingTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;
    public BrandingRenderingTests(SimpleModuleWebApplicationFactory factory) => _factory = factory;

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Claims", $"{ClaimTypes.Role}=Admin;{ClaimTypes.NameIdentifier}=branding-render");
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        return client;
    }

    [Fact]
    public async Task FullPage_Includes_BrandingSharedProp()
    {
        var html = await _factory.CreateClient().GetStringAsync("/");
        // shared prop name appears in the embedded page JSON
        html.Should().Contain("\"branding\"");
    }

    [Fact]
    public async Task FullPage_Injects_PrimaryColor_WhenChanged()
    {
        var admin = AdminClient();
        await admin.PutAsJsonAsync("/api/branding", new BrandingEditModel { ColorPrimary = "#abcdef" });

        var html = await _factory.CreateClient().GetStringAsync("/");
        html.Should().Contain("--color-primary:#abcdef");
    }
}
```

> If the integration collection shares one app instance, the PUT in the second test persists into shared SQLite — fine. If tests run with isolated DBs per class, the PUT and the subsequent GET must use the same instance (`_factory`). Adjust the second GET to use `admin`/same factory if needed.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~BrandingRenderingTests"`
Expected: FAIL — no `branding` prop, no color injection.

- [ ] **Step 3: Implement the shared-data middleware**

```csharp
// BrandingSharedDataMiddleware.cs
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Branding;

public sealed class BrandingSharedDataMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IBrandingContracts branding)
    {
        var sharedData = context.RequestServices.GetService<InertiaSharedData>();
        if (sharedData is not null)
            sharedData.Set("branding", await branding.GetBrandingAsync());

        await next(context);
    }
}
```

- [ ] **Step 4: Implement the head contributor**

```csharp
// BrandingHeadContributor.cs
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Branding;

public sealed partial class BrandingHeadContributor(IBrandingContracts branding) : IInertiaHeadContributor
{
    public async ValueTask<string?> GetHeadHtmlAsync(HttpContext context)
    {
        var b = await branding.GetBrandingAsync();
        var css = await branding.GetCustomCssAsync();

        var colorChanged =
            !ColorEquals(b.ColorPrimary, BrandingDefaults.ColorPrimary)
            || !ColorEquals(b.ColorPrimaryDark, BrandingDefaults.ColorPrimaryDark);
        var hasCss = !string.IsNullOrWhiteSpace(css);

        var sb = new StringBuilder();
        if (colorChanged || hasCss)
        {
            sb.Append("<style>");
            if (colorChanged)
            {
                sb.Append(":root{--color-primary:").Append(SanitizeColor(b.ColorPrimary)).Append(";}");
                sb.Append(".dark{--color-primary:").Append(SanitizeColor(b.ColorPrimaryDark)).Append(";}");
            }
            if (hasCss)
                sb.Append(StripClosingStyle(css));
            sb.Append("</style>");
        }

        if (!string.IsNullOrWhiteSpace(b.FaviconUrl))
            sb.Append("<link rel=\"icon\" href=\"").Append(EncodeAttr(b.FaviconUrl)).Append("\" />");

        return sb.Length == 0 ? null : sb.ToString();
    }

    private static bool ColorEquals(string a, string b) =>
        string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);

    private static string SanitizeColor(string color) =>
        HexColor().IsMatch(color) ? color : BrandingDefaults.ColorPrimary;

    private static string StripClosingStyle(string css) =>
        ClosingStyle().Replace(css, string.Empty);

    private static string EncodeAttr(string value) =>
        value.Replace("\"", "&quot;", System.StringComparison.Ordinal);

    [GeneratedRegex("^#[0-9a-fA-F]{3,8}$")]
    private static partial Regex HexColor();

    [GeneratedRegex("</style>", RegexOptions.IgnoreCase)]
    private static partial Regex ClosingStyle();
}
```

- [ ] **Step 5: Wire registration in `BrandingModule`**

In `BrandingModule.ConfigureServices`, add the contributor registration; add `ConfigureMiddleware`:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IBrandingContracts, BrandingService>();
    services.AddScoped<SimpleModule.Core.Inertia.IInertiaHeadContributor, BrandingHeadContributor>();
}

public void ConfigureMiddleware(Microsoft.AspNetCore.Builder.IApplicationBuilder app)
{
    app.UseMiddleware<BrandingSharedDataMiddleware>();
}
```

(Add `using Microsoft.AspNetCore.Builder;` and `using SimpleModule.Core.Inertia;` to the top instead of inlining the fully-qualified names if you prefer — match file style.)

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~BrandingRenderingTests"`
Expected: PASS. Also re-run `HeadContributorTests` to confirm no regression.

> If `FullPage_Injects_PrimaryColor_WhenChanged` fails because module `ConfigureMiddleware` runs too late to set shared data / contributors aren't resolved on the `/` request, confirm the branding middleware is in the pipeline before endpoint execution (compare with `SimpleModule.Localization`'s middleware). The head contributor path is independent of the middleware (resolved directly by the renderer), so color injection should work even if the shared-prop timing needs adjustment.

- [ ] **Step 7: Commit**

```bash
git add modules/Branding/src/SimpleModule.Branding modules/Branding/tests
git commit -m "feat(branding): shared-prop middleware + head contributor (colors/css/favicon)"
```

---

## Task 6: Frontend — shared types, chrome (brand mark, top bar, footer), layout edits

**Files:**
- Modify: `packages/SimpleModule.UI/components/layouts/types.ts`
- Create: `packages/SimpleModule.UI/components/layouts/brand-mark.tsx`
- Create: `packages/SimpleModule.UI/components/layouts/top-bar.tsx`
- Create: `packages/SimpleModule.UI/components/layouts/footer.tsx`
- Modify: `packages/SimpleModule.UI/components/layouts/app-layout.tsx`
- Modify: `packages/SimpleModule.UI/components/layouts/public-layout.tsx`
- Modify: `packages/SimpleModule.UI/components/layouts/index.ts` (export new pieces if needed)

**Interfaces:**
- Consumes: `branding` shared prop (Task 5).
- Produces: `BrandingProps`/`TopBarConfig`/`FooterConfig`/`BrandingLink` TS types on `SharedProps.branding`; `<BrandMark/>`, `<TopBar/>`, `<Footer/>` used in both layouts.

- [ ] **Step 1: Extend `types.ts`**

Append the branding types and add `branding` to `SharedProps`:

```ts
export interface BrandingLink {
  label: string;
  url: string;
}

export interface TopBarConfig {
  enabled: boolean;
  message: string;
  backgroundColor: string;
  textColor: string;
  links: BrandingLink[];
  dismissible: boolean;
}

export interface FooterConfig {
  enabled: boolean;
  text: string;
  links: BrandingLink[];
  showCopyright: boolean;
}

export interface BrandingProps {
  appName: string;
  logoUrl: string | null;
  faviconUrl: string | null;
  colorPrimary: string;
  colorPrimaryDark: string;
  topBar: TopBarConfig;
  footer: FooterConfig;
}
```

Add to the `SharedProps` interface:
```ts
  branding: BrandingProps;
```

- [ ] **Step 2: Create `brand-mark.tsx` (DRY logo/name)**

```tsx
// brand-mark.tsx
import { usePage } from '@inertiajs/react';
import type { SharedProps } from './types';

export function BrandMark() {
  const { props } = usePage<SharedProps & Record<string, unknown>>();
  const branding = props.branding;
  const appName = branding?.appName ?? 'SimpleModule';

  if (branding?.logoUrl) {
    return <img src={branding.logoUrl} alt={appName} className="h-8 w-auto max-w-[160px] object-contain" />;
  }

  return (
    <>
      <span
        className="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-bold shadow-md transition-transform duration-200 group-hover:scale-105 shrink-0"
        style={{ background: 'var(--color-primary)' }}
      >
        {appName.charAt(0).toUpperCase()}
      </span>
      <span className="text-base sidebar-label">{appName}</span>
    </>
  );
}
```

- [ ] **Step 3: Create `top-bar.tsx`**

```tsx
// top-bar.tsx
import { usePage } from '@inertiajs/react';
import * as React from 'react';
import type { SharedProps } from './types';

export function TopBar() {
  const { props } = usePage<SharedProps & Record<string, unknown>>();
  const topBar = props.branding?.topBar;
  const [dismissed, setDismissed] = React.useState(false);

  React.useEffect(() => {
    if (topBar?.dismissible) {
      setDismissed(localStorage.getItem('branding-topbar-dismissed') === 'true');
    }
  }, [topBar?.dismissible]);

  if (!topBar?.enabled || dismissed || !topBar.message) return null;

  const dismiss = () => {
    setDismissed(true);
    localStorage.setItem('branding-topbar-dismissed', 'true');
  };

  return (
    <div
      className="w-full px-4 py-2 text-sm flex items-center justify-center gap-4"
      style={{ background: topBar.backgroundColor, color: topBar.textColor }}
    >
      <span>{topBar.message}</span>
      {topBar.links.map((link) => (
        <a key={link.url} href={link.url} className="underline font-medium" style={{ color: topBar.textColor }}>
          {link.label}
        </a>
      ))}
      {topBar.dismissible && (
        <button
          type="button"
          onClick={dismiss}
          aria-label="Dismiss"
          className="ml-auto opacity-80 hover:opacity-100"
          style={{ color: topBar.textColor }}
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth={2} viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      )}
    </div>
  );
}
```

- [ ] **Step 4: Create `footer.tsx`**

```tsx
// footer.tsx
import { usePage } from '@inertiajs/react';
import type { SharedProps } from './types';

export function Footer() {
  const { props } = usePage<SharedProps & Record<string, unknown>>();
  const footer = props.branding?.footer;
  const appName = props.branding?.appName ?? 'SimpleModule';
  if (!footer?.enabled) return null;

  const year = new Date().getFullYear();

  return (
    <footer className="border-t border-border px-6 py-6 text-sm text-text-muted">
      <div className="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-3">
        <div>
          {footer.showCopyright && <span>© {year} {appName}. </span>}
          {footer.text && <span>{footer.text}</span>}
        </div>
        <div className="flex items-center gap-4">
          {footer.links.map((link) => (
            <a key={link.url} href={link.url} className="hover:text-text no-underline">
              {link.label}
            </a>
          ))}
        </div>
      </div>
    </footer>
  );
}
```

- [ ] **Step 5: Edit `app-layout.tsx`**

1. Add imports at top:
```tsx
import { BrandMark } from './brand-mark';
import { Footer } from './footer';
import { TopBar } from './top-bar';
```
2. Read `branding` and set the document title (after the existing `const { auth, menus, csrfToken } = props;` line, change it to include branding and add an effect):
```tsx
  const { auth, menus, csrfToken, branding } = props;
  React.useEffect(() => {
    if (branding?.appName) document.title = branding.appName;
  }, [branding?.appName]);
```
3. Replace the **mobile header** logo block (the `<span … >S</span><span className="text-sm">SimpleModule</span>` inside the mobile header `<Link>`) so the `<Link>` renders `<BrandMark />` instead of the hardcoded badge+text. Keep the `<Link href="/" …>` wrapper.
4. Replace the **sidebar** logo block similarly: inside the sidebar logo `<Link>`, render `<BrandMark />`.
5. Render `<TopBar />` as the very first child inside the top-level `<div className="app-layout">` (above the mobile header) so it spans full width.
6. Render `<Footer />` inside `<main className="app-content">` after the `{children}` wrapper:
```tsx
      <main className="app-content">
        <div className="mt-8 mb-16">{children}</div>
        <Footer />
      </main>
```

> Keep the existing class names and structure; only swap the hardcoded brand markup for `<BrandMark/>` and add `<TopBar/>`/`<Footer/>`. The mobile-header badge used `w-7 h-7`/`text-xs`; `BrandMark` standardizes to `w-8 h-8`. That minor size change is acceptable; if pixel-fidelity matters, add a `size` prop to `BrandMark`.

- [ ] **Step 6: Edit `public-layout.tsx`**

1. Add imports:
```tsx
import { BrandMark } from './brand-mark';
import { Footer } from './footer';
import { TopBar } from './top-bar';
```
2. Replace the hardcoded badge+text inside the nav's brand `<Link>` with `<BrandMark />`.
3. Render `<TopBar />` as the first element returned (above the `<nav>`), and `<Footer />` after `<main>`:
```tsx
  return (
    <>
      <TopBar />
      <nav …>…</nav>
      <MobileOverlay … />
      <main className="max-w-7xl mx-auto mt-8 mb-16 px-4 sm:px-6">{children}</main>
      <Footer />
    </>
  );
```

- [ ] **Step 7: Export new pieces (optional) + typecheck**

If other code needs them, add to `packages/SimpleModule.UI/components/layouts/index.ts`:
```ts
export { BrandMark } from './brand-mark';
export { TopBar } from './top-bar';
export { Footer } from './footer';
export type { BrandingLink, BrandingProps, FooterConfig, TopBarConfig } from './types';
```

Run: `npm run check`
Expected: Biome + typecheck pass (fix any TS errors — e.g. `branding` possibly undefined → already guarded with `?.`).

- [ ] **Step 8: Commit**

```bash
git add packages/SimpleModule.UI/components/layouts
git commit -m "feat(ui): branding-aware layout chrome (brand mark, top bar, footer)"
```

---

## Task 7: Frontend — Branding admin page (`Manage.tsx`)

**Files:**
- Create: `modules/Branding/src/SimpleModule.Branding/Pages/Manage.tsx`
- Create: `modules/Branding/src/SimpleModule.Branding/Pages/index.ts` (if deferred from Task 4)
- Create: `modules/Branding/src/SimpleModule.Branding/vite.config.ts`
- Create: `modules/Branding/src/SimpleModule.Branding/package.json`
- Create: `modules/Branding/src/SimpleModule.Branding/tsconfig.json`
- Modify: root `package.json`/workspaces only if module globs don't already include `modules/*/src/*` (they do — verify).

**Interfaces:**
- Consumes: `BrandingEditModel` shape (props.branding) from `ManageEndpoint`; `@simplemodule/ui` components; `csrfToken` shared prop.
- Produces: the `Branding/Manage` page registered in `Pages/index.ts`.

- [ ] **Step 1: Add the Vite/package/tsconfig (match Settings module exactly)**

```ts
// vite.config.ts
import { defineModuleConfig } from '@simplemodule/client/module';
export default defineModuleConfig(import.meta.dirname);
```

```json
// package.json
{
  "private": true,
  "name": "@simplemodule/branding",
  "version": "0.0.0",
  "scripts": {
    "build": "vite build --configLoader runner",
    "build:dev": "cross-env VITE_MODE=dev vite build --configLoader runner",
    "watch": "cross-env VITE_MODE=dev vite build --configLoader runner --watch"
  },
  "peerDependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  }
}
```

```json
// tsconfig.json
{
  "extends": "@simplemodule/tsconfig/base",
  "compilerOptions": {
    "paths": { "@/*": ["./*"] }
  }
}
```

- [ ] **Step 2: Implement `Manage.tsx`**

A single-form admin page with sections + a save button + asset uploads + a live preview. Uses `fetch` for save/upload (endpoints have `DisableAntiforgery`).

```tsx
// Pages/Manage.tsx
import { router, usePage } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Field,
  FieldGroup,
  Input,
  Label,
  PageShell,
  Switch,
  Textarea,
} from '@simplemodule/ui';
import * as React from 'react';

interface BrandingLink {
  label: string;
  url: string;
}
interface TopBarConfig {
  enabled: boolean;
  message: string;
  backgroundColor: string;
  textColor: string;
  links: BrandingLink[];
  dismissible: boolean;
}
interface FooterConfig {
  enabled: boolean;
  text: string;
  links: BrandingLink[];
  showCopyright: boolean;
}
interface BrandingEditModel {
  appName: string;
  logoFileId: string | null;
  logoUrl: string | null;
  faviconFileId: string | null;
  faviconUrl: string | null;
  colorPrimary: string;
  colorPrimaryDark: string;
  customCss: string;
  topBar: TopBarConfig;
  footer: FooterConfig;
}

export default function Manage() {
  const { branding } = usePage<{ branding: BrandingEditModel }>().props;
  const [model, setModel] = React.useState<BrandingEditModel>(branding);
  const [saving, setSaving] = React.useState(false);
  const [saved, setSaved] = React.useState(false);

  const set = <K extends keyof BrandingEditModel>(key: K, value: BrandingEditModel[K]) =>
    setModel((m) => ({ ...m, [key]: value }));

  const setTopBar = <K extends keyof TopBarConfig>(key: K, value: TopBarConfig[K]) =>
    setModel((m) => ({ ...m, topBar: { ...m.topBar, [key]: value } }));

  const setFooter = <K extends keyof FooterConfig>(key: K, value: FooterConfig[K]) =>
    setModel((m) => ({ ...m, footer: { ...m.footer, [key]: value } }));

  async function uploadAsset(kind: 'logo' | 'favicon', file: File) {
    const data = new FormData();
    data.append('file', file);
    const res = await fetch(`/api/branding/assets/${kind}`, { method: 'POST', body: data });
    if (!res.ok) return;
    const json = (await res.json()) as { fileId: string; url: string };
    if (kind === 'logo') {
      set('logoFileId', json.fileId);
      set('logoUrl', json.url);
    } else {
      set('faviconFileId', json.fileId);
      set('faviconUrl', json.url);
    }
  }

  async function save() {
    setSaving(true);
    try {
      const res = await fetch('/api/branding', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(model),
      });
      if (res.ok) {
        setSaved(true);
        setTimeout(() => setSaved(false), 2000);
        router.reload();
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <PageShell title="Branding" description="Customize the appearance of your application.">
      <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Identity</CardTitle>
            </CardHeader>
            <CardContent>
              <FieldGroup>
                <Field>
                  <Label htmlFor="appName">Application name</Label>
                  <Input
                    id="appName"
                    value={model.appName}
                    onChange={(e) => set('appName', e.target.value)}
                  />
                </Field>
                <Field>
                  <Label htmlFor="logo">Logo</Label>
                  {model.logoUrl && (
                    <img src={model.logoUrl} alt="Logo preview" className="h-10 w-auto mb-2" />
                  )}
                  <input
                    id="logo"
                    type="file"
                    accept="image/*"
                    onChange={(e) => {
                      const f = e.target.files?.[0];
                      if (f) void uploadAsset('logo', f);
                    }}
                  />
                  {model.logoFileId && (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => {
                        set('logoFileId', '');
                        set('logoUrl', null);
                      }}
                    >
                      Remove logo
                    </Button>
                  )}
                </Field>
                <Field>
                  <Label htmlFor="favicon">Favicon</Label>
                  {model.faviconUrl && (
                    <img src={model.faviconUrl} alt="Favicon preview" className="h-6 w-6 mb-2" />
                  )}
                  <input
                    id="favicon"
                    type="file"
                    accept="image/*"
                    onChange={(e) => {
                      const f = e.target.files?.[0];
                      if (f) void uploadAsset('favicon', f);
                    }}
                  />
                </Field>
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Colors</CardTitle>
            </CardHeader>
            <CardContent>
              <FieldGroup>
                <Field>
                  <Label htmlFor="primary">Primary color (light)</Label>
                  <input
                    id="primary"
                    type="color"
                    value={model.colorPrimary}
                    onChange={(e) => set('colorPrimary', e.target.value)}
                    className="h-9 w-16 cursor-pointer rounded border-0 bg-transparent p-0"
                  />
                </Field>
                <Field>
                  <Label htmlFor="primaryDark">Primary color (dark)</Label>
                  <input
                    id="primaryDark"
                    type="color"
                    value={model.colorPrimaryDark}
                    onChange={(e) => set('colorPrimaryDark', e.target.value)}
                    className="h-9 w-16 cursor-pointer rounded border-0 bg-transparent p-0"
                  />
                </Field>
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Top bar</CardTitle>
            </CardHeader>
            <CardContent>
              <FieldGroup>
                <Field className="flex items-center gap-3">
                  <Switch
                    id="topbar-enabled"
                    checked={model.topBar.enabled}
                    onCheckedChange={(v) => setTopBar('enabled', v)}
                  />
                  <Label htmlFor="topbar-enabled">Show top bar</Label>
                </Field>
                <Field>
                  <Label htmlFor="topbar-message">Message</Label>
                  <Input
                    id="topbar-message"
                    value={model.topBar.message}
                    onChange={(e) => setTopBar('message', e.target.value)}
                  />
                </Field>
                <Field className="flex items-center gap-3">
                  <Switch
                    id="topbar-dismissible"
                    checked={model.topBar.dismissible}
                    onCheckedChange={(v) => setTopBar('dismissible', v)}
                  />
                  <Label htmlFor="topbar-dismissible">Dismissible</Label>
                </Field>
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Footer</CardTitle>
            </CardHeader>
            <CardContent>
              <FieldGroup>
                <Field className="flex items-center gap-3">
                  <Switch
                    id="footer-enabled"
                    checked={model.footer.enabled}
                    onCheckedChange={(v) => setFooter('enabled', v)}
                  />
                  <Label htmlFor="footer-enabled">Show footer</Label>
                </Field>
                <Field>
                  <Label htmlFor="footer-text">Footer text</Label>
                  <Input
                    id="footer-text"
                    value={model.footer.text}
                    onChange={(e) => setFooter('text', e.target.value)}
                  />
                </Field>
                <Field className="flex items-center gap-3">
                  <Switch
                    id="footer-copyright"
                    checked={model.footer.showCopyright}
                    onCheckedChange={(v) => setFooter('showCopyright', v)}
                  />
                  <Label htmlFor="footer-copyright">Show copyright</Label>
                </Field>
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Advanced — Custom CSS</CardTitle>
            </CardHeader>
            <CardContent>
              <Textarea
                rows={8}
                value={model.customCss}
                placeholder=":root { --color-primary: #059669; }"
                onChange={(e) => set('customCss', e.target.value)}
                className="font-mono text-sm"
              />
              <p className="text-xs text-text-muted mt-2">
                Injected globally into the page head. Use with care — invalid CSS can break the UI.
              </p>
            </CardContent>
          </Card>

          <div className="flex items-center gap-3">
            <Button onClick={() => void save()} disabled={saving}>
              {saving ? 'Saving…' : 'Save changes'}
            </Button>
            {saved && <span className="text-sm text-green-600">Saved ✓</span>}
          </div>
        </div>

        {/* Live preview */}
        <div className="space-y-3">
          <p className="text-sm font-semibold text-text-muted">Preview</p>
          <div className="rounded-xl border border-border overflow-hidden">
            {model.topBar.enabled && model.topBar.message && (
              <div
                className="px-3 py-2 text-xs text-center"
                style={{ background: model.topBar.backgroundColor, color: model.topBar.textColor }}
              >
                {model.topBar.message}
              </div>
            )}
            <div className="flex items-center gap-2 p-3 border-b border-border">
              {model.logoUrl ? (
                <img src={model.logoUrl} alt="" className="h-7 w-auto" />
              ) : (
                <span
                  className="w-7 h-7 rounded-lg flex items-center justify-center text-white text-xs font-bold"
                  style={{ background: model.colorPrimary }}
                >
                  {(model.appName || 'S').charAt(0).toUpperCase()}
                </span>
              )}
              <span className="text-sm font-bold">{model.appName || 'SimpleModule'}</span>
            </div>
            <div className="p-3">
              <button
                type="button"
                className="rounded-md px-3 py-1.5 text-xs font-medium text-white"
                style={{ background: model.colorPrimary }}
              >
                Primary button
              </button>
            </div>
            {model.footer.enabled && (
              <div className="border-t border-border px-3 py-2 text-xs text-text-muted">
                {model.footer.showCopyright && `© ${new Date().getFullYear()} ${model.appName}. `}
                {model.footer.text}
              </div>
            )}
          </div>
        </div>
      </div>
    </PageShell>
  );
}
```

> Verify component import names against `packages/SimpleModule.UI/components/index.ts` (e.g. `Switch`, `Textarea`, `Field`, `FieldGroup`, `PageShell`, `Card*` all exist per research). If `Field`/`FieldGroup` props differ, fall back to plain `div`s with `Label`. The links-editor for top bar/footer is intentionally minimal in v1 (enable/message/text/toggles); add a repeatable link rows editor only if needed.

- [ ] **Step 3: Ensure `Pages/index.ts` is present** (created in Task 4 or here):

```ts
export const pages: Record<string, unknown> = {
  'Branding/Manage': () => import('./Manage'),
};
```

- [ ] **Step 4: Install workspace + build the module**

Run: `npm install`
Run: `npm run build:dev --workspace @simplemodule/branding` (or `npm run dev:build`)
Expected: builds `wwwroot/SimpleModule.Branding.pages.js`.

- [ ] **Step 5: Validate pages + checks**

Run: `npm run validate-pages`
Expected: PASS (Branding/Manage matches the `Inertia.Render("Branding/Manage", …)`).
Run: `npm run check`
Expected: Biome + typecheck pass.

- [ ] **Step 6: Commit**

```bash
git add modules/Branding/src/SimpleModule.Branding/Pages \
        modules/Branding/src/SimpleModule.Branding/vite.config.ts \
        modules/Branding/src/SimpleModule.Branding/package.json \
        modules/Branding/src/SimpleModule.Branding/tsconfig.json \
        package-lock.json
git commit -m "feat(branding): admin Manage page with live preview"
```

---

## Task 8: Full verification (build, tests, CI, e2e) + final commit

**Files:** none (verification) — fix-ups land in the relevant task's files.

- [ ] **Step 1: Backend build + full test run**

Run: `dotnet build`
Expected: 0 warnings, 0 errors (TreatWarningsAsErrors).
Run: `dotnet test`
Expected: all green, including new Branding tests + the framework head-contributor test.

- [ ] **Step 2: Frontend CI checks**

Run: `npm run check` and `npm run validate-pages`
Expected: pass.
Run the repo's local CI aggregate if present: `/ci` (or the documented `npm`/`dotnet` CI steps).

- [ ] **Step 3: Manual/e2e smoke (use the `verify-feature` or `qa` skill / playwright-cli)**

Start the host (`dotnet run --project template/SimpleModule.Host`, kill port 5001 first if needed), log in as an admin, then:
1. Navigate to `/branding`. Confirm the page renders with current values.
2. Change app name → Save → confirm sidebar + browser tab title update after reload.
3. Change primary color → Save → reload → confirm buttons/badge/progress reflect it with **no flash** of the old color.
4. Enable top bar with a message → Save → confirm the bar appears on the app and public pages; dismiss works.
5. Enable footer with text → Save → confirm footer renders.
6. Upload a logo → Save → confirm sidebar shows the image; upload a favicon → confirm tab icon updates (may need hard refresh).
7. Add custom CSS (e.g. `body{}` no-op) → Save → confirm no breakage and the `<style>` is present in `<head>`.
8. Log out → confirm the logo/app-name/top-bar render on the public/login page (anonymous asset serving works).

- [ ] **Step 4: Fix any findings, re-run Steps 1-3 until clean. Final commit if needed.**

```bash
git add -A
git commit -m "test(branding): verification fix-ups"
```

- [ ] **Step 5: Open the PR** (via `verify-feature`/`vf` skill or `gh`), targeting `main`, summarizing the module + the generic `IInertiaHeadContributor` framework addition. Do not add AI attribution.

---

## Self-Review (completed during planning)

**Spec coverage:**
- App name → Task 1 (key/default), 3 (service), 6 (BrandMark + title), 7 (form). ✓
- Logo + favicon (upload/serve/render) → Task 4 (upload/serve), 6 (BrandMark/favicon link via Task 5 head), 7 (form). ✓
- Brand colors (light/dark, no flash) → Task 5 (head contributor), 7 (pickers). ✓
- Custom CSS → Task 5 (head injection), 7 (textarea). ✓
- Top bar → Task 1 (config), 6 (TopBar), 7 (form). ✓
- Footer → Task 1 (config), 6 (Footer), 7 (form). ✓
- Layer on Settings (no DbContext) → Task 3 service uses `ISettingsContracts`; settings registered in module. ✓
- Global scope → all settings `SettingScope.Application`. ✓
- Permission + admin menu → Task 3. ✓
- Framework head hook → Task 2. ✓
- Shared prop → Task 5. ✓

**Type consistency:** `IBrandingContracts` (4 methods) used identically in service (Task 3), endpoints (Task 4), middleware/contributor (Task 5). `BrandingEditModel`/`BrandingDto` field names match across C# DTOs (Task 1), endpoint JSON, and the TS interfaces in `Manage.tsx`/`types.ts`. Asset URL format `/api/branding/assets/{kind}?v={fileId}` is identical in `BrandingService.AssetUrl`, upload endpoint, and serve route.

**Open verification items flagged inline (not blockers):** `FileStorageId.From(Guid)` factory + `ToString()` round-trip; `context.User.GetUserId()` namespace; exact `ISettingsContracts` member list for `FakeSettings`; integration-test auth seeding (Admin role vs seeded permission); module `ConfigureMiddleware` pipeline ordering. Each has a documented fallback.
```
