---
outline: deep
---

# Unit Tests

Unit tests verify individual services, validators, and event handlers in isolation. They use in-memory SQLite for database-backed tests and fake implementations for cross-module dependencies.

## Service Tests

Service tests create a real `DbContext` with an in-memory SQLite connection and test the service directly. Here is the pattern from `CustomerServiceTests`:

```csharp
public sealed class CustomerServiceTests : IDisposable
{
    private readonly CustomersDbContext _db;
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        var options = new DbContextOptionsBuilder<CustomersDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["Customers"] = "Data Source=:memory:",
                },
            }
        );
        _db = new CustomersDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new CustomerService(_db, new TestMessageBus(), NullLogger<CustomerService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateCustomerAsync_CreatesAndReturnsCustomer()
    {
        var request = new CreateCustomerRequest { Name = "Alice", Email = "alice@example.com" };

        var customer = await _sut.CreateCustomerAsync(request);

        customer.Should().NotBeNull();
        customer.Name.Should().Be("Alice");
        customer.Email.Should().Be("alice@example.com");
        customer.Id.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateCustomerAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        var request = new UpdateCustomerRequest { Name = "Test", Email = "test@example.com" };

        var act = () => _sut.UpdateCustomerAsync(CustomerId.From(99999), request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Customer*99999*not found*");
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
        var request = new CreateCustomerRequest { Name = "Alice", Email = "alice@example.com" };

        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new CreateCustomerRequest { Name = "", Email = "alice@example.com" };

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
    public static Faker<Customer> CustomerFaker { get; } =
        new Faker<Customer>()
            .RuleFor(c => c.Id, f => CustomerId.From(f.IndexFaker + 1))
            .RuleFor(c => c.Name, f => f.Person.FullName)
            .RuleFor(c => c.Email, f => f.Internet.Email());

    public static Faker<CreateCustomerRequest> CreateCustomerRequestFaker { get; } =
        new Faker<CreateCustomerRequest>()
            .RuleFor(r => r.Name, f => f.Person.FullName)
            .RuleFor(r => r.Email, f => f.Internet.Email());

    public static Faker<User> UserFaker { get; } =
        new Faker<User>()
            .RuleFor(u => u.Id, f => UserId.From(f.IndexFaker + 1))
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Name, f => f.Person.FullName)
            .RuleFor(u => u.CreatedAt, f => f.Date.Recent());

    // ... fakers for all module DTOs and request types
}
```

Use them in tests to generate realistic test data:

```csharp
var customers = FakeDataGenerators.CustomerFaker.Generate(5);
var request = FakeDataGenerators.CreateCustomerRequestFaker.Generate();
```

## Fake Contract Implementations

For testing code that depends on other modules, the shared project provides fake implementations of contract interfaces. For example, `FakeCustomerContracts` implements `ICustomerContracts` with an in-memory list:

```csharp
public class FakeCustomerContracts : ICustomerContracts
{
    public List<Customer> Customers { get; set; } =
        FakeDataGenerators.CustomerFaker.Generate(3);

    public Task<IEnumerable<Customer>> GetAllCustomersAsync() =>
        Task.FromResult<IEnumerable<Customer>>(Customers);

    public Task<Customer?> GetCustomerByIdAsync(CustomerId id) =>
        Task.FromResult(Customers.FirstOrDefault(c => c.Id == id));

    public Task<Customer> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Id = CustomerId.From(_nextId++),
            Name = request.Name,
            Email = request.Email,
        };
        Customers.Add(customer);
        return Task.FromResult(customer);
    }

    // ... other CRUD methods
}
```

These fakes are useful when a module under test depends on another module's contracts. Rather than spinning up the full dependency, inject the fake:

```csharp
var fakeCustomers = new FakeCustomerContracts();
var service = new InvoiceService(fakeCustomers, db, logger);
```

## Testing Event Handlers

Wolverine handlers are plain classes — instantiate them directly and call `Handle` / `HandleAsync`. To verify a service publishes the right event, substitute `IMessageBus` and assert on the recorded calls:

```csharp
[Fact]
public async Task CreateCustomerAsync_PublishesCustomerCreatedEvent()
{
    var bus = Substitute.For<IMessageBus>();
    var service = new CustomerService(_db, bus, _logger);
    var request = FakeDataGenerators.CreateCustomerRequestFaker.Generate();

    await service.CreateCustomerAsync(request);

    await bus.Received().PublishAsync(Arg.Any<CustomerCreatedEvent>());
}
```

## Next Steps

- [Integration Tests](/testing/integration-tests) -- test HTTP endpoints through the full pipeline
- [E2E Tests](/testing/e2e-tests) -- browser-based testing with Playwright
- [Events](/guide/events) -- handler conventions and delivery semantics
