# Constants Refactoring Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace all hardcoded strings with named constants across the entire project.

**Architecture:** Flat static classes per concern. Cross-cutting constants in `SimpleModule.Core`, module-specific constants in each module, database constants in `SimpleModule.Database`. No tests needed for constant classes themselves — existing tests validate behavior through integration.

**Tech Stack:** C# / .NET 10

---

### Task 1: Create Core Constants — AuthConstants and ConfigKeys

**Files:**
- Create: `src/SimpleModule.Core/Constants/AuthConstants.cs`
- Create: `src/SimpleModule.Core/Constants/ConfigKeys.cs`

**Step 1: Create AuthConstants**

```csharp
namespace SimpleModule.Core.Constants;

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

**Step 2: Create ConfigKeys**

```csharp
namespace SimpleModule.Core.Constants;

public static class ConfigKeys
{
    public const string DatabaseSection = "Database";
    public const string OpenIddictBaseUrl = "OpenIddict:BaseUrl";
    public const string OpenIddictEncryptionCertPath = "OpenIddict:EncryptionCertificatePath";
    public const string OpenIddictSigningCertPath = "OpenIddict:SigningCertificatePath";
    public const string OpenIddictCertPassword = "OpenIddict:CertificatePassword";
    public const string OpenIddictAdditionalRedirectUris = "OpenIddict:AdditionalRedirectUris";
    public const string SeedAdminPassword = "Seed:AdminPassword";
}
```

**Step 3: Build to verify**

Run: `dotnet build src/SimpleModule.Core/SimpleModule.Core.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/SimpleModule.Core/Constants/
git commit -m "feat: add AuthConstants and ConfigKeys to Core"
```

---

### Task 2: Create Core Constants — RouteConstants and ErrorMessages

**Files:**
- Create: `src/SimpleModule.Core/Constants/RouteConstants.cs`
- Create: `src/SimpleModule.Core/Constants/ErrorMessages.cs`

**Step 1: Create RouteConstants**

```csharp
namespace SimpleModule.Core.Constants;

public static class RouteConstants
{
    public const string HealthLive = "/health/live";
    public const string HealthReady = "/health/ready";
    public const string ConnectAuthorize = "/connect/authorize";
    public const string ConnectToken = "/connect/token";
    public const string ConnectEndSession = "/connect/endsession";
    public const string ConnectUserInfo = "/connect/userinfo";
}
```

**Step 2: Create ErrorMessages**

```csharp
namespace SimpleModule.Core.Constants;

public static class ErrorMessages
{
    // Exception titles (used in ProblemDetails)
    public const string ValidationErrorTitle = "Validation Error";
    public const string NotFoundTitle = "Not Found";
    public const string ConflictTitle = "Conflict";
    public const string InternalServerErrorTitle = "Internal Server Error";

    // Default exception messages
    public const string UnexpectedError = "An unexpected error occurred. Please try again later.";
    public const string DefaultValidationMessage = "One or more validation errors occurred.";
    public const string DefaultNotFoundMessage = "The requested resource was not found.";
    public const string DefaultConflictMessage = "A conflict occurred.";

    // OpenID Connect errors
    public const string OpenIdConnectRequestMissing = "The OpenID Connect request cannot be retrieved.";
    public const string UserDetailsMissing = "The user details cannot be retrieved.";
}
```

**Step 3: Build to verify**

Run: `dotnet build src/SimpleModule.Core/SimpleModule.Core.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/SimpleModule.Core/Constants/
git commit -m "feat: add RouteConstants and ErrorMessages to Core"
```

---

### Task 3: Create Core Constants — HealthCheckConstants, ClientConstants, SeedConstants, PersonalDataKeys

**Files:**
- Create: `src/SimpleModule.Core/Constants/HealthCheckConstants.cs`
- Create: `src/SimpleModule.Core/Constants/ClientConstants.cs`
- Create: `src/SimpleModule.Core/Constants/SeedConstants.cs`
- Create: `src/SimpleModule.Core/Constants/PersonalDataKeys.cs`

**Step 1: Create HealthCheckConstants**

```csharp
namespace SimpleModule.Core.Constants;

