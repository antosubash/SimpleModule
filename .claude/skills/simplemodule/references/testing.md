# Testing Patterns

## Test Stack

- **xUnit.v3** — test framework
- **FluentAssertions** — readable assertions
- **Bogus** — fake data generation
- **Microsoft.AspNetCore.Mvc.Testing** — integration test host
- **SQLite in-memory** — unit tests
- **PostgreSQL** — CI integration tests

## Project Structure

```
modules/{Name}/tests/SimpleModule.{Name}.Tests/
  Unit/
    {Name}ServiceTests.cs
    CreateRequestValidatorTests.cs
    {Name}IdTests.cs
  Integration/
    {Name}EndpointTests.cs
    {Name}ViewEndpointTests.cs
```

## Test Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\SimpleModule.{Name}\SimpleModule.{Name}.csproj" />
    <ProjectReference Include="..\..\src\SimpleModule.{Name}.Contracts\SimpleModule.{Name}.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\tests\SimpleModule.Tests.Shared\SimpleModule.Tests.Shared.csproj" />
  </ItemGroup>
</Project>
```

## Unit Test Pattern (Service with In-Memory SQLite)

```csharp
public class ProductServiceTests : IDisposable
{
    private readonly ProductsDbContext _db;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ProductsDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbOptions = Options.Create(new DatabaseOptions { Provider = "Sqlite" });
        _db = new ProductsDbContext(options, dbOptions);
        _db.Database.EnsureCreated();

        _service = new ProductService(_db, NullLogger<ProductService>.Instance);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsSeedData()
    {
        var products = await _service.GetAllProductsAsync();
        products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateProduct_ReturnsNewProduct()
    {
        var request = new CreateProductRequest { Name = "Test", Price = 9.99m };
        var product = await _service.CreateProductAsync(request);

        product.Name.Should().Be("Test");
        product.Price.Should().Be(9.99m);
    }

    [Fact]
    public async Task DeleteProduct_NonExistent_ThrowsNotFoundException()
    {
        var act = () => _service.DeleteProductAsync(ProductId.From(9999));
        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}
```

## Validator Tests

```csharp
public class CreateRequestValidatorTests
{
    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var request = new CreateProductRequest { Name = "", Price = 10 };
        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsValid()
    {
        var request = new CreateProductRequest { Name = "Widget", Price = 5.99m };
        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
```

## Integration Test Pattern (API Endpoints)

```csharp
public class ProductsEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public ProductsEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithPermission_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(
            new Claim("permission", ProductsPermissions.View));

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithoutPermission_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient(); // no permissions
        var response = await client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

## Test Naming Convention

Use underscores: `Method_Scenario_Expected`

```csharp
GetAllProducts_WithViewPermission_Returns200WithProductList
CreateProduct_EmptyName_Returns400WithValidationErrors
DeleteProduct_NonExistent_ThrowsNotFoundException
```

## Running Tests

```bash
dotnet test                                            # all tests
dotnet test --filter "FullyQualifiedName~ClassName"    # single test class
dotnet test --filter "FullyQualifiedName~MethodName"   # single test method
```

## Test Infrastructure

`SimpleModuleWebApplicationFactory` provides:
- In-memory SQLite database
- Test auth scheme
- `CreateAuthenticatedClient(params Claim[] claims)` — client with claims via `X-Test-Claims` header
- `FakeDataGenerators` — Bogus-based fakers for all module DTOs
