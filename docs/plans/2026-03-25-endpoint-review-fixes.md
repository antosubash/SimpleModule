# Endpoint Review Fixes — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix all Minimal API endpoint issues found during the 94-endpoint review: ReadFormAsync anti-patterns, Results→TypedResults migration, HttpContext→ClaimsPrincipal, CrudEndpoints helper adoption, unused usings, IHostEnvironment injection, and multi-endpoint file splits.

**Architecture:** Mechanical refactoring organized by module. Each task is independent and parallelizable. No behavioral changes — only API surface and code quality improvements.

**Tech Stack:** ASP.NET Core Minimal APIs, TypedResults, CrudEndpoints helpers

---

## Task 1: Settings Module — Results → TypedResults

**Files to modify:**
- `modules/Settings/src/Settings/Endpoints/Menus/ClearHomePageEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Menus/CreateMenuItemEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Menus/DeleteMenuItemEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Menus/GetAvailablePagesEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Menus/GetMenuTreeEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Menus/ReorderMenuItemsEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Menus/SetHomePageEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Menus/UpdateMenuItemEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Settings/DeleteSettingEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Settings/GetDefinitionsEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Settings/GetSettingEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Settings/GetSettingsEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/Settings/UpdateSettingEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/UserSettings/DeleteMySettingEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/UserSettings/GetMySettingsEndpoint.cs`
- `modules/Settings/src/Settings/Endpoints/UserSettings/UpdateMySettingEndpoint.cs`

**Changes:** Replace every `Results.Ok(...)`, `Results.NoContent()`, `Results.NotFound()`, `Results.Created(...)`, `Results.Unauthorized()` with `TypedResults.*` equivalents. Note: `TypedResults` has no `.Unauthorized()` — use `TypedResults.Problem(statusCode: 401)` or keep `Results.Unauthorized()` for UserSettings endpoints.

---

## Task 2: Admin Module — ReadFormAsync + Results → TypedResults

**Files to modify:**
- `modules/Admin/src/Admin/Endpoints/Admin/AdminRolesEndpoint.cs`
- `modules/Admin/src/Admin/Endpoints/Admin/AdminUsersEndpoint.cs`
- `modules/Admin/src/Admin/Views/Admin/UsersActivityEndpoint.cs`

**Changes:**
1. `AdminRolesEndpoint.cs`:
   - POST `/` (line 50): Replace `ReadFormAsync()` for `permissions` with `[FromForm] List<string> permissions` parameter. Remove `HttpContext context` if only used for form + user claim (keep it for `context.User`).
   - POST `/{id}/permissions` (line 127): Add `[FromForm] List<string> permissions` parameter. Remove `HttpContext context` form read, keep for `context.User`.
   - Replace all `Results.*` with `TypedResults.*` throughout.

2. `AdminUsersEndpoint.cs`:
   - POST `/` (line 54): Replace `ReadFormAsync()` for `roles` with `[FromForm] List<string> roles` parameter.
   - POST `/{id}/roles` (line 124): Add `[FromForm] List<string> roles` parameter.
   - POST `/{id}/permissions` (line 169): Add `[FromForm] List<string> permissions` parameter.
   - Replace all `Results.*` with `TypedResults.*` throughout.

3. `UsersActivityEndpoint.cs`:
   - Replace `Results.Ok(...)` with `TypedResults.Ok(...)`.

---

## Task 3: OpenIddict Module — ReadFormAsync + Results → TypedResults

**Files to modify:**
- `modules/OpenIddict/src/OpenIddict/Endpoints/OpenIddict/ClientsActionEndpoint.cs`
- `modules/OpenIddict/src/OpenIddict/Endpoints/Connect/AuthorizationEndpoint.cs`
- `modules/OpenIddict/src/OpenIddict/Endpoints/Connect/LogoutEndpoint.cs`
- `modules/OpenIddict/src/OpenIddict/Endpoints/Connect/UserinfoEndpoint.cs`
- `modules/OpenIddict/src/OpenIddict/Views/OpenIddict/ClientsEditEndpoint.cs`

**Changes for ClientsActionEndpoint.cs:**
1. POST `/` (line 46): Add `[FromForm] List<string> redirectUris`, `[FromForm] List<string> postLogoutUris`, `[FromForm] List<string> permissions` parameters. Remove `HttpContext context` and `ReadFormAsync()`.
2. POST `/{id}/uris` (line 112): Add `[FromForm] List<string> redirectUris`, `[FromForm] List<string> postLogoutUris`. Remove `HttpContext context`.
3. POST `/{id}/permissions` (line 145): Add `[FromForm] List<string> permissions`. Remove `HttpContext context`.
4. Replace all `Results.*` with `TypedResults.*`.

**Note:** Connect endpoints (Authorization, Logout, Userinfo) use `Results.Challenge()`, `Results.SignIn()`, `Results.SignOut()` — these have no `TypedResults` equivalents and should stay as-is. Only fix `Results.Ok(claims)` in UserinfoEndpoint.

---

## Task 4: Products Module — Results → TypedResults in Views

**Files to modify:**
- `modules/Products/src/Products/Views/CreateEndpoint.cs`
- `modules/Products/src/Products/Views/EditEndpoint.cs`