public static class HealthCheckConstants
{
    public const string DatabaseCheckName = "database";
    public const string ReadyTag = "ready";
    public const string AllDatabasesReachable = "All module databases are reachable.";
    public const string DatabaseHealthCheckFailed = "Database health check failed.";
    public const string CannotConnectFormat = "Cannot connect to database for module '{0}'";
}
```

**Step 2: Create ClientConstants**

```csharp
namespace SimpleModule.Core.Constants;

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

**Step 3: Create SeedConstants**

```csharp
namespace SimpleModule.Core.Constants;

public static class SeedConstants
{
    public const string AdminRole = "Admin";
    public const string AdminRoleDescription = "Administrator role with full access";
    public const string AdminEmail = "admin@simplemodule.dev";
    public const string AdminDisplayName = "Admin";
    public const string DefaultAdminPassword = "Admin123!";
}
```

**Step 4: Create PersonalDataKeys**

```csharp
namespace SimpleModule.Core.Constants;

public static class PersonalDataKeys
{
    public const string Id = "Id";
    public const string UserName = "UserName";
    public const string Email = "Email";
    public const string PhoneNumber = "PhoneNumber";
    public const string DisplayName = "DisplayName";
    public const string CreatedAt = "CreatedAt";
    public const string LastLoginAt = "LastLoginAt";
    public const string AuthenticatorKey = "Authenticator Key";
    public const string ExternalLoginFormat = "{0} external login provider key";
    public const string NullPlaceholder = "null";
    public const string PersonalDataFileName = "PersonalData.json";
    public const string PersonalDataContentType = "application/json";
}
```

**Step 5: Build to verify**

Run: `dotnet build src/SimpleModule.Core/SimpleModule.Core.csproj`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add src/SimpleModule.Core/Constants/
git commit -m "feat: add HealthCheckConstants, ClientConstants, SeedConstants, PersonalDataKeys to Core"
```

---

### Task 4: Create Database Constants

**Files:**
- Create: `src/SimpleModule.Database/DatabaseConstants.cs`

**Step 1: Create DatabaseConstants**

```csharp
namespace SimpleModule.Database;

public static class DatabaseConstants
{
    public const string SectionName = "Database";
    public const string PostgresHostPrefix = "Host=";
    public const string SqlServerCatalogPrefix = "Initial Catalog=";
    public const string SqlServerLocalPrefix = @"Server=.\";
    public const string SqlServerExpressionPrefix = @"Server=(";
}
```

**Step 2: Build to verify**

Run: `dotnet build src/SimpleModule.Database/SimpleModule.Database.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/SimpleModule.Database/DatabaseConstants.cs
git commit -m "feat: add DatabaseConstants"
```

---

### Task 5: Create Module Constants — OrdersConstants, ProductsConstants, UsersConstants

**Files:**
- Create: `src/modules/Orders/Orders/OrdersConstants.cs`
- Create: `src/modules/Products/Products/ProductsConstants.cs`
- Create: `src/modules/Users/Users/UsersConstants.cs`

**Step 1: Create OrdersConstants**

```csharp
namespace SimpleModule.Orders;

public static class OrdersConstants
{
    public const string ModuleName = "Orders";
    public const string RoutePrefix = "/api/orders";

    public static class Fields
    {
        public const string UserId = "UserId";
        public const string Items = "Items";
    }

    public static class ValidationMessages
    {
        public const string UserIdRequired = "UserId is required.";
        public const string AtLeastOneItemRequired = "At least one item is required.";
        public const string QuantityMustBePositiveFormat = "Items[{0}].Quantity must be greater than 0.";
    }
}
```

**Step 2: Create ProductsConstants**

```csharp
namespace SimpleModule.Products;

public static class ProductsConstants
{
    public const string ModuleName = "Products";
    public const string RoutePrefix = "/api/products";
}
```

**Step 3: Create UsersConstants**

```csharp
namespace SimpleModule.Users;

public static class UsersConstants
{
    public const string ModuleName = "Users";
    public const string RoutePrefix = "/api/users";
    public const string DownloadPersonalDataRoute = "/download-personal-data";
    public const string MeRoute = "/me";
}
```

**Step 4: Build to verify**

Run: `dotnet build`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add src/modules/Orders/Orders/OrdersConstants.cs src/modules/Products/Products/ProductsConstants.cs src/modules/Users/Users/UsersConstants.cs
git commit -m "feat: add OrdersConstants, ProductsConstants, UsersConstants"
```

