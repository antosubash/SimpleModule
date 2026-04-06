# Passkey Login Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add optional WebAuthn passkey authentication to the Users module so users can register, use, and manage passkeys alongside existing password login.

**Architecture:** Adds a hand-written `partial class HostDbContext` to override `SchemaVersion` to `IdentitySchemaVersions.Version3` (auto-provisions `AspNetUserPasskeys` table via EF Core migration against `HostDbContext`, which is the generated aggregate context all migrations target), adds 6 JSON API endpoints + 1 view endpoint using the built-in .NET 10 `SignInManager`/`UserManager` passkey APIs, and updates the React login page and account management UI with TypeScript WebAuthn browser API helpers.

**Tech Stack:** .NET 10 ASP.NET Core Identity passkey APIs (no third-party library), EF Core migrations, xUnit.v3 + FluentAssertions + NSubstitute, React 19 + Inertia.js, TypeScript WebAuthn browser API (`navigator.credentials`)

> **API reference:** Verify exact method signatures for `MakePasskeyCreationOptionsAsync`, `PerformPasskeyAttestationAsync`, `MakePasskeyRequestOptionsAsync`, `PasskeySignInAsync`, `AddOrUpdatePasskeyAsync`, `GetPasskeysAsync`, and `RemovePasskeyAsync` at:
> https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0

---

## File Map

### New files
| File | Purpose |
|---|---|
| `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyRegisterBeginEndpoint.cs` | `POST /api/passkeys/register/begin` — returns WebAuthn creation options |
| `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyRegisterCompleteEndpoint.cs` | `POST /api/passkeys/register/complete` — validates and stores passkey |
| `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyLoginBeginEndpoint.cs` | `POST /api/passkeys/login/begin` — returns WebAuthn request options (anonymous) |
| `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyLoginCompleteEndpoint.cs` | `POST /api/passkeys/login/complete` — authenticates via passkey (anonymous) |
| `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/GetPasskeysEndpoint.cs` | `GET /api/passkeys` — list user's registered passkeys |
| `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/DeletePasskeyEndpoint.cs` | `DELETE /api/passkeys/{credentialId}` — remove a passkey |
| `modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManagePasskeysEndpoint.cs` | `GET /Manage/Passkeys` — Inertia view endpoint |
| `modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManagePasskeys.tsx` | React page for listing/adding/deleting passkeys |
| `modules/Users/src/SimpleModule.Users/Pages/passkey.ts` | WebAuthn browser API helpers (base64url, credentials.create/get) |
| `template/SimpleModule.Host/HostDbContextPasskeys.cs` | Hand-written `partial class HostDbContext` that overrides `SchemaVersion` |
| EF Core migration | Adds `AspNetUserPasskeys` table via Schema Version 3 (targets `HostDbContext`) |
| `modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs` | Integration tests for the 6 API endpoints |
| `modules/Users/tests/SimpleModule.Users.Tests/Integration/ManagePasskeysEndpointTests.cs` | Integration test for the view endpoint |

### Modified files
| File | Change |
|---|---|
| `template/SimpleModule.Host/HostDbContextPasskeys.cs` | **New** — hand-written partial class overriding `SchemaVersion` |
| `modules/Users/src/SimpleModule.Users/UsersModule.cs` | Add `services.Configure<IdentityPasskeyOptions>(...)` |
| `modules/Users/src/SimpleModule.Users/Pages/Account/LoginEndpoint.cs` | Pass `passkeyEnabled` prop from config |
| `modules/Users/src/SimpleModule.Users/Pages/Account/Login.tsx` | Add "Sign in with passkey" button |
| `modules/Users/src/SimpleModule.Users/Pages/index.ts` | Register `"Users/Account/Manage/Passkeys"` page |
| `modules/Users/src/SimpleModule.Users/components/ManageLayout.tsx` | Add Passkeys nav item |
| `template/SimpleModule.Host/appsettings.json` | Add `"Passkeys": { "ServerDomain": "yourdomain.com" }` |
| `template/SimpleModule.Host/appsettings.Development.json` | Add `"Passkeys": { "ServerDomain": "localhost" }` |

---

## Chunk 1: Infrastructure Setup

### Task 1: Add HostDbContext partial, configure passkey options, and run migration

> **Architecture note:** This project uses a Roslyn source generator that synthesizes `HostDbContext` as a `partial class` combining all module DbContexts. All EF Core migrations run against `HostDbContext` (in `template/SimpleModule.Host/Migrations/`), NOT against `UsersDbContext`. To opt into Identity Schema Version 3, we add a hand-written `partial class HostDbContext` file that overrides `SchemaVersion`.

**Files:**
- Create: `template/SimpleModule.Host/HostDbContextPasskeys.cs`
- Modify: `modules/Users/src/SimpleModule.Users/UsersModule.cs`
- Modify: `template/SimpleModule.Host/appsettings.json`
- Modify: `template/SimpleModule.Host/appsettings.Development.json`
- Create: EF Core migration (auto-generated in `template/SimpleModule.Host/Migrations/`)

