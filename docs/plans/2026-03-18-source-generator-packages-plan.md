# Source Generator Packages Integration Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Integrate source generator packages to eliminate boilerplate, improve type safety, and reduce hand-written mapping/validation/enum code across the SimpleModule framework.

**Architecture:** Four independent workstreams, ordered by dependency — enums first (no deps), then strongly-typed IDs (foundational), then validation, then CRUD reduction. Each workstream adds a NuGet package to `Directory.Packages.props` and/or introduces framework helpers, then modifies affected modules.

**Tech Stack:** Vogen (strongly-typed IDs), NetEscapades.EnumGenerators (enum utilities), custom framework helpers (validation/CRUD)

---

## Task 1: Add NetEscapades.EnumGenerators for Enum Utilities

Lowest risk, zero dependencies on other tasks. Replaces `Enum.TryParse` and enables fast `ToString`/`IsDefined` on all enums.

**Files:**
- Modify: `Directory.Packages.props` (add package version)
- Modify: `framework/SimpleModule.Database/SimpleModule.Database.csproj` (add package reference)
- Modify: `framework/SimpleModule.Database/DatabaseProvider.cs`
- Modify: `framework/SimpleModule.Database/DatabaseProviderDetector.cs`
- Modify: `framework/SimpleModule.Core/SimpleModule.Core.csproj` (add package reference)
- Modify: `framework/SimpleModule.Core/Menu/MenuSection.cs`
- Modify: `cli/SimpleModule.Cli/SimpleModule.Cli.csproj` (add package reference)
- Modify: `cli/SimpleModule.Cli/Commands/Doctor/Checks/IDoctorCheck.cs`
- Test: existing tests in `tests/SimpleModule.Database.Tests/DatabaseProviderDetectorTests.cs`

**Step 1: Add package version to central package management**

In `Directory.Packages.props`, add under the `<!-- Analyzers -->` section:

```xml
<PackageVersion Include="NetEscapades.EnumGenerators" Version="1.0.0-beta11" />
```

**Step 2: Add package reference to Database project**

In `framework/SimpleModule.Database/SimpleModule.Database.csproj`, add:

```xml
<PackageReference Include="NetEscapades.EnumGenerators" />
```

**Step 3: Annotate DatabaseProvider enum**

```csharp
using NetEscapades.EnumGenerators;

namespace SimpleModule.Database;

[EnumExtensions]
public enum DatabaseProvider
{
    Sqlite,
    PostgreSql,
    SqlServer,
}
```

This generates `DatabaseProviderExtensions` with `ToStringFast()`, `IsDefined()`, `TryParse()`, etc.

**Step 4: Update DatabaseProviderDetector to use generated TryParse**

Replace in `framework/SimpleModule.Database/DatabaseProviderDetector.cs:11-14`:

```csharp
// Before:
Enum.TryParse<DatabaseProvider>(explicitProvider, ignoreCase: true, out var parsed)

// After:
DatabaseProviderExtensions.TryParse(explicitProvider, out var parsed, ignoreCase: true)
```

**Step 5: Add package reference to Core and CLI projects, annotate remaining enums**

In `framework/SimpleModule.Core/Menu/MenuSection.cs`:

```csharp
using NetEscapades.EnumGenerators;

namespace SimpleModule.Core.Menu;

[EnumExtensions]
public enum MenuSection
{
    Navbar,
    UserDropdown,
}
```

In `cli/SimpleModule.Cli/Commands/Doctor/Checks/IDoctorCheck.cs`:

```csharp
using NetEscapades.EnumGenerators;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

[EnumExtensions]
public enum CheckStatus
{
    Pass,
    Warning,
    Fail,
}
```

**Step 6: Run all tests to verify no regressions**

```bash
dotnet test
```

Expected: All tests pass. The generated extensions are drop-in compatible.

**Step 7: Commit**