---

### Task 6: Replace hardcoded strings in SimpleModule.Core exceptions

**Files:**
- Modify: `src/SimpleModule.Core/Exceptions/GlobalExceptionHandler.cs`
- Modify: `src/SimpleModule.Core/Exceptions/ValidationException.cs`
- Modify: `src/SimpleModule.Core/Exceptions/NotFoundException.cs`
- Modify: `src/SimpleModule.Core/Exceptions/ConflictException.cs`

**Step 1: Update GlobalExceptionHandler.cs**

Replace:
```csharp
"Validation Error",
```
With:
```csharp
Constants.ErrorMessages.ValidationErrorTitle,
```

Replace:
```csharp
"Not Found",
```
With:
```csharp
Constants.ErrorMessages.NotFoundTitle,
```

Replace:
```csharp
"Conflict", null
```
With:
```csharp
Constants.ErrorMessages.ConflictTitle, null
```

Replace:
```csharp
"Internal Server Error", null
```
With:
```csharp
Constants.ErrorMessages.InternalServerErrorTitle, null
```

Replace:
```csharp
"An unexpected error occurred. Please try again later."
```
With:
```csharp
Constants.ErrorMessages.UnexpectedError
```

Add `using SimpleModule.Core.Constants;` at the top (or use fully qualified names).

**Step 2: Update ValidationException.cs**

Replace both occurrences of:
```csharp
"One or more validation errors occurred."
```
With:
```csharp
Constants.ErrorMessages.DefaultValidationMessage
```

Add `using SimpleModule.Core.Constants;`.

**Step 3: Update NotFoundException.cs**

Replace:
```csharp
"The requested resource was not found."
```
With:
```csharp
Constants.ErrorMessages.DefaultNotFoundMessage
```

Add `using SimpleModule.Core.Constants;`.

**Step 4: Update ConflictException.cs**

Replace:
```csharp
"A conflict occurred."
```
With:
```csharp
Constants.ErrorMessages.DefaultConflictMessage
```

Add `using SimpleModule.Core.Constants;`.

**Step 5: Build and test**

Run: `dotnet build src/SimpleModule.Core/SimpleModule.Core.csproj && dotnet test tests/SimpleModule.Core.Tests/SimpleModule.Core.Tests.csproj`
Expected: Build succeeded, all tests pass

**Step 6: Commit**

```bash
git add src/SimpleModule.Core/Exceptions/
git commit -m "refactor: use ErrorMessages constants in Core exceptions"
```

---

### Task 7: Replace hardcoded strings in SimpleModule.Database

**Files:**
- Modify: `src/SimpleModule.Database/DatabaseProviderDetector.cs`
- Modify: `src/SimpleModule.Database/ModuleDbContextOptionsBuilder.cs`
- Modify: `src/SimpleModule.Database/Health/DatabaseHealthCheck.cs`

**Step 1: Update DatabaseProviderDetector.cs**

Replace:
```csharp
connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
```
With:
```csharp
connectionString.Contains(DatabaseConstants.PostgresHostPrefix, StringComparison.OrdinalIgnoreCase)
```

Replace:
```csharp
connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase)
```
With:
```csharp
connectionString.Contains(DatabaseConstants.SqlServerCatalogPrefix, StringComparison.OrdinalIgnoreCase)
```

Replace:
```csharp
connectionString.Contains(@"Server=.\", StringComparison.OrdinalIgnoreCase)
```
With:
```csharp
connectionString.Contains(DatabaseConstants.SqlServerLocalPrefix, StringComparison.OrdinalIgnoreCase)
```

Replace:
```csharp
connectionString.Contains(@"Server=(", StringComparison.OrdinalIgnoreCase)
```
With:
```csharp
connectionString.Contains(DatabaseConstants.SqlServerExpressionPrefix, StringComparison.OrdinalIgnoreCase)
```

**Step 2: Update ModuleDbContextOptionsBuilder.cs**

