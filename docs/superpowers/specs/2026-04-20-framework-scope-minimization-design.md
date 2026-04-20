# Framework Scope Minimization — Design

## Goal

Shrink the framework to the foundational plumbing that every module depends on. Everything domain-shaped, provider-shaped, or optional moves into a module or into a new `tools/` category. Prevent regression via an explicit allowlist enforced in CI.

## Target state

```
framework/        4 projects (Core, Database, Generator, Hosting)
tools/            non-module .NET projects (DevTools and future siblings)
modules/          existing modules + absorbed framework provider projects
packages/         unchanged (frontend npm packages)
scripts/          Node build scripts (just renamed from tools/)
```

The `tools/` rename from the original `tools/` directory to `scripts/` has already landed in commit-pending work.

## What moves where

### Framework allowlist (final state)

Exactly these four projects remain under `framework/`:

- `SimpleModule.Core`
- `SimpleModule.Database`
- `SimpleModule.Generator`
- `SimpleModule.Hosting`

### Migration map

| From | To |
|---|---|
| `framework/SimpleModule.Agents` | `modules/Agents/src/SimpleModule.Agents/` (merge into main assembly) |
| `framework/SimpleModule.AI.Anthropic` | `modules/Agents/src/SimpleModule.Agents.AI.Anthropic/` |
| `framework/SimpleModule.AI.AzureOpenAI` | `modules/Agents/src/SimpleModule.Agents.AI.AzureOpenAI/` |
| `framework/SimpleModule.AI.Ollama` | `modules/Agents/src/SimpleModule.Agents.AI.Ollama/` |
| `framework/SimpleModule.AI.OpenAI` | `modules/Agents/src/SimpleModule.Agents.AI.OpenAI/` |
| `framework/SimpleModule.Rag` | `modules/Rag/src/SimpleModule.Rag/` (merge into main assembly) |
| `framework/SimpleModule.Rag.StructuredRag` | `modules/Rag/src/SimpleModule.Rag.StructuredRag/` |
| `framework/SimpleModule.Rag.VectorStore.InMemory` | `modules/Rag/src/SimpleModule.Rag.VectorStore.InMemory/` |
| `framework/SimpleModule.Rag.VectorStore.Postgres` | `modules/Rag/src/SimpleModule.Rag.VectorStore.Postgres/` |
| `framework/SimpleModule.Storage` | `modules/FileStorage/src/SimpleModule.FileStorage.Storage/` (or merged into Contracts — audit during Phase 1) |
| `framework/SimpleModule.Storage.Azure` | `modules/FileStorage/src/SimpleModule.FileStorage.Azure/` |
| `framework/SimpleModule.Storage.Local` | `modules/FileStorage/src/SimpleModule.FileStorage.Local/` |
| `framework/SimpleModule.Storage.S3` | `modules/FileStorage/src/SimpleModule.FileStorage.S3/` |
| `framework/SimpleModule.DevTools` | `tools/SimpleModule.DevTools/` |

The `FileStorage → Storage` module rename is deferred to a separate follow-up. During this work, the FileStorage module keeps its current name, and the absorbed sub-projects take temporary names like `SimpleModule.FileStorage.Azure`. The follow-up drops `File` everywhere.

## Sub-project convention

A **sub-project** is a `.csproj` under `modules/{Name}/src/` that is not the main module assembly or its Contracts.

Rules:

1. Name matches `SimpleModule.{ModuleName}.{Suffix}` (e.g., `SimpleModule.Agents.AI.Anthropic`).
2. Lives at `modules/{ModuleName}/src/SimpleModule.{ModuleName}.{Suffix}/`.
3. Does not declare `[Module]` — only the main assembly owns lifecycle. Sub-projects expose DI via extension methods that the main module calls from `ConfigureServices`.
4. May contain `IEndpoint` implementations, `[Dto]` types, services, value objects. The source generator picks these up through the main module's transitive reference chain — no generator changes required.
5. May not own a `DbContext`. The module owns data.
6. Dependencies: may reference own Contracts, other modules' Contracts, framework Core; may not reference another module's implementation (SM0011 already enforces this).