```bash
git add Directory.Packages.props framework/SimpleModule.Database/ framework/SimpleModule.Core/ cli/SimpleModule.Cli/
git commit -m "feat: add NetEscapades.EnumGenerators for AOT-safe enum utilities"
```

---

## Task 2: Add Vogen for Strongly-Typed Entity IDs

Foundational change — introduces `ProductId`, `OrderId`, `UserId` value objects that prevent mixing up raw `int`/`string` IDs across module boundaries. Generates EF Core value converters, JSON converters, and route parameter binding automatically.

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `framework/SimpleModule.Core/SimpleModule.Core.csproj`
- Create: `framework/SimpleModule.Core/Ids/ProductId.cs`
- Create: `framework/SimpleModule.Core/Ids/OrderId.cs`
- Create: `framework/SimpleModule.Core/Ids/UserId.cs`
- Modify: `modules/Products/src/Products.Contracts/Product.cs`
- Modify: `modules/Products/src/Products.Contracts/IProductContracts.cs`
- Modify: `modules/Products/src/Products.Contracts/Products.Contracts.csproj`
- Modify: `modules/Products/src/Products/ProductService.cs`
- Modify: `modules/Products/src/Products/ProductsDbContext.cs`
- Modify: `modules/Products/src/Products/Endpoints/Products/GetByIdEndpoint.cs`
- Modify: `modules/Products/src/Products/Endpoints/Products/UpdateEndpoint.cs`
- Modify: `modules/Products/src/Products/Endpoints/Products/DeleteEndpoint.cs`
- Modify: `modules/Orders/src/Orders.Contracts/Order.cs`
- Modify: `modules/Orders/src/Orders.Contracts/OrderItem.cs`
- Modify: `modules/Orders/src/Orders.Contracts/IOrderContracts.cs`
- Modify: `modules/Orders/src/Orders.Contracts/CreateOrderRequest.cs`
- Modify: `modules/Orders/src/Orders.Contracts/UpdateOrderRequest.cs`
- Modify: `modules/Orders/src/Orders/OrderService.cs`
- Modify: `modules/Orders/src/Orders/OrdersDbContext.cs`
- Modify: `modules/Orders/src/Orders/Endpoints/Orders/*`
- Modify: `modules/Users/src/Users.Contracts/UserDto.cs`
- Modify: `modules/Users/src/Users.Contracts/IUserContracts.cs`
- Modify: `modules/Users/src/Users/UserService.cs`
- Modify: `tests/SimpleModule.Tests.Shared/Fakes/FakeDataGenerators.cs`
- Modify: all test files that use raw `int`/`string` IDs
- Test: all existing tests

### Step 1: Add Vogen package

In `Directory.Packages.props`, add:

```xml
<PackageVersion Include="Vogen" Version="5.0.3" />
```

In `framework/SimpleModule.Core/SimpleModule.Core.csproj`, add:

```xml
<PackageReference Include="Vogen" />
```

### Step 2: Define strongly-typed IDs

Create `framework/SimpleModule.Core/Ids/ProductId.cs`:

```csharp
using Vogen;

namespace SimpleModule.Core.Ids;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct ProductId;
```

Create `framework/SimpleModule.Core/Ids/OrderId.cs`:

```csharp
using Vogen;

namespace SimpleModule.Core.Ids;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct OrderId;
```

Create `framework/SimpleModule.Core/Ids/UserId.cs`:

```csharp
using Vogen;

namespace SimpleModule.Core.Ids;

[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct UserId
{
    private static Validation Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Validation.Invalid("UserId cannot be empty")
            : Validation.Ok;
}
```

### Step 3: Update Product contracts

In `modules/Products/src/Products.Contracts/Product.cs`:

```csharp
using SimpleModule.Core;
using SimpleModule.Core.Ids;

namespace SimpleModule.Products.Contracts;

[Dto]
public class Product
{
    public ProductId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

In `modules/Products/src/Products.Contracts/IProductContracts.cs`:

```csharp
using SimpleModule.Core.Ids;

namespace SimpleModule.Products.Contracts;

