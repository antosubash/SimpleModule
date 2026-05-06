---
outline: deep
---

# Integration Tests

Integration tests verify HTTP endpoints through the full ASP.NET pipeline using `WebApplicationFactory`. They exercise routing, authentication, authorization, model binding, and database access in a single test.

## SimpleModuleWebApplicationFactory

The shared test infrastructure provides `SimpleModuleWebApplicationFactory`, which configures an in-process test server with:

- **In-memory SQLite** -- a shared `SqliteConnection` kept open for the factory lifetime, with all module `DbContext` instances pointing to it
- **Test authentication scheme** -- bypasses OpenIddict validation; claims are passed via the `X-Test-Claims` header
- **Removed hosted services** -- seed services and background workers are stripped out to avoid side effects
- **Environment set to `"Testing"`** -- allows conditional behavior in the application

### Database Setup

Each module's `DbContext` is replaced with one backed by the shared in-memory SQLite connection. The factory calls `EnsureCreated()` on all module databases when the first authenticated client is created:

```csharp
public class SimpleModuleWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.Configure<DatabaseOptions>(opts =>
            {
                opts.DefaultConnection = "Data Source=:memory:";
                opts.Provider = "Sqlite";
            });

            ReplaceDbContext<UsersDbContext>(services);
            ReplaceDbContext<SettingsDbContext>(services);
            // ... all module DbContexts
        });
    }
}
```

## Creating Authenticated Clients

The factory provides two overloads for creating test HTTP clients with authentication:

### With Specific Claims

```csharp
var client = factory.CreateAuthenticatedClient(
    new Claim(ClaimTypes.NameIdentifier, "user-123"),
    new Claim(ClaimTypes.Email, "user@example.com")
);
```

If no `NameIdentifier` claim is provided, a default `"test-user-id"` is added automatically.

### With Permissions

```csharp
var client = factory.CreateAuthenticatedClient(
    [CustomersPermissions.View, CustomersPermissions.Create]
);
```

This overload converts each permission string into a `permission` claim. You can also pass additional claims positionally after the permissions array:

```csharp
var client = factory.CreateAuthenticatedClient(
    [CustomersPermissions.View],
    new Claim(ClaimTypes.NameIdentifier, "custom-user-id")
);
```

### Unauthenticated Client

For testing 401 responses, use the standard `CreateClient()` method without claims:

```csharp
var client = factory.CreateClient();
```

## How Test Auth Works

Claims are serialized into the `X-Test-Claims` header as semicolon-separated `type=value` pairs. The `TestAuthHandler` reads this header and builds a `ClaimsPrincipal`:

```
X-Test-Claims: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier=test-user-id;permission=Customers.View
```

Requests without the `X-Test-Claims` header are treated as unauthenticated.

## Writing Integration Tests

Use `IClassFixture<SimpleModuleWebApplicationFactory>` to share the factory across tests in a class:

```csharp
public class CustomersEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public CustomersEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllCustomers_WithViewPermission_Returns200WithCustomerList()
    {
        var client = _factory.CreateAuthenticatedClient(
            [CustomersPermissions.View]);

        var response = await client.GetAsync("/api/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customers = await response.Content
            .ReadFromJsonAsync<List<Customer>>();
        customers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllCustomers_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllCustomers_WithoutPermission_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient(
            [CustomersPermissions.Create]); // wrong permission

        var response = await client.GetAsync("/api/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

## Common Patterns

### Testing CRUD Operations

```csharp
[Fact]
public async Task CreateCustomer_WithCreatePermission_Returns201()
{
    var client = _factory.CreateAuthenticatedClient(
        [CustomersPermissions.Create]);
    var request = new CreateCustomerRequest
    {
        Name = "Alice",
        Email = "alice@example.com",
    };

    var response = await client.PostAsJsonAsync("/api/customers", request);

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var customer = await response.Content.ReadFromJsonAsync<Customer>();
    customer.Should().NotBeNull();
    customer!.Name.Should().Be("Alice");
}
```

### Testing Not Found

```csharp
[Fact]
public async Task UpdateCustomer_WithNonExistentId_Returns404()
{
    var client = _factory.CreateAuthenticatedClient(
        [CustomersPermissions.Update]);
    var request = new UpdateCustomerRequest
    {
        Name = "Updated",
        Email = "updated@example.com",
    };

    var response = await client.PutAsJsonAsync(
        "/api/customers/99999", request);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

### Testing Delete with Setup

```csharp
[Fact]
public async Task DeleteCustomer_WithExistingId_Returns204()
{
    var client = _factory.CreateAuthenticatedClient([
        CustomersPermissions.Create,
        CustomersPermissions.Delete,
    ]);

    // Create a customer first
    var createRequest = new CreateCustomerRequest
    {
        Name = "ToDelete",
        Email = "todelete@example.com",
    };
    var createResponse = await client.PostAsJsonAsync(
        "/api/customers", createRequest);
    var created = await createResponse.Content
        .ReadFromJsonAsync<Customer>();

    var response = await client.DeleteAsync(
        $"/api/customers/{created!.Id}");

    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
}
```

## PostgreSQL in CI

By default, the factory uses in-memory SQLite. In CI, you can switch to PostgreSQL by setting the `Database__DefaultConnection` environment variable:

```yaml
env:
  Database__DefaultConnection: "Host=localhost;Database=test;Username=postgres;Password=..."
```

The `DatabaseOptions` configuration picks this up automatically, and module `DbContext` instances use the configured provider.

## Next Steps

- [E2E Tests](/testing/e2e-tests) -- browser-based testing with Playwright
- [Permissions](/guide/permissions) -- how to test permission-protected endpoints
- [Deployment](/advanced/deployment) -- CI/CD pipeline configuration