The generator does not need to know about sub-projects. It discovers `[Module]` classes (sub-projects have none), `IEndpoint` implementations across referenced assemblies (already works), and `[Dto]` types across assemblies (already works). Existing diagnostics SM0052/SM0053 only fire on types annotated with `[Module]` and so do not affect sub-projects.

## `tools/` category

A `.csproj` under `tools/` is a development-time or host-time utility.

Rules:

1. Name matches `SimpleModule.{ToolName}` (e.g., `SimpleModule.DevTools`).
2. Lives at `tools/SimpleModule.{ToolName}/` — flat layout, no `src/` subdirectory, no Contracts split.
3. Does not declare `[Module]`.
4. No endpoints, no `DbContext`, no events.
5. Referenced by the host or by other tools only — modules never reference `tools/` projects.

Tools are invisible to module discovery because they declare no `[Module]`.

## Enforcement

One CI script, one invariant file, one Constitution section.

### `framework/.allowed-projects`

Plain text, one project name per line. During migration the file starts with all current framework projects listed and shrinks as PRs land. Final content:

```
SimpleModule.Core
SimpleModule.Database
SimpleModule.Generator
SimpleModule.Hosting
```

### `scripts/validate-framework-scope.mjs`

Runs four checks, exit 1 on any failure:

1. **Framework allowlist.** Every directory `framework/*/` must match an entry in `.allowed-projects`.
2. **Sub-project naming.** Every `.csproj` under `modules/{ModuleName}/src/` that is not `SimpleModule.{ModuleName}` or `SimpleModule.{ModuleName}.Contracts` must match `SimpleModule.{ModuleName}.*`.
3. **Sub-project lifecycle.** No `.cs` file in a sub-project declares `[Module(`.
4. **Tools layering.** Every directory under `tools/` is `SimpleModule.{Name}/`. No `.cs` file under `tools/` declares `[Module(`. No `.csproj` under `modules/` has a `ProjectReference` to a `tools/` project.

### CI wiring

New step in the existing GitHub Actions workflow, after `dotnet restore`. No build required — file-scan only. Also appended to `npm run check` so local pre-push catches violations.

### Constitution Section 13 (new)

Text to add to `docs/CONSTITUTION.md`:

> ## 13. Framework Scope
>
> The `framework/` directory contains foundational plumbing: module lifecycle, source generation, DbContext infrastructure, and host bootstrap. Nothing else.
>
> Framework projects are explicitly allowlisted in `framework/.allowed-projects`. This list contains exactly: `SimpleModule.Core`, `SimpleModule.Database`, `SimpleModule.Generator`, `SimpleModule.Hosting`.
>
> Adding a project to `framework/` requires:
>
> 1. Justification that the project is foundational — referenced by the host bootstrap or by every module, with no domain or provider semantics.
> 2. A PR that updates `.allowed-projects`, names the reviewer, and documents why a module or `tools/` project is insufficient.
>
> The `tools/` directory holds non-module .NET utilities consumed by the host or other tools. Tools never declare `[Module]` and are never referenced from modules.
>
> Sub-projects are additional assemblies inside a module. They live at `modules/{Name}/src/SimpleModule.{Name}.{Suffix}/`, do not declare `[Module]`, and may not own a `DbContext`. Section 2 module ownership rules apply; sub-projects inherit them.

### Rejected alternatives

- **MSBuild-level validation** — duplicates the CI check with more error noise during partial builds. Rejected.
- **New SM diagnostic for allowlist violation** — adds complexity to the generator, which contradicts the shrink-the-framework goal. Rejected.
- **NuGet-level enforcement** — out of scope; packaging is orthogonal.

## Migration phases

The allowlist starts permissive (all 19 projects listed) and shrinks as PRs land, keeping CI green throughout.

### Phase 0: Scaffolding

