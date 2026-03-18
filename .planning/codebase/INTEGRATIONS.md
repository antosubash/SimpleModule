# External Integrations

**Analysis Date:** 2026-03-18

## APIs & External Services

**None detected** - The codebase does not integrate with external third-party APIs (Stripe, Twilio, SendGrid, etc.). All functionality is self-contained within the SimpleModule framework and its modules.

## Data Storage

**Databases:**
- **PostgreSQL 16** (Production)
  - Connection: `Database:DefaultConnection` in `appsettings.json`
  - Client: Entity Framework Core 10.0 via `Npgsql.EntityFrameworkCore.PostgreSQL`
  - Multi-tenant via schemas per module (`ModuleDbContextInfo`)
- **SQLite** (Development/Testing)
  - In-memory for unit tests
  - File-based (`app.db`) for local development
  - Client: Entity Framework Core 10.0 via `Microsoft.EntityFrameworkCore.Sqlite`
- **SQL Server** (Optional)
  - Client: Entity Framework Core 10.0 via `Microsoft.EntityFrameworkCore.SqlServer`
  - Schema-based isolation per module

**File Storage:**
- Local filesystem only - Static assets served from `wwwroot/` directory
- Module pages bundled as `{ModuleName}.pages.js` in `wwwroot/_content/{ModuleName}/`

**Caching:**
- None detected - No distributed cache (Redis) or in-memory cache configured

## Authentication & Identity

**Auth Provider:**
- OpenIddict 5.x - Self-hosted OAuth 2.0 / OpenID Connect provider
  - **Implementation**:
    - `OpenIddictSeedService` (`modules/Users/src/Users/Services/OpenIddictSeedService.cs`) seeds initial clients and applications
    - Dual-auth via policy scheme (`AuthConstants.SmartAuthPolicy`):
      - Bearer token → OpenIddict validation
      - Cookie → ASP.NET Core Identity
  - **Certificate-based signing**: Encryption and signing certificates required
    - `OpenIddict:EncryptionCertPath` - Path to encryption certificate
    - `OpenIddict:SigningCertPath` - Path to signing certificate
    - `OpenIddict:CertPassword` - Optional password for encrypted certificates
  - **User & Role Management**: Microsoft.AspNetCore.Identity with `ApplicationUser` and `ApplicationRole` entities

**Endpoints:**
- `GET /connect/authorize` - OAuth authorization endpoint
- `POST /connect/token` - Token endpoint
- Configured in `modules/Users/src/Users/Constants/ConnectRouteConstants.cs`

**Scopes:**
- `openid` - OpenID Connect scope
- `profile` - User profile data
- `email` - User email

## Monitoring & Observability

**Distributed Tracing:**
- OpenTelemetry (OTEL) exporter (`OpenTelemetry.Exporter.OpenTelemetryProtocol`)
- **Instrumentations**:
  - `OpenTelemetry.Instrumentation.AspNetCore` - ASP.NET Core request/response tracing
  - `OpenTelemetry.Instrumentation.Http` - HTTP client call tracing
  - `OpenTelemetry.Instrumentation.Runtime` - .NET runtime metrics
- Configured via `SimpleModule.ServiceDefaults` (`SimpleModule.ServiceDefaults/SimpleModule.ServiceDefaults.csproj`)

**Error Tracking:**
- None detected - No Sentry, Bugsnag, or similar integration

**Logs:**
- Built-in Microsoft.Extensions.Logging
- Configured in `appsettings.json` and `appsettings.Development.json`
- Levels: Information (default), Warning (Microsoft.AspNetCore)

**Health Checks:**
- Liveness probe: `/health/live` (no database checks, confirms process running)
- Readiness probe: `/health/ready` (includes database connectivity check)
- Implemented: `DatabaseHealthCheck` (`modules/*/Health/*`)

## CI/CD & Deployment

**Hosting:**
- Docker container deployment
- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0`
- Multi-stage Dockerfile: Restore → Build → Publish → Runtime
- Runs on port 8080 in container

**Orchestration & Service Management:**
- .NET Aspire 13.1.2 (`SimpleModule.AppHost`)
  - Service discovery via `Microsoft.Extensions.ServiceDiscovery`
  - Aspire-managed connection strings (e.g., `simplemoduledb`)
  - Resilience policies via `Microsoft.Extensions.Http.Resilience`
  - PostgreSQL hosting via `Aspire.Hosting.PostgreSQL`

**CI Pipeline:**
- Not detected in codebase - Likely GitHub Actions or similar (not included in repo)

## Database Features

**Multi-Provider Support:**
- Schema isolation per module (PostgreSQL, SQL Server use schemas; SQLite uses table prefixes)
- `EF Core Migrations` - Production deployments should use explicit migration tooling (not `EnsureCreated()`)
- `HostDbContext` - Unified context for cross-module entities and OpenIddict data

**Integration with OpenIddict:**
- OpenIddict requires `DbContext.UseOpenIddict()` extension in EF Core configuration
- Implemented in `HostDbContext` for token storage, client definitions, scope management

## Environment Configuration

**Required Environment Variables (for non-Aspire deployment):**
- `Database__DefaultConnection` - Database connection string (overrides `appsettings.json`)
- `ASPNETCORE_Environment` - "Development" or "Production"
- `OpenIddict:EncryptionCertPath` - Path to encryption certificate
- `OpenIddict:SigningCertPath` - Path to signing certificate
- `OpenIddict:CertPassword` - Certificate password (if encrypted)

**Secrets Location:**
- Development: `appsettings.Development.json` (local only, not committed)
- Production: Environment variables or Azure Key Vault (via Aspire)
- User secrets (CLI): Via `UserSecretsId` in `SimpleModule.AppHost.csproj`

**Aspire-Managed:**
- Connection strings via service discovery (PostgreSQL hostname resolution)
- OpenTelemetry configuration

## Webhooks & Callbacks

**Incoming:**
- None detected - No webhook endpoints

**Outgoing:**
- None detected - No external service notifications

## Testing Infrastructure

**Test Database Configuration:**
- SQLite in-memory (via `SimpleModule.Tests.Shared`)
- PostgreSQL (in CI only, via docker-compose)
- Test auth: `CreateAuthenticatedClient(params Claim[] claims)` via custom auth scheme with `X-Test-Claims` header

**Fake Data Generation:**
- Bogus library - Pre-built fakers for all module DTOs and request types

## Cross-Module Communication

**Event Bus:**
- In-process event bus via `IEventBus` interface
- `SimpleModule.Core.Events.EventBus` - Scoped service
- Pattern: `IEventBus.PublishAsync<T>()` → All `IEventHandler<T>` implementations
- Handler failures collected in `AggregateException` (isolated execution)

**Contracts Pattern:**
- Each module exposes public interface (e.g., `IProductContracts`)
- Located in `modules/{Name}/src/{Name}.Contracts/`
- Other modules depend on contracts, never implementations
- Contracts only reference Core, not implementation details

---

*Integration audit: 2026-03-18*
