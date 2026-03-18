# Technology Stack: Release Hardening

**Project:** SimpleModule Release Hardening
**Researched:** 2026-03-18
**Mode:** Ecosystem (hardening additions to existing stack)

## Recommended Additions

These are tools and libraries to ADD to the existing stack for hardening. The core stack (.NET 10, React 19, Inertia.js, EF Core, xUnit.v3) does not change.

### Rate Limiting

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| `Microsoft.AspNetCore.RateLimiting` | Built-in (.NET 10) | Per-endpoint rate limiting | Built into ASP.NET Core since .NET 7. No external dependency. Supports fixed window, sliding window, token bucket, and concurrency limiters. The project needs rate limiting on the personal data download endpoint specifically. Use named policies attached to individual endpoints via `RequireRateLimiting("policy-name")`. |

**Confidence:** HIGH -- built-in middleware, documented for ASP.NET Core 10.0.

**Do NOT use:** `AspNetCoreRateLimit` (third-party NuGet). It was the standard before .NET 7 but is now unnecessary since the built-in middleware covers all use cases. Adding it creates a redundant dependency.

**Implementation pattern:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("personal-data", opt =>
    {
        opt.PermitLimit = 1;
        opt.Window = TimeSpan.FromHours(1);
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// On the endpoint:
app.MapGet("/download-personal-data", handler).RequireRateLimiting("personal-data");
```

### Static Analysis (Security)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| `Meziantou.Analyzer` | 2.0.260 | Security + best practice Roslyn analyzer | Catches security issues (hardcoded passwords, insecure crypto, missing disposal), performance issues, and .NET best practices. 15M+ NuGet downloads. Complements the existing Roslynator.Analyzers which focuses on code style/quality rather than security. Add to `Directory.Build.props` alongside Roslynator. |

**Confidence:** HIGH -- actively maintained (released Dec 2025), widely adopted, works alongside existing Roslynator.

**Already in place (keep):**
- `Roslynator.Analyzers` -- already in Directory.Build.props, good for code quality
- `AnalysisLevel=latest-all` + `AnalysisMode=All` -- already enables all built-in .NET SDK analyzers
- `TreatWarningsAsErrors=true` -- already enforces zero-warning policy

**Do NOT use:**
- `SecurityCodeScan` -- last meaningful update was 2022, hasn't kept pace with .NET 8/9/10 changes. Meziantou covers its key security rules.
- `SonarAnalyzer.CSharp` -- good but heavy; designed for SonarQube/SonarCloud pipeline integration. Overkill for a framework project without that infrastructure. Meziantou covers 80% of the same ground with zero setup.

**Configuration approach:** Add to `Directory.Build.props` with `PrivateAssets=all` (analyzer-only, not shipped). Suppress individual rules in `.editorconfig` as needed, same pattern as existing Roslynator setup.

### Test Infrastructure

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| `Testcontainers.PostgreSql` | 4.11.0 | Real PostgreSQL in integration tests | Current tests use SQLite in-memory which hides provider-specific bugs. Testcontainers spins up a real PostgreSQL in Docker per test run. The project already tests against PostgreSQL in CI -- this makes it consistent locally too. Use with `IAsyncLifetime` in xUnit fixtures. |
| `Respawn` | 7.0.0 | Fast database reset between tests | Instead of recreating the database per test (slow) or relying on transactions (leaky), Respawn intelligently deletes data in correct FK order. Pair with Testcontainers: one container per test class, Respawn reset per test (~50ms). |

**Confidence:** HIGH -- both are the standard .NET integration testing stack in 2025-2026. Testcontainers 4.11.0 published on NuGet, Respawn 7.0.0 published Nov 2025.

**Do NOT use:**
- `Microsoft.EntityFrameworkCore.InMemory` -- it does not enforce constraints, has no SQL, and masks real bugs. The project correctly uses SQLite in-memory instead, but should add PostgreSQL via Testcontainers for production-parity testing.

**Integration pattern with existing test infrastructure:**
```
SimpleModule.Tests.Shared (add Testcontainers + Respawn here)
  -> PostgreSqlFixture : IAsyncLifetime (starts container once)
  -> SimpleModuleWebApplicationFactory overrides connection string
  -> Respawn checkpoint resets between tests
```

### Database Migration Strategy

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| EF Core Migrations (built-in) | 10.0 | Schema versioning | Already a dependency via EF Core. The project currently uses `EnsureCreatedAsync()` which is a ticking time bomb -- it cannot handle schema changes, has no rollback, and silently diverges between environments. Move to explicit migrations per module DbContext. |

**Confidence:** MEDIUM -- the technology is HIGH confidence, but the migration strategy for a modular monolith with shared database + per-module schemas requires careful design. See ARCHITECTURE.md for the pattern.

**Migration approach for SimpleModule's multi-provider setup:**
- Each module already has its own `ModuleDbContextInfo` with schema/prefix isolation
- Generate migrations per module DbContext: `dotnet ef migrations add Initial --context ProductsDbContext --output-dir Migrations`
- Use `MigrationsHistoryTable("__EFMigrationsHistory", "products")` per module so migration history is schema-isolated
- Keep `EnsureCreatedAsync()` ONLY for test environments (SQLite in-memory where migrations don't apply)
- Apply migrations on startup in development: `context.Database.MigrateAsync()`
- Apply migrations via CLI in production: `dotnet ef database update`

**Do NOT use:**
- `FluentMigrator` or `DbUp` -- introducing a second migration system alongside EF Core creates confusion. EF Core migrations work, they just need to be set up properly.

### Authorization Hardening

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Custom Roslyn Analyzer (build yourself) | N/A | Enforce `[Authorize]` on `[RequirePermission]` endpoints | The concern about `[RequirePermission]` not implying `[Authorize]` is a framework design issue, not a library gap. Two options: (1) make `[RequirePermission]` automatically apply authorization via a convention, or (2) write a custom analyzer that errors when `[RequirePermission]` appears without `[Authorize]`. Option 1 is cleaner -- apply a fallback authorization policy. |

**Confidence:** HIGH -- ASP.NET Core's `FallbackPolicy` and `RequireAuthenticatedUser()` are well-documented patterns.

**Pattern:**
```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```
This makes ALL endpoints require authentication by default. Anonymous endpoints explicitly opt out with `[AllowAnonymous]`. Combined with `[RequirePermission]`, this eliminates the gap.

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| Rate limiting | Built-in `AddRateLimiter` | AspNetCoreRateLimit NuGet | Unnecessary third-party dep; built-in covers all use cases |
| Security analysis | Meziantou.Analyzer | SonarAnalyzer.CSharp | Requires SonarQube infrastructure; heavier than needed |
| Security analysis | Meziantou.Analyzer | SecurityCodeScan | Unmaintained since 2022; doesn't support modern .NET |
| Integration testing | Testcontainers | Docker Compose in CI | Testcontainers is simpler, self-contained, works locally |
| DB reset | Respawn | Transaction rollback | Transactions leak state in EF Core; Respawn is deterministic |
| DB migrations | EF Core Migrations | FluentMigrator | Second migration tool alongside EF Core is confusing |
| DB migrations | EF Core Migrations | DbUp | Same reason; EF Core migrations are already available |

## Installation

### Directory.Build.props (add Meziantou.Analyzer)

```xml
<ItemGroup>
  <PackageReference Include="Roslynator.Analyzers">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
  <PackageReference Include="Meziantou.Analyzer" Version="2.0.260">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### SimpleModule.Tests.Shared.csproj (add test infrastructure)

```bash
dotnet add tests/SimpleModule.Tests.Shared/SimpleModule.Tests.Shared.csproj package Testcontainers.PostgreSql --version 4.11.0
dotnet add tests/SimpleModule.Tests.Shared/SimpleModule.Tests.Shared.csproj package Respawn --version 7.0.0
```

### Host project (rate limiting is built-in, no package needed)

Rate limiting middleware is part of `Microsoft.AspNetCore.App` framework reference -- zero additional packages.

## Version Pinning Notes

| Package | Pin Strategy | Reason |
|---------|-------------|--------|
| Meziantou.Analyzer | Float (latest 2.x) | Analyzer-only, no runtime impact, frequent bug-fix releases |
| Testcontainers.PostgreSql | Pin 4.11.0 | Breaking changes possible between majors |
| Respawn | Pin 7.0.0 | Stable, infrequent releases |

## Sources

- [ASP.NET Core Rate Limiting Middleware (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0) -- HIGH confidence
- [Meziantou.Analyzer on NuGet](https://www.nuget.org/packages/Meziantou.Analyzer/2.0.260) -- HIGH confidence
- [Meziantou.Analyzer GitHub](https://github.com/meziantou/Meziantou.Analyzer) -- HIGH confidence
- [Testcontainers for .NET PostgreSQL](https://dotnet.testcontainers.org/modules/postgres/) -- HIGH confidence
- [Testcontainers.PostgreSql 4.11.0 on NuGet](https://www.nuget.org/packages/Testcontainers.PostgreSql) -- HIGH confidence
- [Respawn 7.0.0 on NuGet](https://www.nuget.org/packages/respawn) -- HIGH confidence
- [Respawn GitHub](https://github.com/jbogard/Respawn) -- HIGH confidence
- [EF Core Migrations with Multiple Providers (Microsoft Learn)](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers) -- HIGH confidence
- [Per-module migrations in modular monolith (Milan Jovanovic)](https://www.milanjovanovic.tech/blog/how-to-keep-your-data-boundaries-intact-in-a-modular-monolith) -- MEDIUM confidence
- [Testcontainers Best Practices (Milan Jovanovic)](https://www.milanjovanovic.tech/blog/testcontainers-best-practices-dotnet-integration-testing) -- MEDIUM confidence
- [Policy-based Authorization (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0) -- HIGH confidence