Replace both occurrences of:
```csharp
configuration.GetSection("Database")
```
With:
```csharp
configuration.GetSection(DatabaseConstants.SectionName)
```

**Step 3: Update DatabaseHealthCheck.cs**

Add `using SimpleModule.Core.Constants;`.

Replace:
```csharp
$"Cannot connect to database for module '{info.ModuleName}'"
```
With:
```csharp
string.Format(HealthCheckConstants.CannotConnectFormat, info.ModuleName)
```

Replace:
```csharp
"All module databases are reachable."
```
With:
```csharp
HealthCheckConstants.AllDatabasesReachable
```

Replace:
```csharp
"Database health check failed."
```
With:
```csharp
HealthCheckConstants.DatabaseHealthCheckFailed
```

**Step 4: Build and test**

Run: `dotnet build src/SimpleModule.Database/SimpleModule.Database.csproj`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add src/SimpleModule.Database/
git commit -m "refactor: use constants in Database project"
```

---

### Task 8: Replace hardcoded strings in SimpleModule.Api/Program.cs

**Files:**
- Modify: `src/SimpleModule.Api/Program.cs`

**Step 1: Add using statements**

Add:
```csharp
using SimpleModule.Core.Constants;
```

**Step 2: Replace all hardcoded strings**

OAuth2 security definition — replace `"oauth2"` with `AuthConstants.OAuth2Scheme` (lines 19, 41, 42).

Authorization/Token URLs — replace `"/connect/authorize"` with `RouteConstants.ConnectAuthorize` (line 27), `"/connect/token"` with `RouteConstants.ConnectToken` (line 28).

Scopes dictionary keys/values — replace `"openid"` with `AuthConstants.OpenIdScope`, `"profile"` with `AuthConstants.ProfileScope`, `"email"` with `AuthConstants.EmailScope` (lines 31-33, 46-47).

Scope display names (`"OpenID"`, `"Profile"`, `"Email"`) — keep as-is (they are display labels, not functional identifiers).

SmartAuth policy — replace `"SmartAuth"` with `AuthConstants.SmartAuthPolicy` (lines 63, 75, 76, 77).

Health checks — replace `"database"` with `HealthCheckConstants.DatabaseCheckName`, `"ready"` with `HealthCheckConstants.ReadyTag` (lines 83, 117).

Health endpoints — replace `"/health/live"` with `RouteConstants.HealthLive`, `"/health/ready"` with `RouteConstants.HealthReady` (lines 111, 115).

Swagger client ID — replace `"simplemodule-client"` with `ClientConstants.ClientId` (line 97).

**Step 3: Build to verify**

Run: `dotnet build src/SimpleModule.Api/SimpleModule.Api.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/SimpleModule.Api/Program.cs
git commit -m "refactor: use constants in Program.cs"
```

---

### Task 9: Replace hardcoded strings in Orders module

**Files:**
- Modify: `src/modules/Orders/Orders/OrdersModule.cs`
- Modify: `src/modules/Orders/Orders/Features/CreateOrder/CreateOrderEndpoint.cs`
- Modify: `src/modules/Orders/Orders/Features/CreateOrder/CreateOrderRequestValidator.cs`

**Step 1: Update OrdersModule.cs**

Replace:
```csharp
[Module("Orders")]
```
With:
```csharp
[Module(OrdersConstants.ModuleName)]
```

Replace:
```csharp
services.AddModuleDbContext<OrdersDbContext>(configuration, "Orders");
```
With:
```csharp
services.AddModuleDbContext<OrdersDbContext>(configuration, OrdersConstants.ModuleName);
```

Replace:
```csharp
endpoints.MapGroup("/api/orders");
```
With:
```csharp
endpoints.MapGroup(OrdersConstants.RoutePrefix);
```

**Step 2: Update CreateOrderEndpoint.cs**

Replace:
```csharp
TypedResults.Created($"/api/orders/{order.Id}", order);
```
With:
```csharp
TypedResults.Created($"{OrdersConstants.RoutePrefix}/{order.Id}", order);
```

**Step 3: Update CreateOrderRequestValidator.cs**

Replace field name strings and validation messages with `OrdersConstants.Fields.*` and `OrdersConstants.ValidationMessages.*`.

Replace:
```csharp
errors["UserId"] = ["UserId is required."];
```
With:
```csharp
errors[OrdersConstants.Fields.UserId] = [OrdersConstants.ValidationMessages.UserIdRequired];
```

Replace:
```csharp
errors["Items"] = ["At least one item is required."];
```
With:
```csharp
errors[OrdersConstants.Fields.Items] = [OrdersConstants.ValidationMessages.AtLeastOneItemRequired];
```

Replace:
```csharp
itemErrors.Add($"Items[{i}].Quantity must be greater than 0.");
```
With:
```csharp
itemErrors.Add(string.Format(OrdersConstants.ValidationMessages.QuantityMustBePositiveFormat, i));
```

Replace:
```csharp
errors["Items"] = [.. itemErrors];
```
With:
```csharp
errors[OrdersConstants.Fields.Items] = [.. itemErrors];
```

**Step 4: Build and test**

Run: `dotnet build src/modules/Orders/Orders/Orders.csproj && dotnet test tests/modules/Orders.Tests/Orders.Tests.csproj`
Expected: Build succeeded, all tests pass

**Step 5: Commit**

```bash
git add src/modules/Orders/
git commit -m "refactor: use OrdersConstants in Orders module"
```

---

### Task 10: Replace hardcoded strings in Products module

**Files:**
- Modify: `src/modules/Products/Products/ProductsModule.cs`

**Step 1: Update ProductsModule.cs**

Replace:
```csharp
[Module("Products")]
```
With:
```csharp
[Module(ProductsConstants.ModuleName)]
```

Replace:
```csharp
services.AddModuleDbContext<ProductsDbContext>(configuration, "Products");
```
With:
```csharp
services.AddModuleDbContext<ProductsDbContext>(configuration, ProductsConstants.ModuleName);
```

Replace:
```csharp
endpoints.MapGroup("/api/products");
```
With:
```csharp
endpoints.MapGroup(ProductsConstants.RoutePrefix);
```

**Step 2: Build to verify**

Run: `dotnet build src/modules/Products/Products/Products.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/modules/Products/
git commit -m "refactor: use ProductsConstants in Products module"
```

---

### Task 11: Replace hardcoded strings in Users module — UsersModule.cs

**Files:**
- Modify: `src/modules/Users/Users/UsersModule.cs`

**Step 1: Add using statements**

Add:
```csharp
using SimpleModule.Core.Constants;
```

**Step 2: Replace module identity strings**

Replace `[Module("Users")]` with `[Module(UsersConstants.ModuleName)]`.

Replace `"Users"` in `AddModuleDbContext` with `UsersConstants.ModuleName`.

Replace `"/api/users"` with `UsersConstants.RoutePrefix`.

**Step 3: Replace OpenIddict configuration strings**

Replace `"/connect/authorize"` with `RouteConstants.ConnectAuthorize`.
Replace `"/connect/token"` with `RouteConstants.ConnectToken`.
Replace `"/connect/endsession"` with `RouteConstants.ConnectEndSession`.
Replace `"/connect/userinfo"` with `RouteConstants.ConnectUserInfo`.

Replace `configuration["OpenIddict:EncryptionCertificatePath"]` with `configuration[ConfigKeys.OpenIddictEncryptionCertPath]`.
Replace `configuration["OpenIddict:SigningCertificatePath"]` with `configuration[ConfigKeys.OpenIddictSigningCertPath]`.
Replace `configuration["OpenIddict:CertificatePassword"]` with `configuration[ConfigKeys.OpenIddictCertPassword]`.

Replace `options.RegisterScopes("openid", "profile", "email", "roles")` with `options.RegisterScopes(AuthConstants.OpenIdScope, AuthConstants.ProfileScope, AuthConstants.EmailScope, AuthConstants.RolesScope)`.

**Step 4: Replace personal data strings**

Replace `"/download-personal-data"` with `UsersConstants.DownloadPersonalDataRoute`.

Replace personal data dictionary keys with `PersonalDataKeys.*` constants.
Replace `"null"` fallback values with `PersonalDataKeys.NullPlaceholder`.
Replace `"Authenticator Key"` with `PersonalDataKeys.AuthenticatorKey`.
Replace `$"{l.LoginProvider} external login provider key"` with `string.Format(PersonalDataKeys.ExternalLoginFormat, l.LoginProvider)`.
Replace `"application/json"` with `PersonalDataKeys.PersonalDataContentType`.
Replace `"PersonalData.json"` with `PersonalDataKeys.PersonalDataFileName`.

**Step 5: Build to verify**

Run: `dotnet build src/modules/Users/Users/Users.csproj`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add src/modules/Users/Users/UsersModule.cs
git commit -m "refactor: use constants in UsersModule"
```

