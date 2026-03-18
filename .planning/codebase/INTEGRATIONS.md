# External Integrations

**Analysis Date:** 2026-03-18

## APIs & External Services

**None Detected:** The codebase does not integrate with external API services (Stripe, SendGrid, Twilio, etc.). All services are self-hosted within the application.

## Data Storage

**Databases:**

**SQLite:**
- Connection: `Database:DefaultConnection` → `Data Source=app.db` (default `appsettings.json`)
- Client: Entity Framework Core with `Microsoft.EntityFrameworkCore.Sqlite` provider
- Usage: Development, unit tests (in-memory SQLite via integration tests)
- Schema: Table prefixes per module isolation

**PostgreSQL:**
- Connection: `Database:DefaultConnection` → `Host=localhost;Database=simplemodule_dev;Username=postgres;Password=postgres` (via `appsettings.Development.json`)
- Client: Entity Framework Core with `Npgsql.EntityFrameworkCore.PostgreSQL` provider
- Usage: Local development (when Development environment is active), CI integration tests
- Schema: Schemas per module for isolation

**SQL Server (Optional):**
- Provider: `Microsoft.EntityFrameworkCore.SqlServer` available but no default configuration
- Usage: Not currently wired; available for production deployments

**File Storage:**
- Local filesystem only (`wwwroot/` directory for static assets)
- No cloud storage integrations detected (AWS S3, Azure Blob, etc.)

**Caching:**
- Not explicitly configured
- EF Core query caching via DbContext in-memory

## Authentication & Identity

**Auth Provider:**
- OpenIddict 3+ - Custom OAuth2/OIDC provider (self-hosted)
- **Not external OAuth** (no Google, GitHub, Microsoft integrations detected)

**Implementation Approach:**
- OpenIddict.AspNetCore for OAuth2 token validation
- OpenIddict.EntityFrameworkCore for token/scope persistence
- Authorization Code flow configured in Swagger with scopes: `openid`, `profile`, `email`
- Smart auth routing:
  - Bearer token (Authorization header) → OpenIddict validation
  - No bearer → Fall through to Identity cookies (for Blazor SSR)
- Routes configured in `SimpleModule.Users` module (inferred from `ConnectRouteConstants`)

**User Management:**
- Microsoft.AspNetCore.Identity (via `SimpleModule.Users` contracts)
- Identity roles and claims stored in EF Core
- Test auth: Custom `X-Test-Claims` header binding in test helper (`SimpleModule.Tests.Shared`)

## Monitoring & Observability

**Error Tracking:**
- Not detected (no Sentry, DataDog, Application Insights)
- Global exception handler in `./template/SimpleModule.Host/Program.cs` via `AddExceptionHandler<GlobalExceptionHandler>()`
- Returns ProblemDetails standardized error responses

**Logs:**
- Console logging (default ASP.NET Core)
- Log levels configured in `appsettings.json`:
  - Default: Information
  - Microsoft.AspNetCore: Warning
- No external log aggregation detected

**Distributed Tracing:**
- Aspire service defaults (`builder.AddServiceDefaults()`) enables OpenTelemetry
- Export target: Not configured; likely configured at orchestration level
- Trace context: Enabled for request correlation

**Health Checks:**
- Health endpoint: `/health/live` (referenced in `tests/e2e/playwright.config.ts`)
- Implementation: Likely in `SimpleModule.Database.Health` namespace (inferred)

## CI/CD & Deployment

**Hosting:**
- Self-hosted / on-premises capable
- Aspire support for orchestrated deployments (`SimpleModule.AppHost` project)
- HTTPS required (localhost:5001 default for development)

**CI Pipeline:**
- GitHub Actions (inferred from `playwright.config.ts` with `process.env.CI` branching)
- Test matrix: SQLite (in-memory) and PostgreSQL (container)
- Browser matrix in Playwright: Chromium (default), Firefox + WebKit (CI only)
- Playwright retries: 0 locally, 2 in CI
- Parallel workers: Full parallelism locally, single-worker in CI

**Build Artifacts:**
- .NET: NuGet packages (if published)
- Frontend: Vite bundles (`app.js`, vendor bundles in `wwwroot/js/`)
- Modules: React page bundles (`{ModuleName}.pages.js` in module `wwwroot/`)

## Environment Configuration

**Required env vars:**
- None explicitly required; all critical config via `appsettings.json`
- Optional Aspire bridge: `ConnectionStrings:simplemoduledb` → `Database:DefaultConnection` mapping in Program.cs

**Database Selection:**
- Default: SQLite (`Data Source=app.db`)
- Development: PostgreSQL (via `appsettings.Development.json`)
- Custom: Override `Database:DefaultConnection` in environment config

**Secrets Location:**
- Not detected in this analysis
- Typical flow: User Secrets (local development) or Azure Key Vault (production) via Aspire

## Webhooks & Callbacks

**Incoming:**
- Not detected (no webhook endpoints in analyzed endpoints)

**Outgoing:**
- Event bus (`IEventBus.PublishAsync<T>()`) for internal event pub/sub
- No external webhook calls detected
- Event handlers isolated with `AggregateException` collection for failures

## Cross-Cutting Integrations

**Service Discovery:**
- Aspire service defaults for service-to-service resilience and health checks

**OAuth2 Scopes (defined in OpenIddict config):**
- `openid` - OpenID Connect identity scope
- `profile` - User profile information
- `email` - User email address

**Test Infrastructure:**
- `SimpleModule.Tests.Shared` provides `SimpleModuleWebApplicationFactory`
  - In-memory SQLite for unit tests
  - Custom test auth scheme with claims passed via `X-Test-Claims` header
  - Pre-built `FakeDataGenerators` (Bogus) for all DTOs and request types

**Module Communication:**
- Contracts pattern: `.Contracts` projects expose `[Dto]` types and interfaces
- Event bus: `IEventHandler<T>` subscribers to `IEventBus.PublishAsync<T>()` broadcasts

---

*Integration audit: 2026-03-18*