- [ ] **Step 1: Create hand-written HostDbContext partial to override SchemaVersion**

Create `template/SimpleModule.Host/HostDbContextPasskeys.cs`:

```csharp
using Microsoft.AspNetCore.Identity;

namespace SimpleModule.Host;

// Extends the source-generated HostDbContext to opt into Identity Schema Version 3.
// This adds the AspNetUserPasskeys table for WebAuthn passkey support.
public partial class HostDbContext
{
    protected override Version SchemaVersion => IdentitySchemaVersions.Version3;
}
```

> **Why a separate file:** `HostDbContext` is generated by the Roslyn source generator and declared as `partial`. This hand-written partial adds only the `SchemaVersion` override. The generated partial handles everything else (user types, module DbSets, schema mapping).

- [ ] **Step 2: Configure IdentityPasskeyOptions in UsersModule.cs**

In `modules/Users/src/SimpleModule.Users/UsersModule.cs`, add this line immediately after the `.AddDefaultTokenProviders()` chain in `ConfigureServices`:

```csharp
services.Configure<IdentityPasskeyOptions>(configuration.GetSection("Passkeys"));
```

The full `ConfigureServices` method should look like:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddModuleDbContext<UsersDbContext>(configuration, UsersConstants.ModuleName);

    services
        .AddIdentity<ApplicationUser, ApplicationRole>()
        .AddEntityFrameworkStores<UsersDbContext>()
        .AddDefaultTokenProviders();

    services.Configure<IdentityPasskeyOptions>(configuration.GetSection("Passkeys"));

    services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Identity/Account/Login";
        options.LogoutPath = "/Identity/Account/Logout";
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    });

    services.AddSingleton<IPostConfigureOptions<IdentityOptions>, ApplyUsersModuleOptions>();
    services.AddHostedService<UserSeedService>();
    services.AddSingleton<IEmailSender<ApplicationUser>, ConsoleEmailSender>();
}
```

Add using at the top of `UsersModule.cs` if not already present: `using Microsoft.AspNetCore.Identity;`

- [ ] **Step 3: Add Passkeys config to appsettings.json**

In `template/SimpleModule.Host/appsettings.json`, add a `"Passkeys"` section (replace `yourdomain.com` with your actual domain in production):

```json
"Passkeys": {
  "ServerDomain": "yourdomain.com"
}
```

- [ ] **Step 4: Add Passkeys config to appsettings.Development.json**

In `template/SimpleModule.Host/appsettings.Development.json`, add:

```json
"Passkeys": {
  "ServerDomain": "localhost"
}
```

- [ ] **Step 5: Build to verify the partial compiles**

```bash
dotnet build
```

Expected: 0 errors. Fix any namespace mismatches (check the generated `HostDbContext.g.cs` namespace if needed — look in `template/SimpleModule.Host/obj/Debug/net10.0/generated/`).

- [ ] **Step 6: Generate the EF Core migration**

Run from the solution root (all migrations target `HostDbContext` in the Host project):

```bash
dotnet ef migrations add AddPasskeySupport --project template/SimpleModule.Host --startup-project template/SimpleModule.Host
```

Inspect the generated migration file in `template/SimpleModule.Host/Migrations/` and confirm it creates an `AspNetUserPasskeys` table (or prefixed equivalent for the Users schema, e.g., `Users_AspNetUserPasskeys`).

- [ ] **Step 7: Build again after migration**

```bash
dotnet build
```

Expected: 0 errors.

- [ ] **Step 8: Commit**

```bash
git add template/SimpleModule.Host/HostDbContextPasskeys.cs
git add modules/Users/src/SimpleModule.Users/UsersModule.cs
git add template/SimpleModule.Host/appsettings.json
git add template/SimpleModule.Host/appsettings.Development.json
git add -A -- "template/SimpleModule.Host/Migrations/*AddPasskeySupport*"
git commit -m "feat: add Identity Schema V3 partial and IdentityPasskeyOptions for passkey support"
```

---

## Chunk 2: Registration API Endpoints

### Task 2: PasskeyRegisterBeginEndpoint

**Files:**
- Create: `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyRegisterBeginEndpoint.cs`
- Create (or add to): `modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs`

- [ ] **Step 1: Write the failing integration test**

Create `modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs`:

```csharp
using System.Net;
using System.Security.Claims;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;

namespace Users.Tests.Integration;

