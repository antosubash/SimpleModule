# TailwindCSS Integration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace all inline CSS and Bootstrap-style classes with TailwindCSS utility classes using the standalone CLI.

**Architecture:** Download Tailwind standalone CLI to `tools/`, configure it to scan `.razor` files, generate compiled CSS to `wwwroot/css/app.css`, and integrate into MSBuild. Then migrate all 35+ Razor components from inline styles/Bootstrap classes to Tailwind utilities.

**Tech Stack:** TailwindCSS v4 standalone CLI, MSBuild targets, Blazor SSR components

---

### Task 1: Tailwind CLI Download Script

**Files:**
- Create: `tools/download-tailwind.sh`
- Create: `tools/download-tailwind.ps1`
- Modify: `.gitignore`

**Step 1: Add tools/ binaries to .gitignore**

Append to `.gitignore`:
```
tools/tailwindcss*
```

**Step 2: Create PowerShell download script**

Create `tools/download-tailwind.ps1`:
```powershell
$version = "v4.1.3"
$os = if ($IsLinux) { "linux" } elseif ($IsMacOS) { "macos" } else { "windows" }
$arch = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq "Arm64") { "arm64" } else { "x64" }
$ext = if ($os -eq "windows") { ".exe" } else { "" }
$filename = "tailwindcss-$os-$arch$ext"
$url = "https://github.com/tailwindlabs/tailwindcss/releases/download/$version/$filename"
$outPath = Join-Path $PSScriptRoot "tailwindcss$ext"

if (Test-Path $outPath) {
    Write-Host "Tailwind CLI already exists at $outPath"
    exit 0
}

Write-Host "Downloading Tailwind CSS $version..."
Invoke-WebRequest -Uri $url -OutFile $outPath
if ($os -ne "windows") { chmod +x $outPath }
Write-Host "Downloaded to $outPath"
```

**Step 3: Create bash download script**

Create `tools/download-tailwind.sh`:
```bash
#!/usr/bin/env bash
set -euo pipefail
VERSION="v4.1.3"
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
case "$OS" in darwin) OS="macos" ;; esac
ARCH=$(uname -m)
case "$ARCH" in x86_64) ARCH="x64" ;; aarch64|arm64) ARCH="arm64" ;; esac
FILENAME="tailwindcss-${OS}-${ARCH}"
URL="https://github.com/tailwindlabs/tailwindcss/releases/download/${VERSION}/${FILENAME}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT="${SCRIPT_DIR}/tailwindcss"

if [ -f "$OUT" ]; then echo "Tailwind CLI already exists"; exit 0; fi

echo "Downloading Tailwind CSS ${VERSION}..."
curl -sL "$URL" -o "$OUT"
chmod +x "$OUT"
echo "Downloaded to $OUT"
```

**Step 4: Commit**

```bash
git add tools/ .gitignore
git commit -m "feat: add Tailwind CSS standalone CLI download scripts"
```

---

### Task 2: Tailwind Configuration and Input CSS

**Files:**
- Create: `src/SimpleModule.Api/Styles/app.css`
- Create: `src/SimpleModule.Api/wwwroot/css/.gitkeep`

**Step 1: Create Tailwind input CSS**

Create `src/SimpleModule.Api/Styles/app.css`:
```css
@import "tailwindcss";

@theme {
  --color-primary: #0d6efd;
  --color-primary-hover: #0b5ed7;
  --color-danger: #dc3545;
  --color-danger-hover: #bb2d3b;
  --color-success: #0f5132;
  --color-success-bg: #d1e7dd;
  --color-danger-bg: #f8d7da;
  --color-danger-text: #842029;
  --color-warning-bg: #fff3cd;
  --color-warning-border: #ffecb5;
  --color-dark: #212529;
  --color-code-bg: #1e1e1e;
  --color-code-text: #d4d4d4;
  --color-muted: #666;
  --color-border: #e9ecef;
}

/* Blazor validation integration */
.validation-message {
  @apply text-danger text-xs mt-1;
}
```

**Step 2: Create wwwroot output directory**

```bash
mkdir -p src/SimpleModule.Api/wwwroot/css
touch src/SimpleModule.Api/wwwroot/css/.gitkeep
```

**Step 3: Commit**

```bash
git add src/SimpleModule.Api/Styles/ src/SimpleModule.Api/wwwroot/
git commit -m "feat: add Tailwind input CSS with custom theme"
```

