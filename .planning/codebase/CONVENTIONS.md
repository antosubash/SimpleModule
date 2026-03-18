# Coding Conventions

**Analysis Date:** 2026-03-18

## C# Naming Patterns

**Interfaces:**
- Must start with `I` (error-level enforcement)
- Example: `IEndpoint`, `IEventBus`, `IOrderContracts`

**Public members (methods, properties, events, delegates, classes, structs, enums):**
- PascalCase (error-level enforcement)
- Example: `CreateOrderAsync()`, `GetUserByIdAsync()`, `OrderService`

**Private fields:**
- Start with underscore followed by camelCase: `_camelCase` (error-level enforcement)
- Example: `_db`, `_connection`, `_logger`

**Local variables and parameters:**
- camelCase (error-level enforcement)
- Example: `userId`, `request`, `productList`

**Constants:**
- PascalCase (error-level enforcement)
- Example: `QuantityMustBePositiveFormat`, `RoutePrefix`

**Test methods:**
- Underscores allowed in test method names (CA1707 suppressed)
- Pattern: `Method_Scenario_Expected`
- Example: `CreateOrderAsync_WithValidUserAndProduct_CalculatesCorrectTotal()`

## C# Code Style

**Formatting:**
- Usings outside namespace (error-level enforcement)
- File-scoped namespaces only (error-level enforcement)
- Indent style: space, size 4
- Line ending: CRLF
- Insert final newline: true
- Trim trailing whitespace: true
- NewLine before open brace: all cases
- NewLine before else, catch, finally: true
- Indent case contents and switch labels: true
- No space after cast: `(int)value` not `(int) value`
- Space after keywords in control flow: `if (condition)` not `if(condition)`

**var Preferences:**
- Use `var` for built-in types: `var count = 5;`
- Use `var` when type is apparent: `var order = new Order();`
- Use `var` elsewhere unless ambiguous

**Expression-bodied Members:**
- Methods: only when single line
- Constructors: never (use block body)
- Operators: only when single line
- Properties: use when single line
- Accessors: use when single line
- Lambdas: always

**Pattern Matching:**
- Prefer pattern matching over is/cast: `if (obj is string text)`
- Prefer pattern matching over as/null check

**Null Handling:**
- Use throw expressions: `user ?? throw new NotFoundException()`
- Use conditional delegate calls: `handler?.Invoke()`
- Use null coalescing: `name ?? "default"`
- Use null propagation: `user?.GetEmail()`

**JSON and Configuration Files:**
- Indent size: 2 spaces
- Used for: `.csproj`, `.props`, `.targets`, `.xml`, `.json`, `.yml`, `.yaml`

## Logging

**Framework:** `Microsoft.Extensions.Logging` with source-generated `LoggerMessage` attributes

**Pattern - Partial Methods:**
- Define logging calls as private static partial methods annotated with `[LoggerMessage]`
- Use structured logging parameters, not string interpolation

**Example:**
```csharp
public partial class OrderService(ILogger<OrderService> logger) : IOrderContracts
{
    public async Task<Order?> GetOrderByIdAsync(OrderId id)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            LogOrderNotFound(logger, id);
        }
        return order;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Order {OrderId} not found"
    )]
    private static partial void LogOrderNotFound(ILogger logger, OrderId orderId);
}
```

**When to Log:**
- Information: Service initialization, order creation, permission seeding
- Error: Exception handling, operation failures (passed as exception parameter)
- Warnings: Deprecated feature usage (rare)
- Do not log: expected validation failures, routine data access (unless slow)

**Logger Injection:**
- Always via constructor: `ILogger<ClassName> logger`
- Use NullLogger for test fixtures when actual logging not needed: `NullLogger<Service>.Instance`

## Error Handling

**Custom Exceptions:**
- Located in `SimpleModule.Core.Exceptions`
- Primary use: `NotFoundException` — used when entity not found
- Primary use: `ValidationException` — used for request validation errors
- Application specific: inherit from these core exceptions

**Exception Pattern:**
```csharp
var user = await users.GetUserByIdAsync(request.UserId);
if (user is null)
{
    throw new NotFoundException("User", request.UserId);
}
```

