---
outline: deep
---

# Form Requests

A **Form Request** bundles request binding, authorization, normalization, and validation for a single endpoint into one class. Instead of injecting `IValidator<T>` and checking results by hand in every handler, you declare a `[FormRequest]` type, accept it as a handler parameter, and the framework runs the full pipeline before your handler body executes.

The pattern is inspired by Laravel's form requests and lives in `SimpleModule.Core.FormRequests`.

## At a Glance

```csharp
[FormRequest]
public sealed partial class CreateTemplateFormRequest : FormRequest<CreateTemplateFormRequest>
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsHtml { get; set; } = true;

    public override void Prepare()
    {
        Name = Name.Trim();
        Slug = Slug.Trim().ToLowerInvariant();
    }

    protected override void ConfigureRules(RuleConfigurator<CreateTemplateFormRequest> rules)
    {
        rules.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        rules.RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
        rules.RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        rules.RuleFor(x => x.Body).NotEmpty();
    }
}
```

```csharp
public class CreateTemplateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/templates",
            // Bound, authorized, prepared, and validated before this runs:
            async (CreateTemplateFormRequest request, IEmailContracts email) =>
            {
                // request.Name is trimmed; all rules have passed.
                ...
            })
            .RequirePermission(EmailPermissions.ManageTemplates);
}
```

## The Pipeline

When a handler parameter is a `FormRequest`, the `FormRequestEndpointFilter` intercepts the call and runs these steps in order, for each form-request argument, before the handler:

1. **Bind** — ASP.NET minimal-API model binding deserializes the request (route → query → body) into the form-request's public properties.
2. **Authorize** — `Authorize(ClaimsPrincipal user)` runs. Returning `false` short-circuits with **403 Forbidden**.
3. **Prepare** — `Prepare()` normalizes the bound data (trim, lower-case, defaulting).
4. **Validate** — the cached FluentValidation validator runs. Failures short-circuit with **422 Unprocessable Entity**.
5. **Handler** — only now does your handler execute, with a normalized and valid request.

The filter is registered automatically — the source generator appends `.AddFormRequestFilter()` to every module's route group, so you never wire it up yourself.

## Defining Rules

Override `ConfigureRules` and use `RuleConfigurator<TSelf>`, a thin wrapper over FluentValidation's `InlineValidator<T>`:

```csharp
protected override void ConfigureRules(RuleConfigurator<UpdateSettingFormRequest> rules)
{
    rules.RuleFor(x => x.Key)
        .NotEmpty().WithMessage("Setting key is required.")
        .MaximumLength(256);

    // Conditional rule
    rules.RuleFor(x => x.Key)
        .Must(BeValidKey).WithMessage("Invalid key format.")
        .When(x => !string.IsNullOrEmpty(x.Key));

    rules.RuleFor(x => x.Scope).IsInEnum();
}
```

`RuleConfigurator<T>` exposes:

| Method | Purpose |
|--------|---------|
| `RuleFor(expr)` | Standard FluentValidation rule builder for a property |
| `RuleForEach(expr)` | Rules for each item in a collection property |
| `When(predicate, action)` | Apply the rules in `action` only when `predicate` is true |
| `Unless(predicate, action)` | Apply the rules in `action` unless `predicate` is true |

The validator is built once per type and cached, so rule configuration runs only on the first request.

## Authorization & Preparation

Both hooks are optional virtual methods on the base `FormRequest`:

```csharp
public abstract class FormRequest
{
    public virtual bool Authorize(ClaimsPrincipal user) => true; // allow by default
    public virtual void Prepare() { }                            // no-op by default
}
```

- **`Authorize`** lets a request enforce per-instance authorization beyond a static permission check — for example, "the target record belongs to the current user". Returning `false` yields a 403 before validation runs.
- **`Prepare`** normalizes input *before* validation, so your rules can assume clean data (trimmed strings, canonical casing).

`Prepare` always runs before `Validate`, whether the pipeline invokes it (via the filter) or you call `ValidateRulesAsync()` directly in a test.

## Error Responses

The filter is Inertia-aware. The same failure produces a JSON API response or a rendered error page depending on the request:

| Failure | API request | Inertia request |
|---------|-------------|-----------------|
| `Authorize` returns `false` | `403` Problem Details | `Error/403` page |
| Validation fails | `422` Problem Details with an `errors` map | `Error/422` page with errors |

A validation failure on an API endpoint returns RFC 7807 Problem Details:

```json
{
  "title": "Validation Error",
  "status": 422,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Name": ["Customer name is required."],
    "Scope": ["'Scope' has an invalid value."]
  }
}
```

See [Error Pages](/guide/error-pages) for the global error-rendering pipeline.

## TypeScript Generation

`[FormRequest]` types are emitted to the module's generated `types.ts` exactly like `[Dto]` types, so the frontend gets a typed interface for the request body for free:

```ts
// modules/Email/src/SimpleModule.Email/types.ts (generated)
export interface CreateTemplateFormRequest {
  name: string;
  slug: string;
  subject: string;
  body: string;
  isHtml: boolean;
}
```

## Source-Generator Rules

The generator discovers `[FormRequest]` classes and enforces two diagnostics at compile time:

| Diagnostic | Rule |
|-----------|------|
| `SM0056` | A `[FormRequest]` class must be `sealed` |
| `SM0057` | A `[FormRequest]` class must extend `FormRequest<TSelf>` |

Declare the class `sealed partial` (partial so the generator can extend it) and the constraint resolves itself.

## Testing

Because the validator is self-contained, you can exercise rules without spinning up the HTTP pipeline. `ValidateRulesAsync()` runs `Prepare()` and then validation:

```csharp
[Fact]
public async Task EmptyName_FailsValidation()
{
    var request = new CreateTemplateFormRequest { Name = "", Subject = "Hi", Body = "..." };

    var result = await request.ValidateRulesAsync();

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Name));
}
```

Integration tests can assert the end-to-end response shape instead — posting an invalid body and checking for a `422` with the expected field error.

## Relationship to Manual Validation

Form Requests are the recommended way to validate request input for new endpoints. The older pattern — injecting `IValidator<T>` and calling `ValidateAsync` inside the handler — still works and is documented under [Endpoints → Validation](/guide/endpoints#validation). Prefer a Form Request when an endpoint has its own request shape; reach for a standalone `AbstractValidator<T>` when a contract DTO is validated in several places.

## Next Steps

- [Endpoints](/guide/endpoints) — how endpoints are discovered, mapped, and bound.
- [Contracts & DTOs](/guide/contracts) — the `[Dto]` types that also drive TypeScript generation.
- [Error Pages](/guide/error-pages) — how `403`/`422` responses are rendered for API and Inertia callers.
