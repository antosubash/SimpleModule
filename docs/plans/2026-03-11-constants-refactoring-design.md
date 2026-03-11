# Constants Refactoring Design

## Goal

Remove all hardcoded strings from the project and replace them with named constants. Full sweep across all C# source files.

## Organization

Flat static classes per concern. Cross-cutting constants in Core, module-specific constants in each module, database constants in Database project.

### SimpleModule.Core Constants

| Class | Members |
|-------|---------|
| `AuthConstants` | `OAuth2Scheme`, `SmartAuthPolicy`, `OpenIdScope`, `ProfileScope`, `EmailScope`, `RolesScope` |
| `ConfigKeys` | `DatabaseSection`, `OpenIddictBaseUrl`, `OpenIddictEncryptionCertPath`, `OpenIddictSigningCertPath`, `OpenIddictCertPassword`, `OpenIddictAdditionalRedirectUris`, `SeedAdminPassword` |
| `RouteConstants` | `HealthLive`, `HealthReady`, `ConnectAuthorize`, `ConnectToken`, `ConnectEndSession`, `ConnectUserInfo` |
| `ErrorMessages` | `UnexpectedError`, `ValidationErrorTitle`, `NotFoundTitle`, `ConflictTitle`, `InternalServerErrorTitle`, `DefaultValidationMessage`, `DefaultNotFoundMessage`, `DefaultConflictMessage`, `OpenIdConnectRequestMissing`, `UserDetailsMissing` |
| `HealthCheckConstants` | `DatabaseCheckName`, `ReadyTag`, `AllDatabasesReachable`, `DatabaseHealthCheckFailed`, `CannotConnectFormat` |
| `ClientConstants` | `ClientId`, `ClientDisplayName`, `SwaggerCallbackUri`, `OAuthCallbackUri`, `PostLogoutRedirectUri` |
| `SeedConstants` | `AdminRole`, `AdminRoleDescription`, `AdminEmail`, `AdminDisplayName`, `DefaultAdminPassword` |
| `PersonalDataKeys` | `Id`, `UserName`, `Email`, `PhoneNumber`, `DisplayName`, `CreatedAt`, `LastLoginAt`, `AuthenticatorKey`, `NullPlaceholder` |

### Module Constants

| Class | Location | Members |
|-------|----------|---------|
| `OrdersConstants` | Orders module | `ModuleName`, `RoutePrefix`, validation messages, field names (`UserId`, `Items`) |
| `ProductsConstants` | Products module | `ModuleName`, `RoutePrefix` |
| `UsersConstants` | Users module | `ModuleName`, `RoutePrefix`, `PersonalDataFileName`, `PersonalDataContentType`, `DownloadPersonalDataRoute`, `MeRoute` |

### SimpleModule.Database Constants

| Class | Members |
|-------|---------|
| `DatabaseConstants` | `SectionName`, connection string detection patterns (`PostgresHostPrefix`, `SqlServerCatalogPrefix`, `SqlServerLocalPrefix`, `SqlServerExpressionPrefix`) |

## Exclusions

- Inline route templates (`"/"`, `"/{id}"`) — framework conventions
- SQL query strings — already domain-specific and rarely change
- Structured logging message templates — conventionally inline
- Test assertion messages

## Trade-offs

- **Pros**: Single source of truth, prevents typo bugs, easy to search/change, IDE navigation
- **Cons**: More files, indirection when reading code
