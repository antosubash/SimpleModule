# Testing Patterns

**Analysis Date:** 2026-03-18

## Test Framework

**Test Runner:**
- xUnit.v3 ([https://xunit.net](https://xunit.net))
- Supports nullable reference types and async patterns natively

**Assertion Library:**
- FluentAssertions (BDD-style assertions)
- Better readability: `order.Total.Should().Be(75.00m)` vs `Assert.Equal(75.00m, order.Total)`

**Mocking Framework:**
- NSubstitute (simpler API than Moq)
- Useful for unit tests isolating services

**Database Testing:**
- Microsoft.EntityFrameworkCore.Sqlite (in-memory for unit tests)
- SQLite in-memory connection reused across test lifetime
- PostgreSQL/SQL Server used in CI integration test pipeline

**Run Commands:**
```bash
dotnet test                                           # Run all tests
dotnet test --filter "FullyQualifiedName~ClassName"  # Single test class
dotnet test --filter "FullyQualifiedName~MethodName" # Single test method
```

## Test Project Structure

**Organization by Type:**

- Unit tests: `modules/<Name>/tests/<Name>.Tests/Unit/`
- Integration tests: `modules/<Name>/tests/<Name>.Tests/Integration/`
- Shared fixtures: `tests/SimpleModule.Tests.Shared/` (single source of truth)

**File Naming:**
- `{ClassName}Tests.cs` for test classes
- Unit tests: `modules/Orders/tests/Orders.Tests/Unit/OrderServiceTests.cs`
- Integration tests: `modules/Orders/tests/Orders.Tests/Integration/OrdersEndpointTests.cs`

**Example Directory Structure:**
```
modules/Orders/tests/Orders.Tests/
├── Unit/
│   ├── OrderServiceTests.cs
│   ├── OrderIdTests.cs
│   └── CreateOrderRequestValidatorTests.cs
├── Integration/
│   └── OrdersEndpointTests.cs
└── Orders.Tests.csproj
```

## Test Class Structure

**xUnit Pattern with Class Fixtures:**

```csharp
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Orders.Tests.Integration;

public class OrdersEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdersEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MethodName_Scenario_Expected()
    {
        // Arrange
        var request = new CreateOrderRequest { ... };

        // Act
        var response = await _client.PostAsJsonAsync("/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

**Fixture Pattern:**
- Class implements `IClassFixture<TFixture>`
- Fixture injected via constructor
- Fixture instance created once per test class (reused across tests)
- Perfect for shared `HttpClient` or database setup

**IDisposable Pattern (Unit Tests with DbContext):**

```csharp
public sealed class OrderServiceTests : IDisposable
{
    private readonly OrdersDbContext _db;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new OrdersDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task TestMethod() { ... }
}
```

## Test Method Naming

**Pattern:** `{MethodName}_{Scenario}_{Expected}`

**Examples:**
- `CreateOrderAsync_WithValidUserAndProduct_CalculatesCorrectTotal()`
- `CreateOrderAsync_WithInvalidUser_ThrowsNotFoundException()`
- `Browse_ReturnsHtmlPage()`
- `Browse_WithInertia_ReturnsProductsData()`
- `GetOrderByIdAsync_ReturnsMatchingOrder()`
- `DeleteOrderAsync_WithNonExistentOrder_ThrowsNotFoundException()`

**Allowed Characters:**
- Underscores in test names allowed (CA1707 suppressed in test projects)

## Unit Test Structure

**Setup Pattern:**
- Arrange: Create test data, setup mocks
- Act: Execute the method under test
- Assert: Verify results using FluentAssertions

**Example - Service with Dependencies:**

```csharp
public sealed class OrderServiceTests : IDisposable
{
    private readonly OrdersDbContext _db;
    private readonly IUserContracts _users = Substitute.For<IUserContracts>();
    private readonly IProductContracts _products = Substitute.For<IProductContracts>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly OrderService _sut;  // System Under Test

    public OrderServiceTests()
    {
        // DbContext setup...
        _sut = new OrderService(_db, _users, _products, _eventBus, NullLogger<OrderService>.Instance);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidUserAndProduct_CalculatesCorrectTotal()
    {
        // Arrange
        _users
            .GetUserByIdAsync(UserId.From("1"))
            .Returns(new UserDto { Id = UserId.From("1"), DisplayName = "Test" });

        var widget = new Product
        {
            Id = ProductId.From(1),
            Name = "Widget",
            Price = 25.00m,
        };
        _products
            .GetProductsByIdsAsync(Arg.Any<IEnumerable<ProductId>>())
            .Returns(callInfo =>
            {
                var ids = callInfo.Arg<IEnumerable<ProductId>>().ToHashSet();
                return new List<Product> { widget }
                    .Where(p => ids.Contains(p.Id))
                    .ToList() as IReadOnlyList<Product>;
            });

        var request = new CreateOrderRequest
        {
            UserId = UserId.From("1"),
            Items = [new OrderItem { ProductId = ProductId.From(1), Quantity = 3 }],
        };

        // Act
        var order = await _sut.CreateOrderAsync(request);

        // Assert
        order.Total.Should().Be(75.00m);
        order.UserId.Should().Be(UserId.From("1"));
        order.Items.Should().HaveCount(1);
    }
}
```

## Integration Tests

**Setup Pattern:**
- Use `SimpleModuleWebApplicationFactory` fixture for full application context
- Create client via `factory.CreateClient()`
- Optional: Create authenticated client with claims via `factory.CreateAuthenticatedClient(params Claim[] claims)`

**Example - Endpoint Testing:**

```csharp
public class ProductsBrowseEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsBrowseEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Browse_ReturnsHtmlPage()
    {
        // Act
        var response = await _client.GetAsync("/products/browse");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task Browse_WithInertia_ReturnsProductsData()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-Inertia", "true");
        _client.DefaultRequestHeaders.Add("X-Inertia-Version", "1");

        // Act
        var response = await _client.GetAsync("/products/browse");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("component").GetString().Should().Be("Products/Browse");
    }
}
```

## Mocking

**Framework:** NSubstitute

**Pattern - Service Mocks:**
```csharp
private readonly IUserContracts _users = Substitute.For<IUserContracts>();
private readonly IProductContracts _products = Substitute.For<IProductContracts>();
```

**What to Mock:**
- External service dependencies (contract interfaces)
- Any `IUserContracts`, `IProductContracts`, `IOrderContracts` in cross-module tests
- `IEventBus` when testing event publishing behavior

**What NOT to Mock:**
- DbContext (use real in-memory SQLite for isolation and correctness)
- `ILogger<T>` (use `NullLogger<T>.Instance` instead)
- Core framework types like `IEndpoint`, `IModule`

**Complex Return Setup:**

```csharp
_products
    .GetProductsByIdsAsync(Arg.Any<IEnumerable<ProductId>>())
    .Returns(callInfo =>
    {
        var ids = callInfo.Arg<IEnumerable<ProductId>>().ToHashSet();
        return new List<Product> { widget }
            .Where(p => ids.Contains(p.Id))
            .ToList() as IReadOnlyList<Product>;
    });
```

## Fixtures and Factories

**Test Data Generation:**

Location: `tests/SimpleModule.Tests.Shared/Fakes/FakeDataGenerators.cs`

Uses Bogus for realistic test data:

```csharp
public static Faker<Order> OrderFaker { get; } =
    new Faker<Order>()
        .RuleFor(o => o.Id, f => OrderId.From(f.IndexFaker + 1))
        .RuleFor(o => o.UserId, f => UserId.From(f.Random.Int(1, 100).ToString()))
        .RuleFor(o => o.Items, f => OrderItemFaker.Generate(f.Random.Int(1, 3)))
        .RuleFor(o => o.Total, f => f.Finance.Amount(10, 500))
        .RuleFor(o => o.CreatedAt, f => f.Date.Recent());

public static Faker<CreateOrderRequest> CreateOrderRequestFaker { get; } =
    new Faker<CreateOrderRequest>()
        .RuleFor(r => r.UserId, f => UserId.From(f.Random.Int(1, 100).ToString()))
        .RuleFor(r => r.Items, f => OrderItemFaker.Generate(f.Random.Int(1, 3)));
```

**Usage in Tests:**
```csharp
var order = FakeDataGenerators.OrderFaker.Generate();
var orders = FakeDataGenerators.OrderFaker.Generate(5);  // Generate 5 orders
```

## Web Application Factory

**Location:** `tests/SimpleModule.Tests.Shared/Fixtures/SimpleModuleWebApplicationFactory.cs`

**Purpose:**
- Provides a real application instance for integration testing
- Replaces all DbContexts with in-memory SQLite
- Sets up test authentication scheme (bypasses OpenIddict)
- Manages shared in-memory database connection

**Features:**
- Inherits from `WebApplicationFactory<Program>`
- Creates real `HttpClient` for endpoint testing
- Supports authenticated clients with custom claims

**Authenticated Client Creation:**
```csharp
// With explicit claims
var client = factory.CreateAuthenticatedClient(
    new Claim(ClaimTypes.NameIdentifier, "user-123"),
    new Claim("permission", "Orders.Create")
);

// With permission array
var client = factory.CreateAuthenticatedClient(
    new[] { "Orders.Create", "Orders.Edit" },
    new Claim(ClaimTypes.Name, "John Doe")
);
```

**Test Authentication Scheme:**
- Scheme name: `TestScheme`
- Reads claims from `X-Test-Claims` header (format: `Type=Value;Type=Value;...`)
- Bypasses actual OpenIddict validation
- Only authenticates if `Authorization: Bearer` header present

## Error Testing

**Exception Assertions:**

```csharp
[Fact]
public async Task CreateOrderAsync_WithInvalidUser_ThrowsNotFoundException()
{
    _users.GetUserByIdAsync(UserId.From("999")).Returns((UserDto?)null);

    var request = new CreateOrderRequest
    {
        UserId = UserId.From("999"),
        Items = [new OrderItem { ProductId = ProductId.From(1), Quantity = 1 }],
    };

    var act = () => _sut.CreateOrderAsync(request);

    await act.Should()
        .ThrowAsync<NotFoundException>()
        .WithMessage("*User*999*not found*");
}
```

**Pattern:**
- Use `() => method()` to defer execution
- Assert async exceptions with `.Should().ThrowAsync<T>()`
- Use wildcard patterns `*` in message assertions for flexible matching

## Async Testing

**Pattern - Async Methods:**
```csharp
[Fact]
public async Task GetOrderByIdAsync_ReturnsMatchingOrder()
{
    // Arrange
    var created = await _sut.CreateOrderAsync(request);

    // Act
    var found = await _sut.GetOrderByIdAsync(created.Id);

    // Assert
    found.Should().NotBeNull();
    found!.Id.Should().Be(created.Id);
}
```

**Rules:**
- Test methods are `async Task` (not `async void`)
- Use `await` for all async operations
- No `.Result` or `.Wait()` blocking

## Strongly-Typed ID Testing

**Pattern - Value Object Tests:**

```csharp
public sealed class OrderIdTests
{
    [Fact]
    public void From_WithValidInt_CreatesOrderId()
    {
        var id = OrderId.From(42);

        id.Value.Should().Be(42);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var id1 = OrderId.From(7);
        var id2 = OrderId.From(7);

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var id1 = OrderId.From(1);
        var id2 = OrderId.From(2);

        id1.Should().NotBe(id2);
    }
}
```

## Test Project Configuration

**Example .csproj Structure:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
    <PackageReference Include="NSubstitute" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Orders\Orders.csproj" />
    <ProjectReference Include="..\..\..\..\tests\SimpleModule.Tests.Shared\SimpleModule.Tests.Shared.csproj" />
  </ItemGroup>
</Project>
```

## Suppressed Diagnostics in Tests

**Test Project .editorconfig Rules:**

- **CA1707** — underscores in test method names (standard convention)
- **CA1812** — internal class never instantiated (test helper classes)
- **CA2234** — use Uri overload (test HTTP clients use string paths)
- **xUnit1051** — use CancellationToken (over-engineering for test methods)

## Coverage

**Requirements:** No enforced coverage target

**View Coverage:**
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

(Coverage tools configured but not mandatory — prioritize test quality over metrics)

---

*Testing analysis: 2026-03-18*
