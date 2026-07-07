Diagnose why a module isn't being discovered or working correctly in SimpleModule.

If no module name was supplied as an argument, ask: "Which module would you like to debug? (e.g. Products, Orders)"

Work through all 7 checks below **in order**. For each check, mark it ✅ pass / ❌ fail / ⚠️ warning, then apply the described fix before moving to the next check. Do not skip checks even if earlier ones fail.

**Status marker meanings:** ✅ = passes all requirements · ❌ = fails, fix required · ⚠️ = exists and will work, but has a non-critical configuration difference (e.g., SM00xx warnings but no errors, or a file exists but a setting differs from the expected default).

Replace `{Name}` with the module name throughout.

---

## Check 1 — Module class

Read `modules/{Name}/src/SimpleModule.{Name}/{Name}Module.cs`.

Verify:
- The file exists.
- The class is decorated with `[Module(...)]`.
- The class implements `IModule`.
- The class is `public`.
- The class is **not** `sealed`.

Fix if failing: Create or correct the file so the class is `public`, non-sealed, carries `[Module(...)]`, and implements `IModule`.

---

## Check 2 — Contracts SDK

Read `modules/{Name}/src/SimpleModule.{Name}.Contracts/SimpleModule.{Name}.Contracts.csproj`.

Verify:
- The file exists.
- `Sdk="Microsoft.NET.Sdk"` (must NOT be `Microsoft.NET.Sdk.Razor` or `Microsoft.NET.Sdk.Web`).

Fix if failing: Set the `Sdk` attribute to `Microsoft.NET.Sdk`.

---

## Check 3 — Implementation SDK + FrameworkReference

Read `modules/{Name}/src/SimpleModule.{Name}/SimpleModule.{Name}.csproj`.

Verify:
- The file exists.
- `Sdk="Microsoft.NET.Sdk.StaticWebAssets"` (the implementation project ships the module's static frontend bundle; must NOT be `Microsoft.NET.Sdk.Razor` or `Microsoft.NET.Sdk.Web`).
- Contains `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

Fix if failing: Set `Sdk="Microsoft.NET.Sdk.StaticWebAssets"` if it is wrong. If `<FrameworkReference Include="Microsoft.AspNetCore.App" />` is missing, add it inside an `<ItemGroup>`.

---

## Check 4 — Host reference

Read `template/SimpleModule.Host/SimpleModule.Host.csproj`.

Verify:
- A `<ProjectReference>` whose path contains `modules\{Name}\src\SimpleModule.{Name}\SimpleModule.{Name}.csproj` (use a substring match to tolerate slash style differences). The correct relative path from the host project is `..\..\modules\{Name}\src\SimpleModule.{Name}\SimpleModule.{Name}.csproj`.

Fix if failing: Add the `<ProjectReference>` inside an `<ItemGroup>` in the host `.csproj`.

---

## Check 5 — Solution file

Read `SimpleModule.slnx`.

Verify:
- The contracts `.csproj` path appears: `modules/{Name}/src/SimpleModule.{Name}.Contracts/SimpleModule.{Name}.Contracts.csproj`
- The implementation `.csproj` path appears: `modules/{Name}/src/SimpleModule.{Name}/SimpleModule.{Name}.csproj`

Fix if failing: Locate the existing `<Folder Name="/modules/">` element in `SimpleModule.slnx` and add a new child `<Folder Name="/modules/{Name}/">` block inside it with `<Project>` entries for both the contracts and implementation `.csproj` files.

---

## Check 6 — dotnet build diagnostics

Run:
```
dotnet build --no-incremental 2>&1 | grep -E "SM0|error" | head -50
```

Surface any SM00xx source generator diagnostic codes. Codes currently span **SM0001–SM0061** (non-contiguous); `docs/CONSTITUTION.md` is the authoritative catalog. The most commonly hit:

| Code | Meaning |
|------|---------|
| SM0011 | Direct module→module implementation reference (reference the `.Contracts` project instead) |
| SM0014 | Referenced Contracts assembly has no public interfaces |
| SM0025 | No implementation found for a contract interface (create the `{Name}ContractsService`) |
| SM0032 | Permission class is not `sealed` |
| SM0040 | Duplicate module name across modules |
| SM0041 | View page name not prefixed with the module name |
| SM0042 | Module has view endpoints but no `ViewPrefix` on `[Module]` |
| SM0043 | Module overrides no `IModule` method |
| SM0049 | More than one endpoint class in a single file |
| SM0054 | Endpoint missing `public const string Route` (Info) |
| SM0056 / SM0057 | `[FormRequest]` class not sealed / not extending `FormRequest<TSelf>` |
| SM0058–SM0061 | Policy class rules (resource not a `[Dto]`, not public, foreign module, or generic) |

For any code not listed here, look it up in `docs/CONSTITUTION.md`. Fix each reported code before continuing.

---

## Check 7 — Page registry

Run:
```
npm run validate-pages
```

If it exits with an error, identify each missing entry and show the exact line to add to `modules/{Name}/src/SimpleModule.{Name}/Pages/index.ts` (pages are co-located in `Pages/`, so the import path is relative to that folder):

```typescript
'{Name}/{ViewName}': () => import('./{ViewName}'),
```

Derive `{ViewName}` from the component name reported missing by `npm run validate-pages` (e.g., if it reports `AuditLogs/Browse`, the ViewName is `Browse`).

Add the missing entries to the `pages` record in `Pages/index.ts`, then re-run `npm run validate-pages` to confirm it passes.

---

## Final Summary

Print a summary table of all 7 checks:

| Check | Status | Notes |
|-------|--------|-------|
| 1. Module class | ✅/❌/⚠️ | |
| 2. Contracts SDK | ✅/❌/⚠️ | |
| 3. Implementation SDK + FrameworkReference | ✅/❌/⚠️ | |
| 4. Host reference | ✅/❌/⚠️ | |
| 5. Solution file | ✅/❌/⚠️ | |
| 6. dotnet build diagnostics | ✅/❌/⚠️ | |
| 7. Page registry | ✅/❌/⚠️ | |

For every ❌ row, include specific fix instructions. If all checks pass, confirm the module should be discovered correctly on next build.
