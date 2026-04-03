# /module-status

Print a health snapshot of a SimpleModule module. No builds required — reads and greps only.

## Step 0 — Resolve Module Name

If no argument was supplied, ask: "Which module would you like to inspect? (e.g., Products, Orders, Users)"

Use the provided name as `{Name}` throughout. Construct the paths:
- `CONTRACTS = modules/{Name}/src/SimpleModule.{Name}.Contracts/`
- `IMPL = modules/{Name}/src/SimpleModule.{Name}/`
- `TESTS = modules/{Name}/tests/SimpleModule.{Name}.Tests/`

First, verify that IMPL exists by checking if `{Name}Module.cs` or any `.csproj` is present at that path. If not, note: "Note: The implementation directory may use a non-standard name. Check `modules/{Name}/src/` to find the correct directory name and adjust paths accordingly."

---

## Step 1 — Inventory

**API Endpoints**
Use Glob with pattern `modules/{Name}/src/SimpleModule.{Name}/Endpoints/**/*Endpoint.cs`. Count the results and collect the bare file names (strip path and `.cs`).

**View Endpoints**
Use Glob with pattern `modules/{Name}/src/SimpleModule.{Name}/Views/**/*Endpoint.cs`. Count and collect bare names. Note whether any view endpoints exist — this drives Sections 2 and 5.

**Entities**
Check whether `modules/{Name}/src/SimpleModule.{Name}/EntityConfigurations/` exists. If it does, grep for `HasKey(` in that directory (all `.cs` files). For each matching file, derive the entity name by stripping `Configuration` from the class name (e.g., `ProductConfiguration` → `Product`). If the directory does not exist, note "N/A — no EntityConfigurations directory".

**Domain Events**
Grep for `: IEvent` in `modules/{Name}/src/SimpleModule.{Name}.Contracts/` (all `.cs` files). Extract the `record` name from each match. List them; if none, show `0`.

**Services**
Grep for `class\s+\w+\s*:\s*I\w+Contracts` in `modules/{Name}/src/SimpleModule.{Name}/` (all `.cs` files). List each class name found. If no matches found, show "none detected".

---

## Step 2 — Page Registry Coverage

Skip this section entirely (output "N/A — no view endpoints") if no `*Endpoint.cs` files exist in the Views/ directory.

Otherwise:

1. Grep for `Inertia.Render(` in `modules/{Name}/src/SimpleModule.{Name}/` (all `.cs` files). For each match, extract the first string argument — the component name — from the call (e.g., `Inertia.Render("Products/Browse", ...)` → `Products/Browse`). Collect these as **C# renders**.

2. Read `modules/{Name}/src/SimpleModule.{Name}/Pages/index.ts`. Extract every key from the `pages` record (quoted strings before the `:` on each entry line). Collect these as **TS entries**.

3. Compute:
   - **Missing**: keys present in C# renders but absent from TS entries.
   - **Orphaned**: keys present in TS entries but absent from C# renders.
   - **Coverage**: `(count of C# renders with a matching TS entry) / (total C# renders)` shown as `N/N (X%)`.

---

## Step 3 — Test File Coverage

1. Collect all endpoint class names from both Endpoints/ and Views/ (same lists from Step 1).
2. Use Glob `modules/{Name}/tests/SimpleModule.{Name}.Tests/**/*.cs` to list all test files.
3. For each endpoint class name (e.g., `GetAllEndpoint`), check whether that string appears in any test file name or — if needed — grep for it across the test directory.
4. Report covered count / total. List any endpoint classes with no test file found.

---

## Step 4 — Cross-Module Dependencies

**Consumes**
Read `modules/{Name}/src/SimpleModule.{Name}/SimpleModule.{Name}.csproj`. List every `<ProjectReference>` `Include` path that points to another module's `.Contracts` project (i.e., paths containing `Contracts` but not `SimpleModule.{Name}.Contracts` itself). Extract just the project name.

**Consumed by**
Grep for `SimpleModule.{Name}.Contracts` in all `.csproj` files under `modules/`, excluding the module's own projects (`modules/{Name}/`). For each match, extract the module name from the file path.

---

## Step 5 — Required Files

For each entry, check whether the file or directory exists and assign a status symbol.

| File | Required | Status |
|------|----------|--------|
| `*Constants.cs` anywhere under `modules/{Name}/src/` | Yes | ✅ if at least one found (list filenames), ❌ if none |
| `I*Contracts.cs` in CONTRACTS | Yes | ✅ if at least one found (list filenames), ❌ if none |
| `{Name}Module.cs` in IMPL | Yes | ✅ if exists, ❌ if not |
| `Pages/index.ts` in IMPL | If view endpoints exist | ✅/❌ or N/A |
| `vite.config.ts` in IMPL | If view endpoints exist | ✅/❌ or N/A |
| `package.json` in IMPL | If view endpoints exist | ✅/❌ or N/A |
| `tests/` directory (`TESTS`) | Recommended | ✅ if exists, ⚠️ if not |

Mark Pages/index.ts, vite.config.ts, and package.json as `N/A` when no view endpoints exist.

---

## Output Format

Print exactly this structure (fill in real values):

```
## Module Status: {Name}

### Inventory
- API Endpoints: N — [Name1, Name2, ...]
- View Endpoints: N — [Name1, Name2, ...]
- Entities: N — [Name1, Name2, ...]  (or "N/A — no EntityConfigurations directory")
- Domain Events: N — [Name1, Name2, ...]  (or "0")
- Services: N — [ClassName, ...]

### Page Registry
- Coverage: N/N (X%)
- Missing (C# → no TS entry): [list] or "none"
- Orphaned (TS entry → no C# render): [list] or "none"
(or "N/A — no view endpoints")

### Test Coverage
- Covered: N/N endpoint files
- Missing tests for: [EndpointClass1, ...] or "all covered"

### Dependencies
- Consumes: [Module1.Contracts, ...] or "none"
- Consumed by: [Module2, ...] or "none"

### Required Files
| File | Required | Status |
|------|----------|--------|
| *Constants.cs (any, under src/) | Yes | ✅ [filename(s)] / ❌ |
| I*Contracts.cs (any, in Contracts/) | Yes | ✅ [filename(s)] / ❌ |
| {Name}Module.cs | Yes | ✅/❌ |
| Pages/index.ts | If view endpoints | ✅/❌/N/A |
| vite.config.ts | If view endpoints | ✅/❌/N/A |
| package.json | If view endpoints | ✅/❌/N/A |
| tests/ directory | Recommended | ✅/⚠️ |
```

Do not include any additional commentary outside these sections.