**Event Bus Isolation:**
- Event handler failures collected in `AggregateException` (not swallowed)
- Each handler isolated — one failure doesn't prevent other handlers running
- Located in: `SimpleModule.Core.Events/EventBus.cs`
- Pragmatic suppression: `#pragma warning disable CA1031` with inline comment justifying catch-all

## Comments

**JSDoc/Comments:**
- Keep comments minimal — prefer clear code over comments
- Comment complex validation logic or non-obvious business rules
- No XML documentation for simple CRUD services (unless public API)
- No temporal comments (avoid "recently added", "legacy")

**Suppressed Diagnostics:**
- Comments explain why suppression exists
- Example: `#pragma warning disable CA1031 // Event bus must isolate handler failures`

## Import Organization

**Order (for C# using statements):**
1. System namespace imports
2. Microsoft namespace imports
3. Project namespace imports (SimpleModule.*)
4. Local namespace imports

**Example:**
```csharp
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Validation;
using SimpleModule.Orders.Contracts;
```

## Function Design

**Size Guideline:**
- Aim for 10-40 lines per method (including body only)
- Use helper methods and extracted async patterns when longer
- Prioritize readability over brevity

**Parameters:**
- Max 5-6 parameters before considering a builder or options object
- Constructor injection preferred over method parameters for services
- Request objects for HTTP endpoints: `(CreateOrderRequest request, IOrderContracts service) => ...`

**Return Values:**
- Async methods return `Task<T>` or `Task` (not `void`)
- Query methods may return `IReadOnlyList<T>` or `IEnumerable<T>` (mutable returns only from services)
- Nullable returns when resource may not exist: `Task<Order?>` for get-by-id

## Async/Await

**Pattern:**
- All I/O operations use `async/await`
- Never use `.Result` or `.Wait()`
- Nullable reference types enabled — use `?` for nullable operations
- `CancellationToken cancellationToken = default` for optional cancellation support
- No `.ConfigureAwait(false)` needed in ASP.NET Core (suppressed: RCS1090)

## Module Design

**Exports (Public API):**
- Contract interfaces in `*.Contracts` project (e.g., `IOrderContracts`)
- `[Dto]` request/response types in Contracts
- Entity types NOT exposed (internal to module)
- Never export implementation classes

**Barrel Files:**
- Used in module Pages: `modules/<Name>/src/<Name>/Pages/index.ts` exports pages record
- Not typical for C# (prefer explicit imports)

## Validation

**Pattern - Static Validator Methods:**
- Located in endpoint folder alongside endpoint class
- Named `CreateRequestValidator`, `UpdateRequestValidator`
- Return `ValidationResult` (custom type from `SimpleModule.Core.Validation`)
- Use `ValidationBuilder` fluent API

**Example Location:** `modules/Orders/src/Orders/Endpoints/Orders/CreateRequestValidator.cs`

**Example Usage in Endpoint:**
```csharp
public class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/", (CreateOrderRequest request, IOrderContracts orderContracts) =>
        {
            var validation = CreateRequestValidator.Validate(request);
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }
            return orderContracts.CreateOrderAsync(request);
        });
}
```

## DTO and Entity Design

**DTO Characteristics:**
- Marked with `[Dto]` attribute for source generator discovery
- Nested types allowed (`public record CreateOrderRequest { public record Item { ... } }`)
- Collection setters required for deserialization (CA2227 suppressed)
- Exposed in Contracts, used in API contracts

**Entity Characteristics:**
- Located in module implementation, never in Contracts
- Used within module services only
- Include `IEquatable<T>` for strongly-typed IDs

## Dependency Injection

**Constructor Injection:**
- Services injected via constructor (preferred for clarity)
- Primary pattern for `ILogger<T>`, contract interfaces, `DbContext`

**Handler/Event Patterns:**
- `IEventHandler<T>` implementations auto-discovered and registered by generator
- No manual service registration needed (handled by `AddModules()` extension)

## Naming for Configuration

**Endpoints:**
- Route prefixes in module constants: `OrdersConstants.RoutePrefix = "orders"`
- Field names in validation: `OrdersConstants.Fields.UserId`
- Validation messages: `OrdersConstants.ValidationMessages.UserIdRequired`

**Constants Location:** `modules/<Name>/src/<Name>/Constants.cs`

---

*Convention analysis: 2026-03-18*
