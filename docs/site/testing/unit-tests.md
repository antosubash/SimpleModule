---
outline: deep
---

# Unit Tests

Unit tests verify individual services, validators, and event handlers in isolation. They use in-memory SQLite for database-backed tests and fake implementations for cross-module dependencies.

## Service Tests

Service tests create a real `DbContext` with an in-memory SQLite connection and test the service directly. Here is the pattern from `ProductServiceTests`:

```csharp
public sealed class ProductServiceTests : IDisposable
{
    private readonly ProductsDbContext _db;
    private readonly ProductService _sut;

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

    [Fact]
    public async Task CreateProductAsync_CreatesAndReturnsProduct()
    {
        var request = new CreateProductRequest { Name = "Test Widget", Price = 19.99m };

        var product = await _sut.CreateProductAsync(request);

        product.Should().NotBeNull();
        product.Name.Should().Be("Test Widget");
        product.Price.Should().Be(19.99m);
        product.Id.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateProductAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        var request = new UpdateProductRequest { Name = "Test", Price = 10.00m };

        var act = () => _sut.UpdateProductAsync(ProductId.From(99999), request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Product*99999*not found*");
    }
}
```

::: tip Key Pattern
Each test class creates its own in-memory SQLite connection and `DbContext`. The connection is opened in the constructor and the database schema is created with `EnsureCreated()`. The `IDisposable` pattern ensures cleanup.
:::

## Validator Tests

Validators are pure functions that return a validation result. They are straightforward to test:

```csharp
public class CreateRequestValidatorTests
{
    [Fact]
    public void Validate_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateProductRequest { Name = "Widget", Price = 9.99m };

        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new CreateProductRequest { Name = "", Price = 9.99m };

        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
    }
}
```

## Fake Data Generators

The `SimpleModule.Tests.Shared` project provides pre-built Bogus fakers for all module DTOs and request types in `FakeDataGenerators`:

```csharp
public static class FakeDataGenerators
{
    public static Faker<Product> ProductFaker { get; } =
        new Faker<Product>()
            .RuleFor(p => p.Id, f => ProductId.From(f.IndexFaker + 1))
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Price, f => f.Finance.Amount(1, 1000));

    public static Faker<CreateProductRequest> CreateProductRequestFaker { get; } =
        new Faker<CreateProductRequest>()
            .RuleFor(r => r.Name, f => f.Commerce.ProductName())
            .RuleFor(r => r.Price, f => f.Finance.Amount(1, 1000));

    public static Faker<Order> OrderFaker { get; } =
        new Faker<Order>()
            .RuleFor(o => o.Id, f => OrderId.From(f.IndexFaker + 1))
            .RuleFor(o => o.UserId, f => UserId.From(
                f.Random.Int(1, 100).ToString(CultureInfo.InvariantCulture)))
            .RuleFor(o => o.Items, f => OrderItemFaker.Generate(f.Random.Int(1, 3)))
            .RuleFor(o => o.Total, f => f.Finance.Amount(10, 500))
            .RuleFor(o => o.CreatedAt, f => f.Date.Recent());

    // ... fakers for all module DTOs and request types
}
```

Use them in tests to generate realistic test data:

```csharp
var products = FakeDataGenerators.ProductFaker.Generate(5);
var request = FakeDataGenerators.CreateProductRequestFaker.Generate();
```

## Fake Contract Implementations

For testing code that depends on other modules, the shared project provides fake implementations of contract interfaces. For example, `FakeProductContracts` implements `IProductContracts` with an in-memory list:

```csharp
public class FakeProductContracts : IProductContracts
{
    public List<Product> Products { get; set; } =
        FakeDataGenerators.ProductFaker.Generate(3);

    public Task<IEnumerable<Product>> GetAllProductsAsync() =>
        Task.FromResult<IEnumerable<Product>>(Products);

    public Task<Product?> GetProductByIdAsync(ProductId id) =>
        Task.FromResult(Products.FirstOrDefault(p => p.Id == id));

    public Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Id = ProductId.From(_nextId++),
            Name = request.Name,
            Price = request.Price,
        };
        Products.Add(product);
        return Task.FromResult(product);
    }

    // ... other CRUD methods
}
```

These fakes are useful when a module under test depends on another module's contracts. Rather than spinning up the full dependency, inject the fake:

```csharp
var fakeProducts = new FakeProductContracts();
var service = new OrderService(fakeProducts, db, logger);
```

## Testing Event Handlers

Wolverine handlers are plain classes — instantiate them directly and call `Handle` / `HandleAsync`:

```csharp
[Fact]
public async Task Handle_LogsEvent()
{
    var audit = Substitute.For<IAuditContext>();
    var handler = new OrderCreatedAuditHandler(audit);
    var evt = new OrderCreatedEvent(OrderId.From(1), UserId.From(42), 99.99m);

    await handler.Handle(evt, CancellationToken.None);

    await audit.Received().LogAsync("Order created", "1", Arg.Any<CancellationToken>());
}
```

To verify a service publishes the right event, substitute `IMessageBus` and assert on the recorded calls — see [Events](/guide/events#testing-events) for a full example.

## Next Steps

- [Integration Tests](/testing/integration-tests) -- test HTTP endpoints through the full pipeline
- [E2E Tests](/testing/e2e-tests) -- browser-based testing with Playwright
- [Events](/guide/events) -- handler conventions and delivery semantics