---

### Task 3: MSBuild Integration

**Files:**
- Modify: `src/SimpleModule.Api/SimpleModule.Api.csproj`

**Step 1: Add Tailwind build target to csproj**

Add before closing `</Project>` tag:
```xml
  <!-- Tailwind CSS -->
  <PropertyGroup>
    <TailwindCli Condition="'$(OS)' == 'Windows_NT'">$(MSBuildThisFileDirectory)..\..\tools\tailwindcss.exe</TailwindCli>
    <TailwindCli Condition="'$(OS)' != 'Windows_NT'">$(MSBuildThisFileDirectory)../../tools/tailwindcss</TailwindCli>
    <TailwindInput>$(MSBuildThisFileDirectory)Styles\app.css</TailwindInput>
    <TailwindOutput>$(MSBuildThisFileDirectory)wwwroot\css\app.css</TailwindOutput>
  </PropertyGroup>
  <Target Name="TailwindBuild" BeforeTargets="Build" Condition="Exists('$(TailwindCli)')">
    <Exec Command="&quot;$(TailwindCli)&quot; -i &quot;$(TailwindInput)&quot; -o &quot;$(TailwindOutput)&quot; --minify" />
  </Target>
```

**Step 2: Commit**

```bash
git add src/SimpleModule.Api/SimpleModule.Api.csproj
git commit -m "feat: add MSBuild target for Tailwind CSS compilation"
```

---

### Task 4: Download CLI and Verify Build

**Step 1: Download the Tailwind CLI**

```bash
cd tools && pwsh -File download-tailwind.ps1
```

**Step 2: Run the build**

```bash
dotnet build src/SimpleModule.Api
```

Expected: Build succeeds, `src/SimpleModule.Api/wwwroot/css/app.css` is generated.

**Step 3: Commit generated output**

```bash
git add src/SimpleModule.Api/wwwroot/css/app.css
git commit -m "chore: verify Tailwind build produces output"
```

---

### Task 5: Link Tailwind CSS in App.razor

**Files:**
- Modify: `src/SimpleModule.Api/Components/App.razor`

**Step 1: Add stylesheet link**

Add `<link rel="stylesheet" href="/css/app.css" />` inside `<head>` after the viewport meta tag.

**Step 2: Commit**

```bash
git add src/SimpleModule.Api/Components/App.razor
git commit -m "feat: link Tailwind CSS stylesheet in App.razor"
```

---

### Task 6: Migrate MainLayout.razor

**Files:**
- Modify: `src/SimpleModule.Api/Components/Layout/MainLayout.razor`

**Step 1: Replace entire file**

Remove the `<HeadContent><style>...</style></HeadContent>` block entirely. Replace the HTML with Tailwind utility classes:

```razor
@inherits LayoutComponentBase
@inject NavigationManager Navigation

<nav class="flex items-center bg-dark px-6 py-3">
    <div class="flex gap-4">
        <a href="/" class="text-white no-underline">SimpleModule</a>
        <a href="/swagger" class="text-white no-underline">API Docs</a>
    </div>
    <div class="ml-auto">
        @if (HttpContext?.User?.Identity?.IsAuthenticated == true)
        {
            <a href="/Identity/Account/Manage" class="text-white/80 no-underline text-sm">@HttpContext.User.Identity.Name</a>
        }
        else
        {
            <a href="/Identity/Account/Login" class="text-white/80 no-underline text-sm">Login</a>
        }
    </div>
</nav>
<div class="max-w-md mx-auto mt-15 p-8 bg-white rounded-lg shadow-md">
    @Body
</div>

@code {
    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }
}
```

The base styles (box-sizing, body, form inputs, etc.) are handled by Tailwind's preflight and the input CSS theme.

**Step 2: Rebuild and verify**

```bash
dotnet build src/SimpleModule.Api
```

**Step 3: Commit**

```bash
git add src/SimpleModule.Api/Components/Layout/MainLayout.razor
git commit -m "feat: migrate MainLayout to Tailwind CSS"
```

---

### Task 7: Migrate Home.razor

**Files:**
- Modify: `src/SimpleModule.Api/Components/Pages/Home.razor`

**Step 1: Remove the entire `<HeadContent><style>...</style></HeadContent>` block**

**Step 2: Replace all HTML markup with Tailwind classes**