**Changes:**
- Replace `Results.Redirect(...)` with `TypedResults.Redirect(...)`.
- Replace `Results.NotFound()` with `TypedResults.NotFound()`.

---

## Task 5: Orders Module — Results → TypedResults + Remove Unused Usings

**Files to modify:**
- `modules/Orders/src/Orders/Views/CreateEndpoint.cs`
- `modules/Orders/src/Orders/Views/EditEndpoint.cs`

**Changes:**
- Replace `Results.Redirect(...)` with `TypedResults.Redirect(...)`.
- Replace `Results.NotFound()` with `TypedResults.NotFound()`.
- Remove `using SimpleModule.Users.Contracts;` from both files (unused).

---

## Task 6: PageBuilder Module — Results → TypedResults + CrudEndpoints

**Files to modify:**
- `modules/PageBuilder/src/PageBuilder/Views/ViewerEndpoint.cs`
- `modules/PageBuilder/src/PageBuilder/Views/ViewerDraftEndpoint.cs`
- `modules/PageBuilder/src/PageBuilder/Views/EditorEndpoint.cs`
- `modules/PageBuilder/src/PageBuilder/Endpoints/Templates/DeleteTemplateEndpoint.cs`
- `modules/PageBuilder/src/PageBuilder/Endpoints/Templates/GetAllTemplatesEndpoint.cs`
- `modules/PageBuilder/src/PageBuilder/Endpoints/Tags/GetAllTagsEndpoint.cs`
- `modules/PageBuilder/src/PageBuilder/Endpoints/Pages/TrashEndpoint.cs`
- `modules/PageBuilder/src/PageBuilder/Endpoints/Pages/PermanentDeleteEndpoint.cs`

**Changes:**
- Views: Replace `Results.NotFound()` with `TypedResults.NotFound()`.
- `DeleteTemplateEndpoint.cs`: Use `CrudEndpoints.Delete(() => templates.DeleteTemplateAsync(id))`.
- `GetAllTemplatesEndpoint.cs`: Use `CrudEndpoints.GetAll(templates.GetAllTemplatesAsync)`.
- `GetAllTagsEndpoint.cs`: Use `CrudEndpoints.GetAll(tags.GetAllTagsAsync)`.
- `TrashEndpoint.cs`: Use `CrudEndpoints.GetAll(pageBuilder.GetTrashedPagesAsync)`.
- `PermanentDeleteEndpoint.cs`: Use `CrudEndpoints.Delete(() => pageBuilder.PermanentDeletePageAsync(id))`.

---

## Task 7: Users Module — Results → TypedResults + HttpContext → ClaimsPrincipal + CrudEndpoints

**Files to modify:**
- `modules/Users/src/Users/Endpoints/Account/AccountSecurityEndpoint.cs`
- `modules/Users/src/Users/Endpoints/Users/GetAllEndpoint.cs`
- `modules/Users/src/Users/Endpoints/Users/CreateEndpoint.cs`
- `modules/Users/src/Users/Endpoints/Users/DeleteEndpoint.cs`
- `modules/Users/src/Users/Endpoints/Users/DownloadPersonalDataEndpoint.cs`
- `modules/Users/src/Users/Views/Account/Disable2faEndpoint.cs`
- `modules/Users/src/Users/Views/Account/EnableAuthenticatorEndpoint.cs`
- `modules/Users/src/Users/Views/Account/GenerateRecoveryCodesEndpoint.cs`
- `modules/Users/src/Users/Views/Account/ResetAuthenticatorEndpoint.cs`
- `modules/Users/src/Users/Views/Account/TwoFactorAuthenticationEndpoint.cs`

**Changes:**
- `AccountSecurityEndpoint.cs`: Replace all `Results.Redirect(...)` with `TypedResults.Redirect(...)`.
- `DownloadPersonalDataEndpoint.cs`: Replace `Results.NotFound()` with `TypedResults.NotFound()` and `Results.File(...)` with `TypedResults.File(...)`.
- `GetAllEndpoint.cs`: Use `CrudEndpoints.GetAll(userContracts.GetAllUsersAsync)`.
- `CreateEndpoint.cs`: Use `CrudEndpoints.Create(() => userContracts.CreateUserAsync(request), u => $"{UsersConstants.RoutePrefix}/{u.Id}")`.
- `DeleteEndpoint.cs`: Use `CrudEndpoints.Delete(() => userContracts.DeleteUserAsync(id))`.
- All 5 View Account endpoints: Replace `HttpContext context` with `ClaimsPrincipal principal`, pass `principal` to `userManager.GetUserAsync(principal)`. Replace `Results.Redirect(...)` with `TypedResults.Redirect(...)`.

---

## Task 8: AuditLogs + Dashboard — Results → TypedResults + IHostEnvironment

**Files to modify:**
- `modules/AuditLogs/src/AuditLogs/Views/DetailEndpoint.cs`
- `modules/Dashboard/src/Dashboard/Views/HomeEndpoint.cs`

**Changes:**
- `DetailEndpoint.cs`: Replace `Results.NotFound()` with `TypedResults.NotFound()`.
- `HomeEndpoint.cs`: Replace `Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"` with injecting `IHostEnvironment env` and using `env.IsDevelopment()`. Add `using Microsoft.Extensions.Hosting;`.

---

## Verification

After all tasks, run:
```bash
dotnet build
dotnet test
```