---

### Task 12: Replace hardcoded strings in Users module — Connect endpoints and OpenIddictSeedService

**Files:**
- Modify: `src/modules/Users/Users/Features/Connect/AuthorizationEndpoint.cs`
- Modify: `src/modules/Users/Users/Features/Connect/LogoutEndpoint.cs`
- Modify: `src/modules/Users/Users/Features/Connect/UserinfoEndpoint.cs`
- Modify: `src/modules/Users/Users/Services/OpenIddictSeedService.cs`

**Step 1: Update AuthorizationEndpoint.cs**

Add `using SimpleModule.Core.Constants;`.

Replace `"/connect/authorize"` with `RouteConstants.ConnectAuthorize`.
Replace `"The OpenID Connect request cannot be retrieved."` with `ErrorMessages.OpenIdConnectRequestMissing`.
Replace `"The user details cannot be retrieved."` with `ErrorMessages.UserDetailsMissing`.
Replace `"roles"` (line 116) with `AuthConstants.RolesScope`.

**Step 2: Update LogoutEndpoint.cs**

Add `using SimpleModule.Core.Constants;`.

Replace `"/connect/endsession"` with `RouteConstants.ConnectEndSession`.

**Step 3: Update UserinfoEndpoint.cs**

Add `using SimpleModule.Core.Constants;`.