Key mappings:
- `.hero` → `text-center pt-12 pb-8`
- `.hero h1` → `text-4xl font-bold mb-2`
- `.tagline` → `text-muted text-base mb-7`
- `.welcome` → `text-lg mb-5` with `<strong class="text-primary">`
- `.btn-group` → `flex gap-3 justify-center flex-wrap`
- `.btn` base → `inline-block px-6 py-2.5 rounded text-sm font-medium cursor-pointer no-underline border-none`
- `.btn-primary` → `bg-primary text-white hover:bg-primary-hover`
- `.btn-outline` → `bg-transparent text-primary border border-primary hover:bg-primary hover:text-white`
- `.btn-danger` → `bg-danger text-white hover:bg-danger-hover`
- `.btn-sm` → `px-3.5 py-1.5 text-xs`
- `.quick-links` → `flex gap-4 justify-center mt-6 text-sm`
- `.divider` → `border-t border-gray-200 my-8` (use `<hr>`)
- `.hint-box` → `bg-warning-bg border border-warning-border rounded-md p-4 mt-6 text-xs text-left`
- `.panel` → `mb-6`
- `.panel h2` → `text-lg font-semibold mb-4 pb-2 border-b-2 border-primary`
- `.card` → `bg-gray-50 border border-border rounded-md p-4 mb-4`
- `.card h3` → `text-sm font-semibold mb-3`
- `.info-row` → `flex justify-between py-1.5 text-sm border-b border-border last:border-b-0`
- `.info-label` → `text-muted`
- `.info-value` → `font-medium`
- `.token-box` → `bg-code-bg text-code-text rounded p-3 font-mono text-xs break-all max-h-30 overflow-y-auto mt-2`
- `.claims-table` → `w-full text-xs border-collapse mt-2`
- `.response-box` → `bg-code-bg text-code-text rounded p-3 font-mono text-xs whitespace-pre-wrap max-h-50 overflow-y-auto mt-2`
- `.api-btn-row` → `flex gap-2 flex-wrap mb-2`
- `.status-badge` → `inline-block px-2 py-0.5 rounded text-xs font-semibold`
- `.status-ok` → `bg-success-bg text-success`
- `.status-err` → `bg-danger-bg text-danger-text`
- `.spinner` → keep as `@keyframes` in a minimal `<style>` or use Tailwind `animate-spin` on a border element

For the spinner, replace the custom CSS spinner with a Tailwind-based one:
```html
<span class="inline-block w-3.5 h-3.5 border-2 border-gray-300 border-t-primary rounded-full animate-spin align-middle ml-1.5"></span>
```