[Collection(TestCollections.Integration)]
public class PasskeyApiEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;
    private readonly HttpClient _unauthenticated;

    public PasskeyApiEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
        _unauthenticated = factory.CreateClient();
    }

    // ── Register Begin ──────────────────────────────────────────────

    [Fact]
    public async Task RegisterBegin_WhenAuthenticated_Returns200WithJson()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/passkeys/register/begin", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterBegin_WhenUnauthenticated_Returns401()
    {
        var response = await _unauthenticated.PostAsync("/api/passkeys/register/begin", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Run the test to confirm it fails**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~RegisterBegin" -v
```

Expected: FAIL (endpoint does not exist).

- [ ] **Step 3: Create the endpoint**

Create `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyRegisterBeginEndpoint.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class PasskeyRegisterBeginEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/passkeys/register/begin",
                async (
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return Results.Unauthorized();

                    // MakePasskeyCreationOptionsAsync takes a PasskeyUserEntity (NOT ApplicationUser directly).
                    // It stores the challenge in an encrypted auth cookie and returns a JSON string
                    // of PublicKeyCredentialCreationOptions for the browser.
                    // Verify exact PasskeyUserEntity properties at:
                    // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys
                    var userEntity = new PasskeyUserEntity
                    {
                        Id = await userManager.GetUserIdAsync(user),
                        Name = await userManager.GetUserNameAsync(user) ?? user.Email ?? user.Id,
                        DisplayName = user.DisplayName.Length > 0
                            ? user.DisplayName
                            : (await userManager.GetUserNameAsync(user) ?? user.Email ?? user.Id),
                    };

                    var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(userEntity);
                    return Results.Content(optionsJson, "application/json");
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags("Passkeys");
    }
}
```

- [ ] **Step 4: Run the test to confirm it passes**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~RegisterBegin" -v
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyRegisterBeginEndpoint.cs
git add modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs
git commit -m "feat: add PasskeyRegisterBeginEndpoint"
```

---

### Task 3: PasskeyRegisterCompleteEndpoint

**Files:**
- Create: `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyRegisterCompleteEndpoint.cs`
- Modify: `modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs`

- [ ] **Step 1: Add the failing tests to PasskeyApiEndpointTests.cs**

Append to `PasskeyApiEndpointTests.cs`:

```csharp
    // ── Register Complete ─────────────────────────────────────────────

    [Fact]
    public async Task RegisterComplete_WhenUnauthenticated_Returns401()
    {
        var content = new StringContent(
            """{"id":"test"}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _unauthenticated.PostAsync(
            "/api/passkeys/register/complete",
            content
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterComplete_WithInvalidCredential_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        var content = new StringContent(
            """{"invalid":"data"}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync("/api/passkeys/register/complete", content);

        // Invalid attestation should be rejected
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity
        );
    }
```

- [ ] **Step 2: Run the tests to confirm they fail**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~RegisterComplete" -v
```

Expected: FAIL

- [ ] **Step 3: Create the endpoint**

Create `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyRegisterCompleteEndpoint.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class PasskeyRegisterCompleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/passkeys/register/complete",
                async (
                    HttpRequest request,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    SignInManager<ApplicationUser> signInManager
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return Results.Unauthorized();

                    var credentialJson = await new StreamReader(request.Body).ReadToEndAsync();
                    if (string.IsNullOrWhiteSpace(credentialJson))
                        return Results.BadRequest("Credential JSON is required.");

                    // PerformPasskeyAttestationAsync validates the WebAuthn attestation.
                    // Returns a result with .Succeeded and .Passkey (the passkey to store).
                    // Verify exact return type at:
                    // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys
                    var result = await signInManager.PerformPasskeyAttestationAsync(credentialJson);
                    if (!result.Succeeded)
                        return Results.BadRequest("Passkey registration failed.");

                    await userManager.AddOrUpdatePasskeyAsync(user, result.Passkey);
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags("Passkeys");
    }
}
```

- [ ] **Step 4: Run the tests**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~RegisterComplete" -v
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyRegisterCompleteEndpoint.cs
git add modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs
git commit -m "feat: add PasskeyRegisterCompleteEndpoint"
```

---

## Chunk 3: Auth + Management API Endpoints

### Task 4: PasskeyLoginBeginEndpoint

**Files:**
- Create: `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyLoginBeginEndpoint.cs`
- Modify: `modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs`

- [ ] **Step 1: Add the failing test**

Append to `PasskeyApiEndpointTests.cs`:

```csharp
    // ── Login Begin ───────────────────────────────────────────────────

    [Fact]
    public async Task LoginBegin_WhenAnonymous_Returns200WithJson()
    {
        var response = await _unauthenticated.PostAsync("/api/passkeys/login/begin", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
    }
```

- [ ] **Step 2: Run the test to confirm it fails**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~LoginBegin" -v
```

- [ ] **Step 3: Create the endpoint**

Create `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyLoginBeginEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class PasskeyLoginBeginEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/passkeys/login/begin",
                async (SignInManager<ApplicationUser> signInManager) =>
                {
                    // MakePasskeyRequestOptionsAsync stores challenge in encrypted auth cookie.
                    // Pass null for user to allow any registered passkey (discoverable credentials).
                    var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(null);
                    return Results.Content(optionsJson, "application/json");
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithTags("Passkeys");
    }
}
```

- [ ] **Step 4: Run the test**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~LoginBegin" -v
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyLoginBeginEndpoint.cs
git add modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs
git commit -m "feat: add PasskeyLoginBeginEndpoint"
```

---

### Task 5: PasskeyLoginCompleteEndpoint

**Files:**
- Create: `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyLoginCompleteEndpoint.cs`
- Modify: `modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs`

- [ ] **Step 1: Add the failing tests**

Append to `PasskeyApiEndpointTests.cs`:

```csharp
    // ── Login Complete ────────────────────────────────────────────────

    [Fact]
    public async Task LoginComplete_WithInvalidCredential_ReturnsUnauthorized()
    {
        var content = new StringContent(
            """{"id":"invalid","type":"public-key"}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _unauthenticated.PostAsync("/api/passkeys/login/complete", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginComplete_WithEmptyBody_ReturnsBadRequest()
    {
        var response = await _unauthenticated.PostAsync("/api/passkeys/login/complete", null);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized
        );
    }
```

- [ ] **Step 2: Run the tests to confirm they fail**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~LoginComplete" -v
```

- [ ] **Step 3: Create the endpoint**

Create `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyLoginCompleteEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class PasskeyLoginCompleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/passkeys/login/complete",
                async (
                    HttpRequest request,
                    SignInManager<ApplicationUser> signInManager,
                    [FromQuery] string? returnUrl = null
                ) =>
                {
                    var credentialJson = await new StreamReader(request.Body).ReadToEndAsync();
                    if (string.IsNullOrWhiteSpace(credentialJson))
                        return Results.BadRequest("Credential JSON is required.");

                    // PasskeySignInAsync validates the assertion, signs in, and sets the auth cookie.
                    // Verify exact signature at:
                    // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys
                    var result = await signInManager.PasskeySignInAsync(
                        credentialJson,
                        isPersistent: false,
                        lockoutOnFailure: true
                    );

                    if (result.Succeeded)
                    {
                        var redirectUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
                        return Results.Ok(new { redirectUrl });
                    }

                    if (result.IsLockedOut)
                        return Results.Problem("Account is locked out.", statusCode: 423);

                    return Results.Unauthorized();
                }
            )
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithTags("Passkeys");
    }
}
```

- [ ] **Step 4: Run the tests**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~LoginComplete" -v
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/PasskeyLoginCompleteEndpoint.cs
git add modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs
git commit -m "feat: add PasskeyLoginCompleteEndpoint"
```

---

### Task 6: GetPasskeysEndpoint

**Files:**
- Create: `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/GetPasskeysEndpoint.cs`
- Modify: `modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs`

- [ ] **Step 1: Add the failing tests**

Append to `PasskeyApiEndpointTests.cs`:

```csharp
    // ── Get Passkeys ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPasskeys_WhenUnauthenticated_Returns401()
    {
        var response = await _unauthenticated.GetAsync("/api/passkeys");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPasskeys_WhenAuthenticated_ReturnsOkWithList()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/passkeys");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNull();
        // New users have no passkeys — should return empty array
        body.Should().Be("[]");
    }
```

- [ ] **Step 2: Run the tests to confirm they fail**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~GetPasskeys" -v
```

- [ ] **Step 3: Create the endpoint**

Create `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/GetPasskeysEndpoint.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class GetPasskeysEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/passkeys",
                async (
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return Results.Unauthorized();

                    var passkeys = await userManager.GetPasskeysAsync(user);

                    // IdentityUserPasskey properties: CredentialId (byte[]), Name, CreatedAt.
                    // Verify exact property names at:
                    // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys
                    var result = passkeys.Select(p => new
                    {
                        credentialId = ToBase64Url(p.CredentialId),
                        name = p.Name,
                        createdAt = p.CreatedAt,
                    });

                    return Results.Ok(result);
                }
            )
            .RequireAuthorization()
            .WithTags("Passkeys");
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
```

- [ ] **Step 4: Run the tests**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~GetPasskeys" -v
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/GetPasskeysEndpoint.cs
git add modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs
git commit -m "feat: add GetPasskeysEndpoint"
```

---

### Task 7: DeletePasskeyEndpoint

**Files:**
- Create: `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/DeletePasskeyEndpoint.cs`
- Modify: `modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs`

- [ ] **Step 1: Add the failing tests**

Append to `PasskeyApiEndpointTests.cs`, then close the class with `}`:

```csharp
    // ── Delete Passkey ────────────────────────────────────────────────

    [Fact]
    public async Task DeletePasskey_WhenUnauthenticated_Returns401()
    {
        var response = await _unauthenticated.DeleteAsync("/api/passkeys/someCredentialId");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePasskey_WithNonExistentCredential_ReturnsNotFound()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/passkeys/nonexistent-credential-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 2: Run the tests to confirm they fail**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~DeletePasskey" -v
```

- [ ] **Step 3: Create the endpoint**

Create `modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/DeletePasskeyEndpoint.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Endpoints.Passkeys;

public class DeletePasskeyEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                "/api/passkeys/{credentialId}",
                async (
                    string credentialId,
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return Results.Unauthorized();

                    byte[] credentialIdBytes;
                    try
                    {
                        // Decode base64url (URL-safe base64 without padding)
                        var base64 = credentialId.Replace('-', '+').Replace('_', '/');
                        var padding = (4 - (base64.Length % 4)) % 4;
                        base64 = base64.PadRight(base64.Length + padding, '=');
                        credentialIdBytes = Convert.FromBase64String(base64);
                    }
                    catch
                    {
                        return Results.BadRequest("Invalid credential ID format.");
                    }

                    // Verify the passkey belongs to this user before deleting
                    var passkeys = await userManager.GetPasskeysAsync(user);
                    var exists = passkeys.Any(p => p.CredentialId.SequenceEqual(credentialIdBytes));
                    if (!exists)
                        return Results.NotFound();

                    await userManager.RemovePasskeyAsync(user, credentialIdBytes);
                    return Results.NoContent();
                }
            )
            .RequireAuthorization()
            .WithTags("Passkeys");
    }
}
```