Replace `"/connect/userinfo"` with `RouteConstants.ConnectUserInfo`.

**Step 4: Update OpenIddictSeedService.cs**

Add `using SimpleModule.Core.Constants;`.

Replace `"simplemodule-client"` (both occurrences) with `ClientConstants.ClientId`.
Replace `"SimpleModule Client"` with `ClientConstants.ClientDisplayName`.
Replace `configuration["OpenIddict:BaseUrl"] ?? "https://localhost:5001"` with `configuration[ConfigKeys.OpenIddictBaseUrl] ?? ClientConstants.DefaultBaseUrl`.
Replace `"/swagger/oauth2-redirect.html"` with `ClientConstants.SwaggerCallbackPath`.
Replace `"/oauth-callback"` with `ClientConstants.OAuthCallbackPath`.
Replace `"/"` in PostLogoutRedirectUris with `ClientConstants.PostLogoutRedirectPath`.
Replace `configuration.GetSection("OpenIddict:AdditionalRedirectUris")` with `configuration.GetSection(ConfigKeys.OpenIddictAdditionalRedirectUris)`.
Replace `"roles"` (line 72) with `AuthConstants.RolesScope`.

Replace `"Admin"` role references (lines 94, 104, 143) with `SeedConstants.AdminRole`.
Replace `"Administrator role with full access"` with `SeedConstants.AdminRoleDescription`.
Replace `"admin@simplemodule.dev"` (lines 123, 132, 133) with `SeedConstants.AdminEmail`.
Replace `"Admin"` display name (line 134) with `SeedConstants.AdminDisplayName`.
Replace `configuration["Seed:AdminPassword"] ?? "Admin123!"` with `configuration[ConfigKeys.SeedAdminPassword] ?? SeedConstants.DefaultAdminPassword`.

**Step 5: Build and test**

Run: `dotnet build src/modules/Users/Users/Users.csproj`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add src/modules/Users/Users/
git commit -m "refactor: use constants in Users Connect endpoints and seed service"
```

---

### Task 13: Run full build and all tests

**Step 1: Full build**

Run: `dotnet build`
Expected: Build succeeded with 0 errors

**Step 2: Run all tests**

Run: `dotnet test`
Expected: All tests pass

**Step 3: Final commit if any fixups needed**

If any test adjustments were needed, commit them.