The JavaScript section stays unchanged (it's functionality, not styling). But update the `className` assignments in JS to use the new Tailwind classes.

**Step 3: Rebuild and verify**

**Step 4: Commit**

```bash
git add src/SimpleModule.Api/Components/Pages/Home.razor
git commit -m "feat: migrate Home page to Tailwind CSS"
```

---

### Task 8: Migrate OAuthCallback.razor

**Files:**
- Modify: `src/SimpleModule.Api/Components/Pages/OAuthCallback.razor`

**Step 1: Replace inline style with Tailwind classes**

```razor
@page "/oauth-callback"
<PageTitle>Authenticating... - SimpleModule</PageTitle>
<p class="text-center text-muted">Completing authentication...</p>
<script suppress-error="BL9992">
    window.location.href = '/' + window.location.search;
</script>
```

**Step 2: Commit**

```bash
git add src/SimpleModule.Api/Components/Pages/OAuthCallback.razor
git commit -m "feat: migrate OAuthCallback to Tailwind CSS"
```

---

### Task 9: Migrate Users Module Shared Components

**Files:**
- Modify: `src/modules/Users/Users/Components/Layout/ManageLayout.razor`
- Modify: `src/modules/Users/Users/Components/Shared/ManageNav.razor`
- Modify: `src/modules/Users/Users/Components/Shared/StatusMessage.razor`

**Step 1: Migrate ManageLayout.razor**

Replace inline flex styles:
```razor
@inherits LayoutComponentBase
@inject NavigationManager Navigation

<h1 class="text-2xl font-bold mb-2">Manage your account</h1>
<div>
    <h2 class="text-lg font-semibold mb-4">Change your account settings</h2>
    <hr class="mb-6" />
    <div class="flex gap-6">
        <div class="min-w-[180px]">
            <ManageNav ActivePage="@GetActivePage()" />
        </div>
        <div class="flex-1">
            @Body
        </div>
    </div>
</div>

@code {
    private string GetActivePage()
    {
        var uri = new Uri(Navigation.Uri);
        var path = uri.AbsolutePath.TrimEnd('/');
        var lastSegment = path.Split('/').LastOrDefault() ?? "";
        return lastSegment == "Manage" ? "Index" : lastSegment;
    }
}
```

**Step 2: Migrate ManageNav.razor**

Replace inline style method with CSS class toggling:
```razor
@inject SignInManager<ApplicationUser> SignInManager

<ul class="list-none p-0 mb-6">
    <li><a href="/Identity/Account/Manage" class="@NavClass("Index")">Profile</a></li>
    <li><a href="/Identity/Account/Manage/Email" class="@NavClass("Email")">Email</a></li>
    <li><a href="/Identity/Account/Manage/ChangePassword" class="@NavClass("ChangePassword")">Password</a></li>
    @if (_hasExternalLogins)
    {
        <li><a href="/Identity/Account/Manage/ExternalLogins" class="@NavClass("ExternalLogins")">External logins</a></li>
    }
    <li><a href="/Identity/Account/Manage/TwoFactorAuthentication" class="@NavClass("TwoFactorAuthentication")">Two-factor authentication</a></li>
    <li><a href="/Identity/Account/Manage/PersonalData" class="@NavClass("PersonalData")">Personal data</a></li>
</ul>

@code {
    [Parameter]
    public string? ActivePage { get; set; }

    private bool _hasExternalLogins;

    protected override async Task OnInitializedAsync()
    {
        _hasExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).Any();
    }

    private string NavClass(string page)
    {
        var isActive = string.Equals(ActivePage, page, StringComparison.OrdinalIgnoreCase);
        return isActive
            ? "block px-3 py-2 text-primary font-semibold no-underline bg-blue-50 rounded mb-0.5"
            : "block px-3 py-2 text-gray-700 no-underline mb-0.5 hover:bg-gray-100 rounded";
    }
}
```

**Step 3: Migrate StatusMessage.razor**

```razor
@if (!string.IsNullOrEmpty(Message))
{
    var isError = Message.StartsWith("Error");
    <div class="@(isError ? "bg-danger-bg text-danger-text" : "bg-success-bg text-success") p-4 rounded-md mb-4" role="alert">
        @Message
    </div>
}

@code {
    [Parameter]
    public string? Message { get; set; }
}
```

**Step 4: Commit**

```bash
git add src/modules/Users/Users/Components/
git commit -m "feat: migrate Users module shared components to Tailwind CSS"
```

---

### Task 10: Migrate Account Auth Pages (Login, Register, ForgotPassword, ResetPassword, ResendEmailConfirmation)

**Files:**
- Modify: `src/modules/Users/Users/Components/Pages/Account/Login.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Register.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/ForgotPassword.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/ResetPassword.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/ResendEmailConfirmation.razor`

**Step 1: Apply consistent class replacements across all files**

Standard mappings for all form pages:
- `class="text-danger"` → `class="text-danger text-sm"`
- `class="form-floating mb-3"` → `class="mb-4"`
- `class="form-control"` → `class="w-full px-3 py-2.5 border border-gray-300 rounded text-sm focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary/15"`
- `class="form-label"` → `class="block mb-1 font-medium text-sm"`
- `class="w-100 btn btn-lg btn-primary"` → `class="w-full py-2.5 px-6 bg-primary text-white rounded text-sm font-medium cursor-pointer hover:bg-primary-hover"`
- `class="btn btn-primary"` → `class="px-6 py-2.5 bg-primary text-white rounded text-sm font-medium cursor-pointer hover:bg-primary-hover"`
- `class="form-check-input"` → `class="mr-2"`
- `style="list-style:none;padding:0;"` → `class="list-none p-0"`
- `class="checkbox mb-3"` → `class="mb-4"`

**Step 2: Rebuild and verify**

**Step 3: Commit**

```bash
git add src/modules/Users/Users/Components/Pages/Account/Login.razor
git add src/modules/Users/Users/Components/Pages/Account/Register.razor
git add src/modules/Users/Users/Components/Pages/Account/ForgotPassword.razor
git add src/modules/Users/Users/Components/Pages/Account/ResetPassword.razor
git add src/modules/Users/Users/Components/Pages/Account/ResendEmailConfirmation.razor
git commit -m "feat: migrate account auth pages to Tailwind CSS"
```

---

### Task 11: Migrate Account 2FA Pages (LoginWith2fa, LoginWithRecoveryCode, ExternalLogin)

**Files:**
- Modify: `src/modules/Users/Users/Components/Pages/Account/LoginWith2fa.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/LoginWithRecoveryCode.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/ExternalLogin.razor`

**Step 1: Apply the same form class mappings from Task 10**

**Step 2: Commit**

```bash
git add src/modules/Users/Users/Components/Pages/Account/LoginWith2fa.razor
git add src/modules/Users/Users/Components/Pages/Account/LoginWithRecoveryCode.razor
git add src/modules/Users/Users/Components/Pages/Account/ExternalLogin.razor
git commit -m "feat: migrate 2FA and external login pages to Tailwind CSS"
```

---

### Task 12: Migrate Account Static Pages

**Files:**
- Modify: `src/modules/Users/Users/Components/Pages/Account/AccessDenied.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Lockout.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/ConfirmEmail.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/ConfirmEmailChange.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/RegisterConfirmation.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/ResetPasswordConfirmation.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/ForgotPasswordConfirmation.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Error.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Logout.razor`

**Step 1: Apply class mappings — these pages are mostly static text with minimal styling**

- `class="text-danger"` → `class="text-danger text-sm"`
- `class="text-info"` → `class="text-blue-500 text-sm"`
- Any `alert` classes → Tailwind equivalents from Task 9

**Step 2: Commit**

```bash
git add src/modules/Users/Users/Components/Pages/Account/
git commit -m "feat: migrate account static pages to Tailwind CSS"
```

---

### Task 13: Migrate Manage Pages (Index, Email, ChangePassword, SetPassword)

**Files:**
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/Index.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/Email.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/ChangePassword.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/SetPassword.razor`

**Step 1: Apply form class mappings from Task 10**

Additional for Email.razor:
- `style="display:flex;..."` → `class="flex gap-2 items-center"`

**Step 2: Commit**

```bash
git add src/modules/Users/Users/Components/Pages/Account/Manage/Index.razor
git add src/modules/Users/Users/Components/Pages/Account/Manage/Email.razor
git add src/modules/Users/Users/Components/Pages/Account/Manage/ChangePassword.razor
git add src/modules/Users/Users/Components/Pages/Account/Manage/SetPassword.razor
git commit -m "feat: migrate manage profile pages to Tailwind CSS"
```

---

### Task 14: Migrate Manage Security Pages

**Files:**
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/TwoFactorAuthentication.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/EnableAuthenticator.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/ResetAuthenticator.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/Disable2fa.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/GenerateRecoveryCodes.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/ShowRecoveryCodes.razor`

**Step 1: Apply class mappings**

Additional mappings for these pages:
- `class="alert alert-warning"` → `class="bg-warning-bg border border-warning-border p-4 rounded-md mb-4"`
- `class="alert alert-danger"` → `class="bg-danger-bg text-danger-text p-4 rounded-md mb-4"`
- `class="btn btn-danger"` → `class="px-6 py-2.5 bg-danger text-white rounded text-sm font-medium cursor-pointer hover:bg-danger-hover"`
- `class="btn-link"` → `class="text-primary underline cursor-pointer bg-transparent border-none"`

**Step 2: Commit**

```bash
git add src/modules/Users/Users/Components/Pages/Account/Manage/
git commit -m "feat: migrate manage security pages to Tailwind CSS"
```

---

### Task 15: Migrate Manage Data Pages

**Files:**
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/ExternalLogins.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/PersonalData.razor`
- Modify: `src/modules/Users/Users/Components/Pages/Account/Manage/DeletePersonalData.razor`

**Step 1: Apply class mappings**

Additional for ExternalLogins:
- `class="table"` → `class="w-full text-sm border-collapse"`

**Step 2: Commit**

```bash
git add src/modules/Users/Users/Components/Pages/Account/Manage/
git commit -m "feat: migrate manage data pages to Tailwind CSS"
```

---

### Task 16: Final Build Verification

**Step 1: Rebuild entire solution**

```bash
dotnet build
```

Expected: Clean build, no errors.

**Step 2: Run the app and verify visually**

```bash
dotnet run --project src/SimpleModule.Api
```

Check: Home page, Login, Register, Manage pages all render correctly with Tailwind.

**Step 3: Final commit if any fixes needed**
