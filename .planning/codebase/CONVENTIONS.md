# Coding Conventions

**Analysis Date:** 2026-03-18

## C# Naming Patterns

**Files:**
- Endpoints: `{ActionName}Endpoint.cs` (e.g., `CreateEndpoint.cs`, `GetAllEndpoint.cs`)
- Validators: `{Name}Validator.cs` (e.g., `CreateRequestValidator.cs`)
- Services: `{DomainName}Service.cs` (e.g., `ProductService.cs`)
- Modules: `{ModuleName}Module.cs`
- DbContexts: `{ModuleName}DbContext.cs`
- Tests: `{ClassName}Tests.cs` for unit/integration tests
- Test fixtures: `{FeatureName}Fixture.cs` or `{FactoryName}Factory.cs`

**Functions:**
- Public methods: `PascalCase` with async methods suffixed `Async` (e.g., `GetProductByIdAsync()`)
- Private methods: `PascalCase` (e.g., `LogProductNotFound()`)
- Local functions: `camelCase`
- Unit test methods: `MethodUnderTest_Scenario_ExpectedOutcome` (e.g., `GetProductById_WithValidId_ReturnsProduct()`)

**Variables:**
- Public properties: `PascalCase`
- Private fields: `_camelCase` prefix (e.g., `_factory`, `_db`)
- Local variables: `camelCase`
- Parameters: `camelCase`

**Types:**
- Interfaces: `IPascalCase` prefix required (enforced error-level in `.editorconfig`)
- Classes: `PascalCase`
- Records: `PascalCase`
- Constants: `PascalCase` (enforced as const fields)
- Enums: `PascalCase`

## C# Code Style

**Formatting:**
- Indentation: 4 spaces
- Line endings: CRLF
- New lines before open braces for all constructs
- File-scoped namespaces: `namespace SimpleModule.Products;` (error-level requirement)
- Using directives: Must be outside namespace (error-level requirement)

**Linting:**
- Tool: .NET analyzers via `.editorconfig` with `AnalysisLevel=latest-all` and `AnalysisMode=All`
- `TreatWarningsAsErrors` enabled globally via `Directory.Build.props`
- Suppressed rules listed in `.editorconfig`:
  - `IDE0058` — expression value never used (noisy in tests/handlers)
  - `IDE0130` — namespace/folder mismatch
  - `CA1062` — parameter validation (handled by nullable refs)
  - `CA1848` — LoggerMessage delegates (avoid over-engineering)
  - `CA1034` — nested types (DTO pattern)
  - `CA2234` — Uri overload (test clients use strings)
  - `xUnit1051` — CancellationToken in tests (not required for tests)

**Code Style Preferences:**
- `var` preferred when type is apparent or obvious
- Expression-bodied members for single-line methods/properties
- Pattern matching over casts and null checks
- Null coalescing `??` and null propagation `?.`
- Conditional delegate invocation
- Throw expressions

**Example:**
```csharp
using FluentAssertions;
using SimpleModule.Products.Contracts;

namespace Products.Tests.Unit;

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

## C# Import Organization

**Order:**
1. System namespaces (`using System;`)
2. System.* namespaces (`using System.Collections.Generic;`)
3. Microsoft namespaces (`using Microsoft.AspNetCore.Builder;`)
4. Third-party namespaces (`using FluentAssertions;`)
5. Project namespaces (`using SimpleModule.Products;`)

**Path Aliases:**
Not used in this codebase.

## C# Error Handling

**Custom Exceptions:**
- `NotFoundException(string entityName, object id)` — thrown when entity not found. Format: `"{entityName} with ID {id} not found"`
- `ValidationException(Dictionary<string, string[]> errors)` — thrown for validation failures with error dictionary keyed by property name
- `ConflictException` — thrown for resource conflicts
- Thrown as `throw new NotFoundException("Product", id);` or `throw new ValidationException(validation.Errors);`

**Global Exception Handler:**
- `GlobalExceptionHandler` implements `IExceptionHandler` in `SimpleModule.Core.Exceptions`
- Maps exceptions to HTTP status codes:
  - `ValidationException` → 400 Bad Request (includes error dict in response)
  - `NotFoundException` → 404 Not Found
  - `ConflictException` → 409 Conflict
  - Unhandled exceptions → 500 Internal Server Error
- Logs warnings for handled exceptions, errors for unhandled ones
- Returns `ProblemDetails` with status, title, and detail fields

**Pattern:**
```csharp
public async Task<Product> UpdateProductAsync(ProductId id, UpdateProductRequest request)
{
    var product = await db.Products.FindAsync(id);
    if (product is null)
    {
        throw new NotFoundException("Product", id);
    }
    product.Name = request.Name;
    await db.SaveChangesAsync();
    return product;
}
```

## C# Logging

**Framework:** .NET structured logging with `ILogger<T>`

**Patterns:**
- Use `[LoggerMessage]` attribute for performance (zero-allocation logging)
- Private static partial methods only
- Include log level and message template

**Example:**
```csharp
[LoggerMessage(
    Level = LogLevel.Information,
    Message = "Product {ProductId} created: {ProductName}"
)]
private static partial void LogProductCreated(
    ILogger logger,
    ProductId productId,
    string productName
);
```

**When to Log:**
- Info: Successful operations (create, update, delete)
- Warning: Non-fatal issues (entity not found, missing data)
- Error: Unhandled exceptions only (handled in GlobalExceptionHandler)

## C# Comments

**When to Comment:**
- Only for "why" decisions, not "what" (code is self-documenting)
- Complex business logic requiring context
- Workarounds and non-obvious solutions

**XML Documentation:**
Not systematically applied. Used sparingly on public contracts.

## C# Function Design

**Size:** Prefer small functions (10-20 lines typical)

**Parameters:**
- Prefer explicit parameters over injecting entire config objects
- DI services auto-injected in minimal API handlers
- Use `params` sparingly

**Return Values:**
- Nullable types (`Product?`) for queries that might not find data
- Non-nullable (`Product`) for operations guaranteed to return data or throw
- `IEnumerable<T>` for lazy collections, `IReadOnlyList<T>` for eagerly-loaded

**Example:**
```csharp
public async Task<Product?> GetProductByIdAsync(ProductId id) =>
    await db.Products.FindAsync(id);

