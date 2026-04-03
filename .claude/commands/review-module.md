Convention review for a SimpleModule module. Checks the module against the Constitution and project conventions.

If no module name was supplied as an argument, ask: "Which module would you like to review? (e.g. Products, Orders)"

Work through all 7 areas below in order. Do **not** auto-fix violations — record them and report at the end. For each violation found, record: **file path**, **approximate line or pattern**, **what's wrong**, **how to fix it**.

Replace `{Name}` with the module name throughout.

---

## Area 1 — Architecture: No impl→impl dependencies

Read `modules/{Name}/src/SimpleModule.{Name}/SimpleModule.{Name}.csproj`.

For every `<ProjectReference>` path in that file, check whether the path points into another module's implementation assembly. A violation is a path that:
- Contains `modules/` (or `modules\`)
- Does NOT end with `.Contracts/SimpleModule.*.Contracts.csproj` (or the backslash equivalent)

References to `.Contracts` projects are fine. References to the host or shared infrastructure are fine.

Violation fix: Replace the implementation reference with a reference to the other module's `.Contracts` project. Inject `I{OtherModule}Contracts` via constructor rather than using the concrete class directly.

---

## Area 2 — Endpoints: CrudEndpoints helpers and TypedResults

Grep for `MapGet|MapPost|MapPut|MapDelete` in `modules/{Name}/src/SimpleModule.{Name}/Endpoints/`.

Check each match:

1. **CrudEndpoints helpers** — Standard CRUD operations should use `CrudEndpoints.GetAll`, `CrudEndpoints.GetById`, `CrudEndpoints.Create`, `CrudEndpoints.Update`, or `CrudEndpoints.Delete` rather than raw inline `Results.*` returns for the same operation. Flag any endpoint that manually reimplements what a CrudEndpoints helper already provides.

2. **TypedResults** — API endpoints must use `TypedResults.*` (e.g., `TypedResults.Ok(...)`, `TypedResults.NotFound()`) not the non-generic `Results.*` static methods. Flag any use of `Results.Ok`, `Results.NotFound`, etc.

3. **RequirePermission** — Endpoints must use `.RequirePermission(...)` not bare `.RequireAuthorization()`. Flag any `.RequireAuthorization()` call without a permission argument.

---

## Area 3 — Naming conventions

Grep for class definitions (`class `) in `modules/{Name}/src/SimpleModule.{Name}/Endpoints/` and `modules/{Name}/src/SimpleModule.{Name}/Views/`.

For each source file found:

1. **One class per file** — if a file contains more than one `class` definition, flag it.
2. **Class name matches file name** — e.g., `CreateEndpoint.cs` must contain `class CreateEndpoint`. Flag mismatches.
3. **public class** — endpoint and view classes must be declared `public class`. Flag any that are `internal`, `private`, or `sealed`.
4. **File-scoped namespaces** — check for `namespace Foo {` (brace-style). Must be `namespace Foo;` (file-scoped). Flag any brace-style namespace declarations.
5. **Private field naming** — grep for `private` field declarations. Fields must use `_camelCase` prefix. Flag any private field that does not start with `_`.

---

## Area 4 — Frontend: Page registry completeness

Grep for `Inertia.Render(` in `modules/{Name}/src/SimpleModule.{Name}/`. Extract the first string argument from each call — this is the component key (e.g., `"Products/Browse"`).

Read `modules/{Name}/src/SimpleModule.{Name}/Pages/index.ts`. Extract all keys defined in the `pages` record.

Compare the two sets: every component key from an `Inertia.Render` call must appear as a key in `pages`. Flag any that are missing.

Then run `npm run validate-pages` for authoritative output and report any additional mismatches it finds.

Violation fix: Add a matching entry to `Pages/index.ts`:
```typescript
"{Name}/{ViewName}": () => import("../Views/{ViewName}"),
```

---

## Area 5 — Events: Proper definition and registration

Grep for `: IEvent` in `modules/{Name}/src/SimpleModule.{Name}.Contracts/`.

For each event type found:

1. **Record type** — events must be `record` types, not `class`. Flag any `class` that implements `IEvent`.

Grep for `IEventHandler<` in `modules/{Name}/src/SimpleModule.{Name}/`.

For each handler found, check that a corresponding `services.AddScoped<IEventHandler<TEvent>, THandler>()` call exists in `ConfigureServices` (or wherever DI is configured in the module class). Flag any handler with no registration.

Violation fix for missing registration: Add `services.AddScoped<IEventHandler<{EventType}>, {HandlerType}>();` in `ConfigureServices`.

---

## Area 6 — Tests: Coverage by endpoint file

List all files matching `*Endpoint.cs` in:
- `modules/{Name}/src/SimpleModule.{Name}/Endpoints/`
- `modules/{Name}/src/SimpleModule.{Name}/Views/`

List all test files in `modules/{Name}/tests/SimpleModule.{Name}.Tests/`.

For each endpoint class, check whether a corresponding test file exists (match by class name, e.g., `CreateEndpoint.cs` → `CreateEndpointTests.cs` or `CreateTests.cs`). Flag endpoint classes with no matching test file as missing coverage.

Note: integration tests using `SimpleModuleWebApplicationFactory` with `CreateAuthenticatedClient` are strongly preferred over unit tests that mock HTTP context.

---

## Area 7 — Permissions: Sealed class with const strings

Grep for `IModulePermissions` in `modules/{Name}/src/` (covers both the implementation assembly and the `.Contracts` assembly).

For the permissions class found:

1. **Sealed** — the class must be `sealed`. Flag if not.
2. **Naming pattern** — all `const string` values must follow the `"Module.Action"` format (e.g., `"Products.Create"`, `"Products.Delete"`). Flag any permission string that does not match this pattern.

If a permissions class implementing `IModulePermissions` was found above, grep for `AddPermissions<` or `builder.AddPermissions` in `modules/{Name}/src/SimpleModule.{Name}/{Name}Module.cs`.

3. **ConfigurePermissions registration** — This check is conditional:
   - If a permissions class implementing `IModulePermissions` exists **and** `builder.AddPermissions<{Name}Permissions>()` is absent from `{Name}Module.cs`: flag it as a violation.
   - If no permissions class exists and no `AddPermissions` call exists: mark Area 7 as N/A.
   - If both exist and the type matches: OK.

---

## Final Report

After completing all 7 areas, print a grouped report:

- If no violations in an area: `✅ [Area Name] — OK`
- If violations exist: `❌ [Area Name]` followed by a bulleted list of violations, each with:
  - File path (absolute or relative from repo root)
  - Approximate line or pattern where the problem appears
  - What is wrong
  - Specific fix

End the report with one of:
- `✅ {Name} module passes convention review.` (if zero violations found)
- `Found N violations across N areas.` (if any violations found)
