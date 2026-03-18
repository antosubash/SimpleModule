# Testing Patterns

**Analysis Date:** 2026-03-18

## .NET Test Framework

**Runner:**
- xUnit v3 (latest)
- Config: None required (convention-based)
- Test discovery: `*.Tests/*.cs` folders, classes suffixed `Tests`, methods marked `[Fact]` or `[Theory]`

**Assertion Library:**
- FluentAssertions (latest)
- Example: `result.Should().Be(expected)`, `products.Should().NotBeEmpty()`, `await act.Should().ThrowAsync<NotFoundException>()`

**Run Commands:**
```bash
dotnet test                                          # Run all tests
dotnet test --filter "FullyQualifiedName~ClassName" # Run single test class
dotnet test --filter "FullyQualifiedName~MethodName"# Run single test method
```

## .NET Test File Organization

**Location:**
- Co-located with source: `modules/<Name>/tests/<Name>.Tests/`
- Unit tests: `Unit/` subfolder
- Integration tests: `Integration/` subfolder

**Naming:**
- Test class: `{ClassBeingTested}Tests.cs`
- Test method: `{MethodUnderTest}_{Scenario}_{ExpectedOutcome}()`
  - Example: `CreateProduct_WithCreatePermission_Returns201()`
  - Test method names may use underscores (CA1707 suppressed in `.editorconfig` for test projects)

**Structure:**
```
modules/
├── Products/
│   └── tests/
│       └── Products.Tests/
│           ├── Products.Tests.csproj
│           ├── Unit/
│           │   ├── ProductIdTests.cs
│           │   ├── ProductServiceTests.cs
│           │   └── CreateRequestValidatorTests.cs
│           └── Integration/
│               ├── ProductsEndpointTests.cs
│               └── ProductsBrowseEndpointTests.cs
```

## .NET Test Structure

**Unit Test Pattern:**
```csharp
public sealed class ProductIdTests
{
    [Fact]
    public void From_WithValidInt_CreatesProductId()
    {
        var id = ProductId.From(42);

        id.Value.Should().Be(42);
    }
}
```

**Integration Test Pattern:**
```csharp
public class ProductsEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ProductsEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllProducts_WithViewPermission_Returns200WithProductList()
    {
        var client = _factory.CreateAuthenticatedClient([ProductsPermissions.View]);

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.Should().NotBeEmpty();
    }
}
```

**Test Structure Pattern (AAA):**
1. **Arrange** — Set up test data, client, or service
2. **Act** — Call the method or endpoint
3. **Assert** — Verify the result using FluentAssertions

## .NET Service/Unit Test Setup

**In-Memory SQLite:**
- Used for unit tests of services that access the database
- Connection kept open during test lifetime
- Database schema created via `EnsureCreated()`

**Example Setup:**
```csharp
public sealed class ProductServiceTests : IDisposable
{
    private readonly ProductsDbContext _db;
    private readonly ProductService _sut; // System Under Test

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<ProductsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["Products"] = "Data Source=:memory:",
                },
            }
        );
        _db = new ProductsDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new ProductService(_db, NullLogger<ProductService>.Instance);
    }

    public void Dispose() => _db.Dispose();
}
```

## .NET Integration Testing

**Web Application Factory:**
- `SimpleModuleWebApplicationFactory` extends `WebApplicationFactory<Program>`
- Location: `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs`
- Injected as xUnit fixture via `IClassFixture<SimpleModuleWebApplicationFactory>`

**Key Features:**
- In-memory SQLite shared across all tests in a factory instance
- Custom test authentication scheme (`TestAuthScheme`) that bypasses OpenIddict
- `CreateAuthenticatedClient(params Claim[] claims)` — creates HTTP client with claims in header
- `CreateAuthenticatedClient(string[] permissions, params Claim[] additionalClaims)` — convenience overload for permission strings
- All db contexts automatically replaced with SQLite

**Test Auth Handler:**
- Reads `X-Test-Claims` header with format: `{Type}={Value};{Type}={Value};...`
- Always adds `ClaimTypes.NameIdentifier` claim if missing (set to "test-user-id")
- Requires `Authorization: Bearer test-token` header to authenticate