public async Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IEnumerable<ProductId> ids)
{
    var idList = ids.ToList();
    return await db.Products.Where(p => idList.Contains(p.Id)).ToListAsync();
}
```

## C# Module Design

**Exports:**
- Module class implements `IModule` (single public type per module)
- Contract interface (e.g., `IProductContracts`) in `.Contracts` project
- Service class implements contract
- Validators are `static` classes with `static` `Validate()` methods
- Endpoints are `IEndpoint` implementors auto-discovered by source generator

**Barrel Exports:**
- Page exports via `Pages/index.ts` record (not C#)

## TypeScript Naming Patterns

**Files:**
- Components: `{ComponentName}.tsx` (PascalCase, e.g., `Create.tsx`, `Browse.tsx`)
- Pages: Folder structure mirrors route names (e.g., `/Pages/Account/ManageLayout.tsx`)
- Page objects: `{Page}.page.ts` (snake_case for page objects)
- Types: `types.ts` (auto-generated from `[Dto]` C# types)
- Config: `vite.config.ts`
- Tests: `{feature}.spec.ts`

**Variables/Constants:**
- React component names: `PascalCase`
- Props interfaces: `interface {ComponentName}Props` or inline `Props`
- Function names: `camelCase`
- React hooks: `const [state, setState] = useState()`
- Constants: `SCREAMING_SNAKE_CASE` (rare, usually just inline)

**Types:**
- `type` for unions/primitives
- `interface` for objects (more readable in component props)
- Imported from auto-generated `types.ts`

## TypeScript Code Style

**Formatting:**
- Indentation: 2 spaces
- Line width: 100 characters (Biome configured)
- Single quotes for strings
- Trailing commas in multiline constructs
- Semicolons always required
- No semicolons in JSX attribute values

**Linting:**
- Tool: Biome (v2.4.6+)
- Strict rules enabled: `recommended` + custom rules
- `a11y` warnings: `noSvgWithoutTitle`, `noLabelWithoutControl`, `useButtonType`
- `suspicious` warnings: `noExplicitAny`, `noArrayIndexKey`
- Tailwind CSS directives enabled in CSS parsing

**Code Style Preferences:**
- Default exports for page components
- Named exports for utilities and hooks
- Type imports: `import type { Product }`
- Prefer const over let
- Use optional chaining `?.` and nullish coalescing `??`

**Example:**
```typescript
import { Button, Card } from '@simplemodule/ui';
import type { Product } from '../types';

export default function Browse({ products }: { products: Product[] }) {
  return (
    <div className="space-y-3">
      {products.map((p) => (
        <Card key={p.id}>
          <span className="font-medium">{p.name}</span>
        </Card>
      ))}
    </div>
  );
}
```

## TypeScript Import Organization

**Order:**
1. React imports (`import { useState } from 'react';`)
2. Inertia.js imports (`import { router } from '@inertiajs/react';`)
3. Package imports (`import { Button } from '@simplemodule/ui';`)
4. Type imports (`import type { Product } from '../types';`)
5. Relative imports (same folder or parent)

## TypeScript Error Handling

**Frontend Errors:**
- Global Inertia error handler in `app.tsx` catches non-Inertia responses
- Shows toast notification for server errors (404, 500, etc.)
- Error format: `{ detail?: string; title?: string }` from ProblemDetails

**Pattern:**
```typescript
router.on('invalid', (event) => {
  event.preventDefault();
  const response = event.detail.response;
  const body = response.data as { detail?: string; title?: string } | undefined;
  const message = body?.detail ?? body?.title ?? `Server error (${response.status})`;
  showErrorToast(message);
});
```

## TypeScript Component Design

**Patterns:**
- Functional components only (no class components)
- Props always typed (inline interface or top-level type)
- Destructure props: `function Create({ products }: Props)`
- Use `key={id}` (not index) in lists
- Event handlers: `handleSubmit()`, `handleClick()`
- Callbacks as arrow functions for closure over state

**Example:**
```typescript
interface Props {
  products: Product[];
}

export default function Create({ products }: Props) {
  const [userId, setUserId] = useState('');

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    router.post('/orders', { userId });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* form content */}
    </form>
  );
}
```

---

*Convention analysis: 2026-03-18*
