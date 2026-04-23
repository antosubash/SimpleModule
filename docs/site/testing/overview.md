---
outline: deep
---

# Testing Overview

SimpleModule uses a layered testing strategy: unit tests for isolated logic, integration tests for HTTP endpoints with a real application pipeline, and end-to-end tests for browser-based flows.

## Test Stack

| Library | Purpose |
|---------|---------|
| [xUnit.v3](https://xunit.net/) | Test framework |
| [FluentAssertions](https://fluentassertions.com/) | Expressive assertions |
| [Bogus](https://github.com/bchavez/Bogus) | Fake data generation |
| [NSubstitute](https://nsubstitute.github.io/) | Mocking / test doubles (used in select modules, e.g. BackgroundJobs, Settings) |
| [Microsoft.AspNetCore.Mvc.Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests) | In-process integration testing |
| [Playwright](https://playwright.dev/) | Browser-based E2E testing |

## Project Conventions

Test projects follow a consistent naming and location pattern:

| Type | Location | Naming |
|------|----------|--------|
| Module tests | `modules/<Name>/tests/<Name>.Tests/` | `<Name>.Tests.csproj` |
| Shared infrastructure | `tests/SimpleModule.Tests.Shared/` | Fixtures, fakes, generators |
| E2E tests | `tests/e2e/` | Playwright specs |

Each module test project is organized into subdirectories:

```
modules/Products/tests/Products.Tests/
  Unit/
    ProductServiceTests.cs
    CreateRequestValidatorTests.cs
  Integration/
    ProductsEndpointTests.cs
```

## Running Tests

```bash
# Run all .NET tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~ProductServiceTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~CreateProductAsync_CreatesAndReturnsProduct"

# Run E2E tests
npm run test:e2e

# Run E2E tests with UI
npm run test:e2e:ui
```

## Test Naming

Test methods use underscore-separated names following the pattern:

```
Method_Scenario_Expected
```

For example:
- `GetProductByIdAsync_WithExistingId_ReturnsProduct`
- `CreateProduct_WithCreatePermission_Returns201`
- `Validate_WithEmptyName_ReturnsError`

This convention is enforced by `.editorconfig` which suppresses the `CA1707` naming rule in test projects.

## CI Strategy

CI runs tests against two database providers:

- **SQLite in-memory** -- fast, used for all local development and the primary CI pass
- **PostgreSQL** -- used for integration verification in CI to catch provider-specific issues

The `SimpleModuleWebApplicationFactory` automatically uses SQLite in-memory with a shared connection kept open for the lifetime of the test run.

## Benchmarks (BenchmarkDotNet)

Micro-benchmarks measure endpoint latency and JSON serialization performance for every module:

```bash
# Run all benchmarks
dotnet run -c Release --project tests/SimpleModule.Benchmarks

# Run benchmarks for a specific module
dotnet run -c Release --project tests/SimpleModule.Benchmarks -- --filter "*Products*"
```

Benchmarks use `SimpleModuleWebApplicationFactory` with test auth headers for low-overhead measurement of CRUD operations via an in-process TestServer.

## Load Tests (NBomber)

HTTP load tests using real OAuth Bearer tokens acquired via password grant from OpenIddict:

```bash
# Run all 11 scenarios (50 concurrent users, ~5 min)
dotnet test tests/SimpleModule.LoadTests

# Run a single scenario
dotnet test tests/SimpleModule.LoadTests --filter "Products_Crud"
```

Scenarios cover all modules at 50 concurrent copies:

- **CRUD lifecycle** -- Products, Orders, Users, PageBuilder (create, read, update, delete)
- **Read operations** -- Settings, AuditLogs, FileStorage, FeatureFlags
- **Admin operations** -- role create/delete (handles 302 Blazor SSR redirects)
- **Anonymous** -- Marketplace search and browse
- **Mixed Realistic** -- weighted workload (70% reads, 20% creates, 10% updates)

Key infrastructure:
- `LoadTestWebApplicationFactory` with file-based SQLite + WAL mode
- `PasswordGrantTokenHandler` for OpenIddict ROPC grant
- `SqliteBusyTimeoutInterceptor` for concurrent access

## Next Steps

- [Unit tests](./unit-tests) -- testing services, validators, and event handlers in isolation
- [Integration tests](./integration-tests) -- testing HTTP endpoints end-to-end with `WebApplicationFactory`
- [E2E tests](./e2e-tests) -- browser-based testing with Playwright