**Example:**
```csharp
[Fact]
public async Task GetAllProducts_WithViewPermission_Returns200WithProductList()
{
    var client = _factory.CreateAuthenticatedClient([ProductsPermissions.View]);

    var response = await client.GetAsync("/api/products");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var products = await response.Content.ReadFromJsonAsync<List<Product>>();
    products.Should().NotBeEmpty();
}

[Fact]
public async Task GetAllProducts_WithoutPermission_Returns403()
{
    var client = _factory.CreateAuthenticatedClient([ProductsPermissions.Create]);

    var response = await client.GetAsync("/api/products");

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

## .NET Mocking & Fakes

**Mocking Framework:** None used in core tests

**Fakes:**
- `SimpleModule.Tests.Shared` provides `FakeDataGenerators` using Bogus
- Pre-built fakers for all module DTOs and request types
- Example: `new Faker<CreateProductRequest>()` or use factory methods

**What to Mock:**
- External HTTP services (typically not needed for integration tests due to factory)
- File I/O (if any)
- Expensive operations (typically avoided in tests)

**What NOT to Mock:**
- Database (use in-memory SQLite)
- Dependency Injection services (all registered in test host)
- Authentication (use test auth handler)

## .NET Test Types & Scope

**Unit Tests:**
- Scope: Single class/method in isolation
- Location: `Unit/` subfolder
- Database: In-memory SQLite
- Example: `ProductServiceTests.cs`, `CreateRequestValidatorTests.cs`
- Focus: Business logic validation, error conditions

**Integration Tests:**
- Scope: Full HTTP request → endpoint → service → database
- Location: `Integration/` subfolder
- Database: In-memory SQLite via `SimpleModuleWebApplicationFactory`
- Example: `ProductsEndpointTests.cs`
- Focus: Permission checking, status codes, full flow

**E2E Tests:**
- Framework: Playwright
- See "JavaScript Test Framework" section below
- Scope: Full user workflows across multiple pages

## .NET Test Error Cases

**Pattern for Thrown Exceptions:**
```csharp
[Fact]
public async Task UpdateProductAsync_WithNonExistentId_ThrowsNotFoundException()
{
    var request = new UpdateProductRequest { Name = "Test", Price = 10.00m };

    var act = () => _sut.UpdateProductAsync(ProductId.From(99999), request);

    await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Product*99999*not found*");
}
```

**Pattern for HTTP Error Responses:**
```csharp
[Fact]
public async Task CreateProduct_Unauthenticated_Returns401()
{
    var client = _factory.CreateClient(); // No authentication

    var response = await client.PostAsJsonAsync("/api/products", request);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

## .NET Test Coverage

**Requirements:** No global coverage requirement enforced

**CI Coverage:**
- SQLite: All tests run against in-memory SQLite
- PostgreSQL: Integration tests run against PostgreSQL in CI (separate pipeline)

## JavaScript Test Framework

**Runner:**
- Playwright (latest)
- Config: `tests/e2e/playwright.config.ts`

**Test Location:**
```
tests/
└── e2e/
    ├── playwright.config.ts
    ├── fixtures/
    │   └── base.ts
    ├── pages/
    │   ├── dashboard.page.ts
    │   ├── products/
    │   │   ├── browse.page.ts
    │   │   ├── create.page.ts
    │   │   ├── edit.page.ts
    │   │   └── manage.page.ts
    │   └── orders/
    │       ├── create.page.ts
    │       ├── edit.page.ts
    │       └── list.page.ts
    └── tests/
        ├── flows/
        │   ├── products-crud.spec.ts
        │   ├── orders-crud.spec.ts
        │   └── permissions.spec.ts
        └── smoke/
            ├── dashboard.spec.ts
            ├── orders.spec.ts
            └── products.spec.ts
```

**Run Commands:**
```bash
npm run test:e2e                    # Run all Playwright tests
npm run test:e2e -- --debug        # Run with inspector
npm run test:e2e -- products       # Run matching test file
npm run test:e2e:ui                # Open test UI
```

**Playwright Config:**
- Base URL: `https://localhost:5001`
- Web server: Auto-starts `dotnet run` (reuses if running)
- Browsers: Chromium always, Firefox + Safari in CI only
- Parallel: Full parallel locally, sequential in CI (workers: 1)
- Retries: 0 locally, 2 in CI
- Traces: `on-first-retry` (records on failure)
- Screenshots: `only-on-failure`

## JavaScript Test Structure (Playwright)

**Fixture Pattern:**
```typescript
import { test as base } from '@playwright/test';

export const test = base.extend({
  storageState: async ({}, use) => {
    await use('auth/.auth/user.json');
  },
});

export { expect } from '@playwright/test';
```

**Page Object Pattern:**
```typescript
import type { Page } from '@playwright/test';

export class ProductsBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/products/browse');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /products/i });
  }

  get productCards() {
    return this.page.locator('[data-testid="product-card"]');
  }

  productByName(name: string) {
    return this.page.getByText(name);
  }
}
```

**Test Structure (Flow Test):**
```typescript
import { expect, test } from '../../fixtures/base';
import { ProductsBrowsePage } from '../../pages/products/browse.page';
import { ProductsCreatePage } from '../../pages/products/create.page';
import { ProductsEditPage } from '../../pages/products/edit.page';
import { ProductsManagePage } from '../../pages/products/manage.page';

test.describe('Products CRUD', () => {
  test('create, verify, edit, and delete a product', async ({ page }) => {
    const suffix = Date.now();
    const productName = `E2E Product ${suffix}`;
    const updatedName = `E2E Updated ${suffix}`;

    const createPage = new ProductsCreatePage(page);
    const browsePage = new ProductsBrowsePage(page);
    const managePage = new ProductsManagePage(page);
    const editPage = new ProductsEditPage(page);

    // Create a product
    await createPage.goto();
    await createPage.createProduct(productName, '49.99');

    // Verify it appears on browse page
    await browsePage.goto();
    await expect(browsePage.productByName(productName)).toBeVisible();

    // Edit the product
    await managePage.goto();
    await managePage.editButton(productName).click();
    await expect(editPage.heading).toBeVisible();
    await editPage.updateProduct(updatedName, '59.99');

    // Verify update
    await browsePage.goto();
    await expect(browsePage.productByName(updatedName)).toBeVisible();

    // Delete
    await managePage.goto();
    page.on('dialog', (dialog) => dialog.accept());
    await managePage.deleteButton(updatedName).click();
    await page.waitForLoadState('networkidle');

    // Verify deletion
    await expect(managePage.productRow(updatedName)).not.toBeVisible();
  });
});
```

## JavaScript Test Patterns

**Locator Strategies (in order of preference):**
1. Role-based: `page.getByRole('button', { name: /submit/i })`
2. Label/text: `page.getByLabel('Username')`, `page.getByText('Product Name')`
3. Testid: `page.locator('[data-testid="product-card"]')`
4. Locator chaining: `page.locator('form').getByRole('button')`

**Wait Patterns:**
- `page.waitForLoadState('networkidle')` — after navigation or click with network activity
- `await expect(element).toBeVisible()` — waits up to 30s by default
- Implicit waits in Playwright (no explicit waits needed for visibility)

**Dialog Handling:**
```typescript
page.on('dialog', (dialog) => dialog.accept());
await managePage.deleteButton(productName).click();
```

**User Interactions:**
- Typing: `await input.fill('text')`
- Clicking: `await button.click()`
- Selection: `await select.selectOption('value')`
- Form submission: `await form.press('Enter')` or `await button.click()`

## JavaScript Test Types

**Smoke Tests:**
- Scope: Single page load/basic interaction
- Location: `tests/smoke/`
- Focus: Page renders, no 500 errors
- Example: `dashboard.spec.ts`, `products.spec.ts`

**Flow Tests:**
- Scope: User workflow across multiple pages (CRUD, permissions)
- Location: `tests/flows/`
- Focus: End-to-end user journeys, multi-step interactions
- Examples: `products-crud.spec.ts`, `orders-crud.spec.ts`, `permissions.spec.ts`

## Test Data

**.NET Factories/Builders:**
- Bogus-based fakers in `SimpleModule.Tests.Shared`
- Example: `new Faker<CreateProductRequest>()`
- Used inline in tests or pre-generated in setup

**E2E Test Data:**
- Unique names generated with timestamp: `Date.now()` suffix
- No fixed test IDs (data is ephemeral)
- Example: `const productName = \`E2E Product ${Date.now()}\`;`

## CI/CD Test Execution

**.NET Tests (GitHub Actions):**
- Matrix: SQLite and PostgreSQL
- All tests run in CI: `dotnet test`

**E2E Tests (GitHub Actions):**
- Requires running app: `dotnet run --project template/SimpleModule.Host`
- Playwright runs against multiple browsers in CI
- Artifacts: HTML reports, failure traces, screenshots

---

*Testing analysis: 2026-03-18*