- [ ] **Step 4: Run the tests**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~DeletePasskey" -v
```

Expected: PASS

- [ ] **Step 5: Run all passkey API tests together**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~PasskeyApiEndpointTests" -v
```

Expected: All PASS.

- [ ] **Step 6: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Endpoints/Passkeys/DeletePasskeyEndpoint.cs
git add modules/Users/tests/SimpleModule.Users.Tests/Integration/PasskeyApiEndpointTests.cs
git commit -m "feat: add DeletePasskeyEndpoint"
```

---

## Chunk 4: View Endpoint + Frontend

### Task 8: ManagePasskeysEndpoint (Inertia view)

**Files:**
- Create: `modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManagePasskeysEndpoint.cs`
- Create: `modules/Users/tests/SimpleModule.Users.Tests/Integration/ManagePasskeysEndpointTests.cs`

- [ ] **Step 1: Write the failing integration test**

Create `modules/Users/tests/SimpleModule.Users.Tests/Integration/ManagePasskeysEndpointTests.cs`:

```csharp
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleModule.Tests.Shared.Fixtures;

namespace Users.Tests.Integration;

[Collection(TestCollections.Integration)]
public class ManagePasskeysEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ManagePasskeysEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_WhenAuthenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/Identity/Account/Manage/Passkeys");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_WhenUnauthenticated_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/Identity/Account/Manage/Passkeys");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect,
            HttpStatusCode.Found,
            HttpStatusCode.Unauthorized
        );
    }
}
```

- [ ] **Step 2: Run the test to confirm it fails**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~ManagePasskeysEndpointTests" -v
```

- [ ] **Step 3: Create the view endpoint**

Create `modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManagePasskeysEndpoint.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Pages.Account.Manage;

public class ManagePasskeysEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/Manage/Passkeys",
                async (
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                        return TypedResults.Redirect("/Identity/Account/Login");

                    var passkeys = await userManager.GetPasskeysAsync(user);

                    var passkeysDto = passkeys.Select(p => new
                    {
                        credentialId = ToBase64Url(p.CredentialId),
                        name = p.Name,
                        createdAt = p.CreatedAt,
                    });

                    return Inertia.Render(
                        "Users/Account/Manage/Passkeys",
                        new { passkeys = passkeysDto }
                    );
                }
            )
            .RequireAuthorization();
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
```

- [ ] **Step 4: Run the tests**

```bash
dotnet test modules/Users/tests/SimpleModule.Users.Tests \
  --filter "FullyQualifiedName~ManagePasskeysEndpointTests" -v
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManagePasskeysEndpoint.cs
git add modules/Users/tests/SimpleModule.Users.Tests/Integration/ManagePasskeysEndpointTests.cs
git commit -m "feat: add ManagePasskeysEndpoint (Inertia view)"
```

---

### Task 9: passkey.ts — WebAuthn Browser API Utility

**Files:**
- Create: `modules/Users/src/SimpleModule.Users/Pages/passkey.ts`

- [ ] **Step 1: Create the utility**

Create `modules/Users/src/SimpleModule.Users/Pages/passkey.ts`:

```typescript
// WebAuthn uses base64url encoding for all binary data.
// These helpers convert between ArrayBuffer (required by browser API) and base64url strings.

function base64urlToArrayBuffer(base64url: string): ArrayBuffer {
  const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  const binary = atob(padded);
  const buffer = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    buffer[i] = binary.charCodeAt(i);
  }
  return buffer.buffer;
}

function arrayBufferToBase64url(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = '';
  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
}

// Convert the server's JSON options to the format the browser API expects.
// The server sends base64url strings; the browser API needs ArrayBuffers.
function prepareCreationOptions(json: Record<string, unknown>): PublicKeyCredentialCreationOptions {
  const opts = json as Record<string, unknown>;
  return {
    ...opts,
    challenge: base64urlToArrayBuffer(opts['challenge'] as string),
    user: {
      ...(opts['user'] as Record<string, unknown>),
      id: base64urlToArrayBuffer((opts['user'] as Record<string, unknown>)['id'] as string),
    },
    excludeCredentials: ((opts['excludeCredentials'] as unknown[]) ?? []).map(
      (c: unknown) => {
        const cred = c as Record<string, unknown>;
        return { ...cred, id: base64urlToArrayBuffer(cred['id'] as string) };
      }
    ),
  } as unknown as PublicKeyCredentialCreationOptions;
}

function prepareRequestOptions(json: Record<string, unknown>): PublicKeyCredentialRequestOptions {
  const opts = json as Record<string, unknown>;
  return {
    ...opts,
    challenge: base64urlToArrayBuffer(opts['challenge'] as string),
    allowCredentials: ((opts['allowCredentials'] as unknown[]) ?? []).map(
      (c: unknown) => {
        const cred = c as Record<string, unknown>;
        return { ...cred, id: base64urlToArrayBuffer(cred['id'] as string) };
      }
    ),
  } as unknown as PublicKeyCredentialRequestOptions;
}

// Serialize the browser's attestation response back to a JSON-compatible object for the server.
function serializeAttestation(credential: PublicKeyCredential): Record<string, unknown> {
  const r = credential.response as AuthenticatorAttestationResponse;
  return {
    id: credential.id,
    rawId: arrayBufferToBase64url(credential.rawId),
    type: credential.type,
    response: {
      clientDataJSON: arrayBufferToBase64url(r.clientDataJSON),
      attestationObject: arrayBufferToBase64url(r.attestationObject),
      transports: r.getTransports?.() ?? [],
    },
    clientExtensionResults: credential.getClientExtensionResults(),
  };
}

// Serialize the browser's assertion response back to a JSON-compatible object for the server.
function serializeAssertion(credential: PublicKeyCredential): Record<string, unknown> {
  const r = credential.response as AuthenticatorAssertionResponse;
  return {
    id: credential.id,
    rawId: arrayBufferToBase64url(credential.rawId),
    type: credential.type,
    response: {
      clientDataJSON: arrayBufferToBase64url(r.clientDataJSON),
      authenticatorData: arrayBufferToBase64url(r.authenticatorData),
      signature: arrayBufferToBase64url(r.signature),
      userHandle: r.userHandle ? arrayBufferToBase64url(r.userHandle) : null,
    },
    clientExtensionResults: credential.getClientExtensionResults(),
  };
}

/**
 * Full passkey registration flow:
 * 1. Fetches creation options from the server
 * 2. Prompts the user's device for biometric/PIN confirmation
 * 3. Returns the serialized credential to be posted to /api/passkeys/register/complete
 */
export async function startPasskeyRegistration(): Promise<Record<string, unknown>> {
  const beginRes = await fetch('/api/passkeys/register/begin', { method: 'POST' });
  if (!beginRes.ok) {
    throw new Error('Failed to start passkey registration');
  }
  const optionsJson = (await beginRes.json()) as Record<string, unknown>;
  const options = prepareCreationOptions(optionsJson);

  const credential = await navigator.credentials.create({ publicKey: options });
  if (!credential) {
    throw new Error('No credential returned from device');
  }
  return serializeAttestation(credential as PublicKeyCredential);
}

/**
 * Full passkey authentication flow:
 * 1. Fetches request options from the server
 * 2. Prompts the user's device for biometric/PIN confirmation
 * 3. Returns the serialized credential to be posted to /api/passkeys/login/complete
 */
export async function startPasskeyAssertion(): Promise<Record<string, unknown>> {
  const beginRes = await fetch('/api/passkeys/login/begin', { method: 'POST' });
  if (!beginRes.ok) {
    throw new Error('Failed to start passkey sign-in');
  }
  const optionsJson = (await beginRes.json()) as Record<string, unknown>;
  const options = prepareRequestOptions(optionsJson);

  const credential = await navigator.credentials.get({ publicKey: options });
  if (!credential) {
    throw new Error('No credential returned from device');
  }
  return serializeAssertion(credential as PublicKeyCredential);
}
```

- [ ] **Step 2: Run biome lint check**

```bash
npm run check
```

Fix any issues automatically:

```bash
npm run check:fix
```

- [ ] **Step 3: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Pages/passkey.ts
git commit -m "feat: add WebAuthn browser API utility (passkey.ts)"
```

---

### Task 10: ManagePasskeys.tsx React Page

**Files:**
- Create: `modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManagePasskeys.tsx`

- [ ] **Step 1: Create the page**

Create `modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManagePasskeys.tsx`:

```tsx
import { router } from '@inertiajs/react';
import { Button, CardContent, CardHeader, CardTitle } from '@simplemodule/ui';
import { useState } from 'react';
import ManageLayout from '@/components/ManageLayout'; // '@/' alias resolves to module src root
import { startPasskeyRegistration } from '../../passkey';

interface Passkey {
  credentialId: string;
  name: string;
  createdAt: string;
}

interface Props {
  passkeys: Passkey[];
}