public interface IProductContracts
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(ProductId id);
    Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IEnumerable<ProductId> ids);
    Task<Product> CreateProductAsync(CreateProductRequest request);
    Task<Product> UpdateProductAsync(ProductId id, UpdateProductRequest request);
    Task DeleteProductAsync(ProductId id);
}
```

### Step 4: Update Order contracts

In `modules/Orders/src/Orders.Contracts/Order.cs`:

```csharp
using SimpleModule.Core;
using SimpleModule.Core.Ids;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class Order
{
    public OrderId Id { get; set; }
    public UserId UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

In `modules/Orders/src/Orders.Contracts/OrderItem.cs`:

```csharp
using SimpleModule.Core;
using SimpleModule.Core.Ids;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class OrderItem
{
    public ProductId ProductId { get; set; }
    public int Quantity { get; set; }
}
```

In `modules/Orders/src/Orders.Contracts/CreateOrderRequest.cs` and `UpdateOrderRequest.cs`:

```csharp
using SimpleModule.Core;
using SimpleModule.Core.Ids;

namespace SimpleModule.Orders.Contracts;

[Dto]
public class CreateOrderRequest
{
    public UserId UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
```

### Step 5: Update User contracts

In `modules/Users/src/Users.Contracts/UserDto.cs`:

```csharp
using SimpleModule.Core;
using SimpleModule.Core.Ids;

namespace SimpleModule.Users.Contracts;

[Dto]
public class UserDto
{
    public UserId Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
}
```

In `modules/Users/src/Users.Contracts/IUserContracts.cs`:

```csharp
using SimpleModule.Core.Ids;

namespace SimpleModule.Users.Contracts;

public interface IUserContracts
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(UserId id);
    Task<UserDto?> GetCurrentUserAsync(UserId userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto> UpdateUserAsync(UserId id, UpdateUserRequest request);
    Task DeleteUserAsync(UserId id);
}
```

### Step 6: Update service implementations

Update `ProductService.cs` — change all `int id` parameters to `ProductId id`. The `.Value` property extracts the raw int when needed (e.g., `db.Products.FindAsync(id.Value)`).

Update `OrderService.cs` — change `int id` to `OrderId id`, `request.UserId` is now `UserId` type.

Update `UserService.cs` — change `string id` to `UserId id`. When calling `userManager.FindByIdAsync()`, pass `id.Value`. When creating `UserDto`, wrap with `UserId.From(user.Id)`.

### Step 7: Update EF Core configurations

Register Vogen EF Core converters in each DbContext's `OnModelCreating` or use the generated `RegisterAllIn*` extension on `ModelConfigurationBuilder`. The exact approach depends on Vogen's generated code — check the generated `ConfigureConventions` extension method.

### Step 8: Update endpoints

All endpoint route parameters change from `int id` to `ProductId id` / `OrderId id` / `UserId id`. Vogen generates the route parameter binding automatically via `IParsable<T>`.

### Step 9: Update test fakers

In `tests/SimpleModule.Tests.Shared/Fakes/FakeDataGenerators.cs`:

```csharp
// Before:
.RuleFor(p => p.Id, f => f.IndexFaker + 1)
// After:
.RuleFor(p => p.Id, f => ProductId.From(f.IndexFaker + 1))

// Before:
.RuleFor(o => o.UserId, f => f.Random.Int(1, 100).ToString(CultureInfo.InvariantCulture))
// After:
.RuleFor(o => o.UserId, f => UserId.From(f.Random.Int(1, 100).ToString(CultureInfo.InvariantCulture)))
```

### Step 10: Update all tests with raw ID usage

Update test files that construct entities or pass IDs directly. For example in `ProductServiceTests.cs`:

```csharp
// Before:
var product = await _sut.GetProductByIdAsync(1);
// After:
var product = await _sut.GetProductByIdAsync(ProductId.From(1));
```

### Step 11: Run all tests

```bash
dotnet test
```

Fix any remaining compilation errors — the compiler will catch every place a raw primitive is used where a strongly-typed ID is expected.

### Step 12: Commit

```bash
git add -A
git commit -m "feat: add Vogen strongly-typed IDs (ProductId, OrderId, UserId) for compile-time safety"
```

---

## Task 3: Improve Validation with Source-Generated Pattern

The existing validation pattern (manual `Dictionary<string, string[]>` accumulation) works but is verbose. Rather than adding a third-party validation package (which may conflict with the existing `ValidationResult`/`ValidationException` pattern), we improve the existing pattern by:
1. Adding a fluent `ValidationBuilder` helper to Core
2. Adding validation to Products module (currently has none)
3. Making the Orders validation cleaner

This is a code quality improvement, not a package integration — keeping the validation approach consistent with the project's "no external dependencies unless source-generated" philosophy.

**Files:**
- Create: `framework/SimpleModule.Core/Validation/ValidationBuilder.cs`
- Modify: `modules/Orders/src/Orders/Endpoints/Orders/CreateRequestValidator.cs`
- Modify: `modules/Orders/src/Orders/OrdersConstants.cs`
- Create: `modules/Products/src/Products/Endpoints/Products/CreateRequestValidator.cs`
- Create: `modules/Products/src/Products/ProductsConstants.cs` (add validation messages if not present)
- Modify: `modules/Products/src/Products/Endpoints/Products/CreateEndpoint.cs`
- Modify: `modules/Products/src/Products/Endpoints/Products/UpdateEndpoint.cs`
- Create: `modules/Products/tests/Products.Tests/Unit/CreateRequestValidatorTests.cs`
- Test: `modules/Orders/tests/Orders.Tests/Unit/CreateOrderRequestValidatorTests.cs`

### Step 1: Create ValidationBuilder helper

Create `framework/SimpleModule.Core/Validation/ValidationBuilder.cs`:

```csharp
namespace SimpleModule.Core.Validation;

public sealed class ValidationBuilder
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public ValidationBuilder AddErrorIf(bool condition, string field, string message)
    {
        if (condition)
        {
            if (!_errors.TryGetValue(field, out var list))
            {
                list = [];
                _errors[field] = list;
            }

            list.Add(message);
        }

        return this;
    }

    public ValidationResult Build()
    {
        if (_errors.Count == 0)
        {
            return ValidationResult.Success;
        }

        var errors = _errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        return ValidationResult.WithErrors(errors);
    }
}
```

### Step 2: Write test for ValidationBuilder

Create `tests/SimpleModule.Core.Tests/Validation/ValidationBuilderTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Core.Validation;

namespace SimpleModule.Core.Tests.Validation;

public class ValidationBuilderTests
{
    [Fact]
    public void Build_WithNoErrors_ReturnsSuccess()
    {
        var result = new ValidationBuilder().Build();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void AddErrorIf_WhenConditionTrue_AddsError()
    {
        var result = new ValidationBuilder()
            .AddErrorIf(true, "Name", "Name is required.")
            .Build();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
        result.Errors["Name"].Should().Contain("Name is required.");
    }

    [Fact]
    public void AddErrorIf_WhenConditionFalse_DoesNotAddError()
    {
        var result = new ValidationBuilder()
            .AddErrorIf(false, "Name", "Name is required.")
            .Build();

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddErrorIf_MultipleSameField_AccumulatesErrors()
    {
        var result = new ValidationBuilder()
            .AddErrorIf(true, "Name", "Name is required.")
            .AddErrorIf(true, "Name", "Name must be at least 2 characters.")
            .Build();

        result.Errors["Name"].Should().HaveCount(2);
    }
}
```

### Step 3: Run test to verify it fails, then implement, then run again

```bash
dotnet test --filter "FullyQualifiedName~ValidationBuilderTests"
```

### Step 4: Simplify Orders CreateRequestValidator

```csharp
using System.Globalization;
using System.Text;
using SimpleModule.Core.Validation;
using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders.Endpoints.Orders;

public static class CreateRequestValidator
{
    private static readonly CompositeFormat QuantityMustBePositiveFormat = CompositeFormat.Parse(
        OrdersConstants.ValidationMessages.QuantityMustBePositiveFormat
    );

    public static ValidationResult Validate(CreateOrderRequest request)
    {
        var builder = new ValidationBuilder()
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.UserId.Value),
                OrdersConstants.Fields.UserId,
                OrdersConstants.ValidationMessages.UserIdRequired
            )
            .AddErrorIf(
                request.Items is null || request.Items.Count == 0,
                OrdersConstants.Fields.Items,
                OrdersConstants.ValidationMessages.AtLeastOneItemRequired
            );

        if (request.Items is { Count: > 0 })
        {
            for (var i = 0; i < request.Items.Count; i++)
            {
                builder.AddErrorIf(
                    request.Items[i].Quantity <= 0,
                    OrdersConstants.Fields.Items,
                    string.Format(CultureInfo.InvariantCulture, QuantityMustBePositiveFormat, i)
                );
            }
        }

        return builder.Build();
    }
}
```

### Step 5: Add Products validation

Create `modules/Products/src/Products/Endpoints/Products/CreateRequestValidator.cs`:

```csharp
using SimpleModule.Core.Validation;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public static class CreateRequestValidator
{
    public static ValidationResult Validate(CreateProductRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.Name),
                "Name",
                "Product name is required."
            )
            .AddErrorIf(
                request.Price <= 0,
                "Price",
                "Price must be greater than zero."
            )
            .Build();
}
```

### Step 6: Wire validation into Products endpoints

In `modules/Products/src/Products/Endpoints/Products/CreateEndpoint.cs`, add validation before calling service:

```csharp
var validation = CreateRequestValidator.Validate(request);
if (!validation.IsValid)
{
    throw new ValidationException(validation.Errors);
}
```

Same for `UpdateEndpoint.cs` (validate the `UpdateProductRequest`).

### Step 7: Write tests for Products validator

Create `modules/Products/tests/Products.Tests/Unit/CreateRequestValidatorTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Products.Contracts;
using SimpleModule.Products.Endpoints.Products;

namespace Products.Tests.Unit;

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

    [Fact]
    public void Validate_WithZeroPrice_ReturnsError()
    {
        var request = new CreateProductRequest { Name = "Widget", Price = 0 };

        var result = CreateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Price");
    }
}
```

### Step 8: Run all tests

```bash
dotnet test
```

### Step 9: Commit

```bash
git add framework/SimpleModule.Core/Validation/ modules/Products/ modules/Orders/ tests/
git commit -m "feat: add ValidationBuilder helper, add Products validation, simplify Orders validation"
```

---

## Task 4: Reduce CRUD Endpoint Boilerplate via Shared Base Patterns

Rather than adding a package, this task creates reusable base patterns that reduce the repetitive CRUD endpoint code. Every module currently has nearly identical GetAll/GetById/Create/Update/Delete endpoints. We introduce generic endpoint helper methods in Core that modules can opt into.

**Important design decision:** We keep the `IEndpoint` interface — each module still has its own endpoint classes. But we extract the common handler logic into static helper methods that reduce the lambda boilerplate.

**Files:**
- Create: `framework/SimpleModule.Core/Endpoints/CrudEndpoints.cs`
- Modify: `modules/Products/src/Products/Endpoints/Products/GetAllEndpoint.cs`
- Modify: `modules/Products/src/Products/Endpoints/Products/GetByIdEndpoint.cs`
- Modify: `modules/Products/src/Products/Endpoints/Products/CreateEndpoint.cs`
- Modify: `modules/Products/src/Products/Endpoints/Products/UpdateEndpoint.cs`
- Modify: `modules/Products/src/Products/Endpoints/Products/DeleteEndpoint.cs`
- Test: existing integration tests in `modules/Products/tests/Products.Tests/Integration/`

### Step 1: Create CrudEndpoints helper

Create `framework/SimpleModule.Core/Endpoints/CrudEndpoints.cs`:

```csharp
using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Endpoints;

public static class CrudEndpoints
{
    public static async Task<IResult> GetAll<T>(Func<Task<IEnumerable<T>>> getAll) =>
        TypedResults.Ok(await getAll());

    public static async Task<IResult> GetById<T>(Func<Task<T?>> getById) where T : class
    {
        var entity = await getById();
        return entity is not null ? TypedResults.Ok(entity) : TypedResults.NotFound();
    }

    public static async Task<IResult> Create<T>(
        Func<Task<T>> create,
        Func<T, string> locationFactory
    )
    {
        var entity = await create();
        return TypedResults.Created(locationFactory(entity), entity);
    }

    public static async Task<IResult> Update<T>(Func<Task<T>> update) where T : class =>
        TypedResults.Ok(await update());

    public static async Task<IResult> Delete(Func<Task> delete)
    {
        await delete();
        return TypedResults.NoContent();
    }
}
```

### Step 2: Simplify Products endpoints

In `modules/Products/src/Products/Endpoints/Products/GetAllEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Endpoints;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products.Endpoints.Products;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", (IProductContracts products) =>
            CrudEndpoints.GetAll(products.GetAllProductsAsync));
}
```

In `GetByIdEndpoint.cs`:

```csharp
public class GetByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{id}", (ProductId id, IProductContracts products) =>
            CrudEndpoints.GetById(() => products.GetProductByIdAsync(id)));
}
```

In `CreateEndpoint.cs`:

```csharp
public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", (CreateProductRequest request, IProductContracts products) =>
        {
            var validation = CreateRequestValidator.Validate(request);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            return CrudEndpoints.Create(
                () => products.CreateProductAsync(request),
                p => $"{ProductsConstants.RoutePrefix}/{p.Id}");
        });
}
```

In `UpdateEndpoint.cs`:

```csharp
public class UpdateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}", (ProductId id, UpdateProductRequest request, IProductContracts products) =>
            CrudEndpoints.Update(() => products.UpdateProductAsync(id, request)));
}
```

In `DeleteEndpoint.cs`:

```csharp
public class DeleteEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/{id}", (ProductId id, IProductContracts products) =>
            CrudEndpoints.Delete(() => products.DeleteProductAsync(id)));
}
```

### Step 3: Run integration tests

```bash
dotnet test --filter "FullyQualifiedName~Products.Tests.Integration"
```

### Step 4: Apply same pattern to Orders endpoints

Repeat the same simplification for Orders endpoints. The Orders `CreateEndpoint` and `UpdateEndpoint` keep their validation logic before calling `CrudEndpoints.Create`/`CrudEndpoints.Update`.

### Step 5: Run all tests

```bash
dotnet test
```

### Step 6: Commit

```bash
git add framework/SimpleModule.Core/Endpoints/ modules/Products/ modules/Orders/
git commit -m "feat: add CrudEndpoints helper to reduce endpoint boilerplate"
```

---

## Execution Order Summary

| Task | Depends On | Risk | Impact |
|------|-----------|------|--------|
| 1. EnumGenerators | None | Low | Low — perf improvement |
| 2. Vogen IDs | None | **High** — touches all modules | **High** — compile-time safety |
| 3. Validation | Task 2 (UserId type) | Low | Medium — consistency |
| 4. CRUD helpers | Task 2 | Low | Medium — less boilerplate |

Task 1 can run in parallel with Task 2. Tasks 3-4 should run sequentially after Task 2.

## Rollback Strategy

Each task is an independent commit. If any task causes issues:
1. `git revert <commit-hash>` for that specific task
2. Tasks 3-4 can be reverted independently of Task 2 (they build on it but don't break the IDs if removed)
3. Task 2 (Vogen) is the riskiest — if reverted, Tasks 3-4 that reference the ID types would also need adjustment