- Add `framework/.allowed-projects` listing all 19 current framework projects.
- Add `scripts/validate-framework-scope.mjs`.
- Add Constitution Section 13.
- Wire check into CI and `npm run check`.

Risk: low. Adds files, changes nothing functional.

### Phase 1: DevTools → `tools/`

Warm-up phase, proves the `tools/` category works with least scope.

- `framework/SimpleModule.DevTools/` → `tools/SimpleModule.DevTools/`
- Update host reference in `template/SimpleModule.Host/SimpleModule.Host.csproj`.
- Update `SimpleModule.slnx`.
- Remove `SimpleModule.DevTools` from `.allowed-projects`.

Risk: low. Single project move, no absorption.

### Phase 2: Storage providers → `modules/FileStorage/`

Moves 4 framework projects into the existing FileStorage module.

- `framework/SimpleModule.Storage*` → `modules/FileStorage/src/SimpleModule.FileStorage.{Storage,Azure,Local,S3}/`
- Audit whether `SimpleModule.Storage` (the abstractions) should merge into existing `SimpleModule.FileStorage.Contracts` or remain as its own sub-project.
- Resolve pre-existing SM0025 error on FileStorage contract-implementation split.
- Update `.slnx`, host `ProjectReference`s, any `.targets` paths.
- Remove 4 entries from `.allowed-projects`.

Risk: moderate. Structural moves touch solution file and host references.

### Phase 3: Agents providers → `modules/Agents/`

Same pattern as Phase 2, 5 projects.

- `framework/SimpleModule.Agents` + `AI.{Anthropic,AzureOpenAI,Ollama,OpenAI}` → `modules/Agents/src/SimpleModule.Agents[.AI.*]/`
- Resolve pre-existing SM0025 on `IAgentsContracts`.
- Remove 5 entries from `.allowed-projects`.

Risk: moderate. Five projects, four external SDK dependencies.

### Phase 4: Rag providers → `modules/Rag/`

Same pattern, 4 projects.

- `framework/SimpleModule.Rag*` → `modules/Rag/src/SimpleModule.Rag[.StructuredRag|.VectorStore.*]/`
- Resolve pre-existing SM0025 on `IRagContracts`.
- Remove 4 entries from `.allowed-projects`.

Risk: moderate.

### Phase 5 (implicit)

After Phases 1-4 land, `.allowed-projects` contains exactly the four target entries. No separate work — the allowlist is the natural end state.

## Follow-up (not in this project)

**`FileStorage → Storage` module rename.** Separate PR, separate risk profile.

Scope:
- Rename module directory, projects, namespaces, classes, constants.
- Change `[Module]` name, `RoutePrefix`, `ViewPrefix`.
- Update permission constants (SM0034 will enforce prefix).
- Update React page names and `Pages/index.ts` keys.
- DB migration: rename Postgres/SQL Server schema (`filestorage` → `storage`) or SQLite table prefix (`FileStorage_` → `Storage_`).
- Update any cross-module references to `SimpleModule.FileStorage.Contracts`.

Risk: high. User-facing URL changes, permission string changes, irreversible DB migration. Ship alone so revert is clean.

## Cross-phase note

The current build fails on pre-existing SM0025 errors for `IAgentsContracts`, `IRagContracts`, and `IJobExecutionContext`. These are not introduced by the migration — they indicate the framework/module split is already partially broken on `main`. Phases 2-4 must resolve their respective SM0025 as part of absorption; they block per-phase verification.

Unrelated: `MailKit 4.15.1` has a known moderate-severity vulnerability (NU1902) failing the host build under `TreatWarningsAsErrors`. This is pre-existing and outside the scope of this work — flag for a separate dependency-upgrade PR.

## Out of scope

- Splitting `SimpleModule.Core` into smaller framework projects (Orchard-style decomposition). Interesting but orthogonal.
- Decomposing modules into feature-level toggles (Orchard's `[Feature]` concept). Not requested; the current "one module = one feature" is working.
- Any change to the source generator's diagnostic set. The generator stays untouched.