export default function ManagePasskeys({ passkeys }: Props) {
  const [registering, setRegistering] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleAddPasskey() {
    if (!window.PublicKeyCredential) {
      setError('Your browser does not support passkeys.');
      return;
    }
    setRegistering(true);
    setError(null);
    try {
      const credential = await startPasskeyRegistration();
      const res = await fetch('/api/passkeys/register/complete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(credential),
      });
      if (!res.ok) {
        setError('Passkey registration failed. Please try again.');
        return;
      }
      router.reload();
    } catch (err) {
      if (err instanceof Error && err.name === 'NotAllowedError') {
        setError('Registration was cancelled.');
      } else {
        setError('An unexpected error occurred. Please try again.');
      }
    } finally {
      setRegistering(false);
    }
  }

  async function handleDeletePasskey(credentialId: string) {
    if (!confirm('Remove this passkey?')) return;
    const res = await fetch(`/api/passkeys/${encodeURIComponent(credentialId)}`, {
      method: 'DELETE',
    });
    if (res.ok) {
      router.reload();
    } else {
      setError('Failed to remove passkey. Please try again.');
    }
  }

  return (
    <ManageLayout activePage="Passkeys">
      <CardHeader>
        <CardTitle>Passkeys</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-text-muted mb-4">
          Passkeys let you sign in with your fingerprint, face, or device PIN — no password needed.
        </p>

        {error && (
          <div className="alert-danger mb-4 text-sm" role="alert">
            {error}
          </div>
        )}

        {passkeys.length === 0 ? (
          <p className="text-sm text-text-muted mb-4">No passkeys registered yet.</p>
        ) : (
          <ul className="space-y-2 mb-4">
            {passkeys.map((passkey) => (
              <li
                key={passkey.credentialId}
                className="flex items-center justify-between p-3 border border-border rounded-lg"
              >
                <div>
                  <p className="text-sm font-medium">{passkey.name || 'Passkey'}</p>
                  <p className="text-xs text-text-muted">
                    Added {new Date(passkey.createdAt).toLocaleDateString()}
                  </p>
                </div>
                <Button
                  type="button"
                  variant="secondary"
                  className="text-sm"
                  onClick={() => handleDeletePasskey(passkey.credentialId)}
                >
                  Remove
                </Button>
              </li>
            ))}
          </ul>
        )}

        <Button type="button" onClick={handleAddPasskey} disabled={registering}>
          {registering ? 'Registering…' : 'Add passkey'}
        </Button>
      </CardContent>
    </ManageLayout>
  );
}
```

- [ ] **Step 2: Run biome lint check**

```bash
npm run check
```

Fix automatically if needed:

```bash
npm run check:fix
```

- [ ] **Step 3: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Pages/Account/Manage/ManagePasskeys.tsx
git commit -m "feat: add ManagePasskeys React page"
```

---

### Task 11: Update Login.tsx with passkey sign-in button

**Files:**
- Modify: `modules/Users/src/SimpleModule.Users/Pages/Account/Login.tsx`

- [ ] **Step 1: Update the Props interface and add imports**

At the top of `Login.tsx`, add:

```tsx
import { useState } from 'react';
import { startPasskeyAssertion } from '../passkey';
```

Update the Props interface to add `passkeyEnabled`:

```tsx
interface Props {
  returnUrl: string;
  showTestAccounts: boolean;
  passkeyEnabled: boolean;
  errors?: { email?: string };
}
```

- [ ] **Step 2: Add state and handler inside the component**

Add to the component body (after the existing `quickLogin` function). `startPasskeyAssertion` is imported from `'../passkey'` (i.e., `Pages/passkey.ts` relative to `Pages/Account/Login.tsx`):

```tsx
export default function Login({ returnUrl, showTestAccounts, passkeyEnabled, errors }: Props) {
  const [passkeyError, setPasskeyError] = useState<string | null>(null);
  const [passkeyLoading, setPasskeyLoading] = useState(false);

  // ... keep existing handleSubmit and quickLogin ...

  async function handlePasskeySignIn() {
    if (!window.PublicKeyCredential) {
      setPasskeyError('Your browser does not support passkeys.');
      return;
    }
    setPasskeyLoading(true);
    setPasskeyError(null);
    try {
      const credential = await startPasskeyAssertion();
      const res = await fetch(
        `/api/passkeys/login/complete?returnUrl=${encodeURIComponent(returnUrl)}`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(credential),
        }
      );
      if (res.ok) {
        const data = (await res.json()) as { redirectUrl: string };
        window.location.href = data.redirectUrl;
      } else if (res.status === 423) {
        setPasskeyError('Your account is locked. Please try again later.');
      } else {
        setPasskeyError('Passkey sign-in failed. Use your password instead.');
      }
    } catch (err) {
      if (err instanceof Error && err.name === 'NotAllowedError') {
        setPasskeyError('Passkey sign-in was cancelled.');
      } else {
        setPasskeyError('An unexpected error occurred.');
      }
    } finally {
      setPasskeyLoading(false);
    }
  }
```

- [ ] **Step 3: Add the passkey button to the JSX**

Inside `<CardContent>`, after the closing `</form>` tag and before the test accounts section, add:

```tsx
{passkeyEnabled && (
  <>
    <div className="relative my-6">
      <hr />
      <span className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-surface px-3 text-xs text-text-muted">
        or
      </span>
    </div>
    {passkeyError && (
      <div className="alert-danger mb-3 text-sm" role="alert">
        {passkeyError}
      </div>
    )}
    <Button
      type="button"
      variant="secondary"
      className="w-full"
      onClick={handlePasskeySignIn}
      disabled={passkeyLoading}
    >
      <svg
        className="w-4 h-4 mr-2"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
      </svg>
      {passkeyLoading ? 'Signing in…' : 'Sign in with passkey'}
    </Button>
  </>
)}
```

- [ ] **Step 4: Run biome lint check and fix**

```bash
npm run check:fix
```

- [ ] **Step 5: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Pages/Account/Login.tsx
git commit -m "feat: add passkey sign-in button to login page"
```

---

### Task 12: Update LoginEndpoint.cs to pass passkeyEnabled prop

**Files:**
- Modify: `modules/Users/src/SimpleModule.Users/Pages/Account/LoginEndpoint.cs`

- [ ] **Step 1: Inject IOptions<IdentityPasskeyOptions> and pass the flag**

In the GET handler of `LoginEndpoint.cs`, add `IOptions<IdentityPasskeyOptions> passkeyOptions` as a parameter and pass `passkeyEnabled` to `Inertia.Render`.

Add the using at the top:
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
```

Update the GET handler signature:

```csharp
app.MapGet(
    "/Login",
    async (
        HttpContext context,
        ISettingsContracts settingsService,
        ISettingsDefinitionRegistry settingsDefinitions,
        IOptions<IdentityPasskeyOptions> passkeyOptions,
        [FromQuery] string? returnUrl
    ) =>
    {
        await context.SignOutAsync(IdentityConstants.ExternalScheme);

        var showTestAccounts = await settingsService.GetSettingAsync(
            ConfigKeys.ShowTestAccounts,
            SettingScope.System
        );
        showTestAccounts ??= settingsDefinitions
            .GetDefinition(ConfigKeys.ShowTestAccounts)
            ?.DefaultValue;

        return Inertia.Render(
            "Users/Account/Login",
            new
            {
                returnUrl = returnUrl ?? "/",
                showTestAccounts = showTestAccounts == "true",
                passkeyEnabled = !string.IsNullOrEmpty(passkeyOptions.Value.ServerDomain),
            }
        );
    }
)
.AllowAnonymous();
```

- [ ] **Step 2: Build to verify no compilation errors**

```bash
dotnet build
```

Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/Pages/Account/LoginEndpoint.cs
git commit -m "feat: pass passkeyEnabled prop from IdentityPasskeyOptions to login page"
```

---

### Task 13: Update ManageLayout.tsx and Pages/index.ts

**Files:**
- Modify: `modules/Users/src/SimpleModule.Users/components/ManageLayout.tsx`
- Modify: `modules/Users/src/SimpleModule.Users/Pages/index.ts`

- [ ] **Step 1: Add Passkeys to the navItems array in ManageLayout.tsx**

In `components/ManageLayout.tsx`, add this entry to the `navItems` array after the `TwoFactorAuthentication` entry:

```typescript
{
  href: '/Identity/Account/Manage/Passkeys',
  page: 'Passkeys',
  label: 'Passkeys',
  icon: 'M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z',
},
```

- [ ] **Step 2: Register the page in Pages/index.ts**

In `modules/Users/src/SimpleModule.Users/Pages/index.ts`, add to the `pages` object:

```typescript
'Users/Account/Manage/Passkeys': () => import('./Account/Manage/ManagePasskeys'),
```

Place it after the `'Users/Account/Manage/ExternalLogins'` entry.

- [ ] **Step 3: Run biome lint check**

```bash
npm run check:fix
```

- [ ] **Step 4: Validate page registrations**

```bash
npm run validate-pages
```

Expected: No mismatches. The key `"Users/Account/Manage/Passkeys"` in `Pages/index.ts` must match the string in `Inertia.Render(...)` in `ManagePasskeysEndpoint.cs`.

- [ ] **Step 5: Commit**

```bash
git add modules/Users/src/SimpleModule.Users/components/ManageLayout.tsx
git add modules/Users/src/SimpleModule.Users/Pages/index.ts
git commit -m "feat: register ManagePasskeys page and add Passkeys nav item to account sidebar"
```

---

### Task 14: Final verification

- [ ] **Step 1: Full build**

```bash
dotnet build
```

Expected: 0 errors.

- [ ] **Step 2: Run all tests**

```bash
dotnet test
```

Expected: All tests pass.

- [ ] **Step 3: Biome lint + format check**

```bash
npm run check
```

Fix automatically if needed:

```bash
npm run check:fix
```

Expected: No lint errors across `passkey.ts`, `ManagePasskeys.tsx`, and updated `Login.tsx`.

- [ ] **Step 4: Build frontend**

```bash
npm run dev:build
```

Expected: All module builds succeed.

- [ ] **Step 5: Validate page registrations**

```bash
npm run validate-pages
```

Expected: No mismatches.

- [ ] **Step 6: Smoke test (manual)**

Run the dev server and verify:
1. Login page shows "Sign in with passkey" button (when `ServerDomain` is configured)
2. Account Settings → Passkeys page loads
3. "Add passkey" triggers the browser's passkey UI (requires HTTPS or localhost)
4. Registered passkeys appear in the list with a Remove button

```bash
npm run dev
```

Navigate to `https://localhost:5001/Identity/Account/Login`
