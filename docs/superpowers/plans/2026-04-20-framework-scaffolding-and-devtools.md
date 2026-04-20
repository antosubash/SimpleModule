# Framework Scope Minimization — Phase 0 (Scaffolding) + Phase 1 (DevTools) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land the enforcement scaffolding (allowlist file, CI validation script, Constitution section) that prevents `framework/` from growing, and prove it works by moving `SimpleModule.DevTools` out of `framework/` into a new `tools/` category.

**Architecture:** A plain-text allowlist file (`framework/.allowed-projects`) names every project permitted under `framework/`. A Node validation script (`scripts/validate-framework-scope.mjs`) fails CI if `framework/` contains anything else, if sub-projects under `modules/` violate the naming pattern, or if `tools/` projects misbehave. Constitution Section 13 documents the invariant. After scaffolding is green, `SimpleModule.DevTools` moves from `framework/` to a new `tools/` directory — the first project to exercise the new category.

**Tech Stack:** Node.js (validation script), MSBuild `.slnx` / `.csproj` (solution file, project references), GitHub Actions (CI), markdown (Constitution).

**Follow-up plans (not in this plan):** Phase 2 (Storage providers → `modules/FileStorage/`), Phase 3 (Agents providers → `modules/Agents/`), Phase 4 (Rag providers → `modules/Rag/`). Each will be its own plan once Phase 1 lands.

**Related spec:** `docs/superpowers/specs/2026-04-20-framework-scope-minimization-design.md`

---

## Task 1: Create the permissive allowlist

The allowlist starts with every current framework project listed. This keeps CI green during migration. Entries are removed as each framework project moves out.

**Files:**
- Create: `framework/.allowed-projects`

- [ ] **Step 1: Create the allowlist file**

Write the following to `framework/.allowed-projects` (one project name per line, alphabetical for diffability):

```
SimpleModule.Agents
SimpleModule.AI.Anthropic
SimpleModule.AI.AzureOpenAI
SimpleModule.AI.Ollama
SimpleModule.AI.OpenAI
SimpleModule.Core
SimpleModule.Database
SimpleModule.DevTools
SimpleModule.Generator
SimpleModule.Hosting
SimpleModule.Rag
SimpleModule.Rag.StructuredRag
SimpleModule.Rag.VectorStore.InMemory
SimpleModule.Rag.VectorStore.Postgres
SimpleModule.Storage
SimpleModule.Storage.Azure
SimpleModule.Storage.Local
SimpleModule.Storage.S3
```

This mirrors the output of `ls framework/` today (excluding `Directory.Build.props`).

- [ ] **Step 2: Verify the list matches reality**

Run: `ls framework/ | grep -v Directory.Build.props | sort | diff - framework/.allowed-projects`
Expected: no output (files match exactly).

---

## Task 2: Write the validation script

A single-file Node script, no dependencies beyond Node stdlib. It performs four checks and exits 1 on any failure. The codebase pattern (see `scripts/validate-i18n.mjs`) is self-contained scripts without separate unit tests — CI exercises the script on the real repo.

**Files:**
- Create: `scripts/validate-framework-scope.mjs`

- [ ] **Step 1: Create the script with the shebang and framework allowlist check**

```javascript
#!/usr/bin/env node
// Validates framework scope rules (see docs/CONSTITUTION.md Section 13).
// Usage: node scripts/validate-framework-scope.mjs
//
// Checks:
// 1. framework/ only contains projects listed in framework/.allowed-projects
// 2. Sub-projects under modules/{Name}/src/ match SimpleModule.{Name}.{Suffix}
// 3. Sub-projects do not declare [Module]
// 4. tools/ projects are flat-layout SimpleModule.{Name} and never declare [Module]
//    and no module csproj references a tools/ project

import { readdirSync, readFileSync, statSync } from 'fs';
import { resolve, join, basename } from 'path';

const repoRoot = resolve(new URL('..', import.meta.url).pathname);
const errors = [];

function readAllowlist() {
    const path = join(repoRoot, 'framework', '.allowed-projects');
    return readFileSync(path, 'utf8')
        .split('\n')
        .map((line) => line.trim())
        .filter((line) => line.length > 0 && !line.startsWith('#'));
}

function checkFrameworkAllowlist() {
    const allowed = new Set(readAllowlist());
    const frameworkDir = join(repoRoot, 'framework');
    const entries = readdirSync(frameworkDir, { withFileTypes: true });
    for (const entry of entries) {
        if (!entry.isDirectory()) continue;
        if (!allowed.has(entry.name)) {
            errors.push(
                `framework/${entry.name} is not in framework/.allowed-projects. ` +
                    `Add it with reviewer approval, or move it out of framework/.`,
            );
        }
    }
}

// Placeholder implementations — filled in by later tasks.
function checkSubProjectNaming() {}
function checkSubProjectNoModuleAttribute() {}
function checkToolsLayering() {}

checkFrameworkAllowlist();
checkSubProjectNaming();
checkSubProjectNoModuleAttribute();
checkToolsLayering();

if (errors.length > 0) {
    console.error('Framework scope validation failed:\n');
    for (const err of errors) console.error(`  ✗ ${err}`);
    process.exit(1);
}

console.log('✓ Framework scope validation passed');
```

- [ ] **Step 2: Make the script executable and verify it runs**

Run: `chmod +x scripts/validate-framework-scope.mjs && node scripts/validate-framework-scope.mjs`
Expected: `✓ Framework scope validation passed` (exit 0). The allowlist matches current framework/ state, so the check passes.

- [ ] **Step 3: Verify the check catches violations**

Temporarily create a rogue project directory to confirm the check fires:

```bash
mkdir -p framework/SimpleModule.RogueProject
node scripts/validate-framework-scope.mjs
echo "Exit: $?"
rmdir framework/SimpleModule.RogueProject
```

Expected output (exit 1):
```
Framework scope validation failed:

  ✗ framework/SimpleModule.RogueProject is not in framework/.allowed-projects. ...
Exit: 1
```

Expected after restoration: re-running the script exits 0.

---

## Task 3: Add sub-project naming check

Sub-projects live under `modules/{ModuleName}/src/` and must be named `SimpleModule.{ModuleName}` (main), `SimpleModule.{ModuleName}.Contracts`, or `SimpleModule.{ModuleName}.{Suffix}` (sub-project).

**Files:**
- Modify: `scripts/validate-framework-scope.mjs`

- [ ] **Step 1: Implement `checkSubProjectNaming`**

Replace the placeholder `function checkSubProjectNaming() {}` with:

```javascript
function checkSubProjectNaming() {
    const modulesDir = join(repoRoot, 'modules');
    if (!exists(modulesDir)) return;
    const moduleDirs = readdirSync(modulesDir, { withFileTypes: true })
        .filter((e) => e.isDirectory());
    for (const moduleEntry of moduleDirs) {
        const moduleName = moduleEntry.name;
        const srcDir = join(modulesDir, moduleName, 'src');
        if (!exists(srcDir)) continue;
        const projectDirs = readdirSync(srcDir, { withFileTypes: true })
            .filter((e) => e.isDirectory());
        const expectedPrefix = `SimpleModule.${moduleName}`;
        for (const projectEntry of projectDirs) {
            const name = projectEntry.name;
            // Must match SimpleModule.{ModuleName} or SimpleModule.{ModuleName}.{Suffix}
            if (name !== expectedPrefix && !name.startsWith(`${expectedPrefix}.`)) {
                errors.push(
                    `modules/${moduleName}/src/${name}/ does not match required ` +
                        `pattern '${expectedPrefix}[.*]'. Sub-projects must be named ` +
                        `'${expectedPrefix}.{Suffix}'.`,
                );
            }
        }
    }
}

function exists(path) {
    try { statSync(path); return true; } catch { return false; }
}
```

- [ ] **Step 2: Verify the check passes against current repo state**

Run: `node scripts/validate-framework-scope.mjs`
Expected: `✓ Framework scope validation passed` (exit 0).

If this fails, it means an existing module already violates the pattern — fix the violation in a separate PR before continuing. (Unlikely, since the spec asserts the convention is already followed.)

- [ ] **Step 3: Verify the check catches violations**

```bash
mkdir -p modules/Products/src/SimpleModule.WrongName
node scripts/validate-framework-scope.mjs
echo "Exit: $?"
rmdir modules/Products/src/SimpleModule.WrongName
```

Expected (exit 1): `modules/Products/src/SimpleModule.WrongName/ does not match required pattern 'SimpleModule.Products[.*]'...`

---

## Task 4: Add sub-project no-[Module] check

Sub-projects may not declare `[Module]` — only the main module assembly owns lifecycle. A simple grep across `.cs` files is sufficient.

**Files:**
- Modify: `scripts/validate-framework-scope.mjs`

- [ ] **Step 1: Add a recursive `.cs` file walker helper**

Insert this helper near the other helpers:

```javascript
function walkCsFiles(dir) {
    const results = [];
    const stack = [dir];
    while (stack.length > 0) {
        const current = stack.pop();
        let entries;
        try {
            entries = readdirSync(current, { withFileTypes: true });
        } catch {
            continue;
        }
        for (const entry of entries) {
            const full = join(current, entry.name);
            if (entry.isDirectory()) {
                if (entry.name === 'bin' || entry.name === 'obj' || entry.name === 'node_modules') {
                    continue;
                }
                stack.push(full);
            } else if (entry.isFile() && entry.name.endsWith('.cs')) {
                results.push(full);
            }
        }
    }
    return results;
}
```

- [ ] **Step 2: Implement `checkSubProjectNoModuleAttribute`**

Replace `function checkSubProjectNoModuleAttribute() {}` with:

```javascript
function checkSubProjectNoModuleAttribute() {
    const modulesDir = join(repoRoot, 'modules');
    if (!exists(modulesDir)) return;
    const moduleDirs = readdirSync(modulesDir, { withFileTypes: true })
        .filter((e) => e.isDirectory());
    for (const moduleEntry of moduleDirs) {
        const moduleName = moduleEntry.name;
        const srcDir = join(modulesDir, moduleName, 'src');
        if (!exists(srcDir)) continue;
        const projectDirs = readdirSync(srcDir, { withFileTypes: true })
            .filter((e) => e.isDirectory());
        for (const projectEntry of projectDirs) {
            const name = projectEntry.name;
            const isMain = name === `SimpleModule.${moduleName}`;
            const isContracts = name === `SimpleModule.${moduleName}.Contracts`;
            if (isMain || isContracts) continue;
            // This is a sub-project. Scan its .cs files for [Module(
            const files = walkCsFiles(join(srcDir, name));
            for (const file of files) {
                const content = readFileSync(file, 'utf8');
                if (/\[\s*Module\s*\(/.test(content)) {
                    errors.push(
                        `Sub-project ${name} declares [Module] in ${file.substring(repoRoot.length + 1)}. ` +
                            `Only the main module assembly (SimpleModule.${moduleName}) may declare [Module].`,
                    );
                }
            }
        }
    }
}
```

- [ ] **Step 3: Verify the check passes**

Run: `node scripts/validate-framework-scope.mjs`
Expected: `✓ Framework scope validation passed` (no existing sub-projects exist yet, so this check is a no-op against current state).

- [ ] **Step 4: Verify the check catches a violation**

Create a temporary sub-project with a bogus `[Module]`:

```bash
mkdir -p modules/Products/src/SimpleModule.Products.Fake
cat > modules/Products/src/SimpleModule.Products.Fake/Bad.cs <<'EOF'
using SimpleModule.Core;
[Module("Bad")]
public class BadSub {}
EOF
node scripts/validate-framework-scope.mjs
echo "Exit: $?"
rm -rf modules/Products/src/SimpleModule.Products.Fake
```

Expected (exit 1): `Sub-project SimpleModule.Products.Fake declares [Module] in modules/Products/src/SimpleModule.Products.Fake/Bad.cs. Only the main module assembly (SimpleModule.Products) may declare [Module].`

Expected after cleanup: script passes again.

---

## Task 5: Add tools/ layering check

Tools live at `tools/SimpleModule.{Name}/` (flat). No `.cs` file under `tools/` declares `[Module]`. No `.csproj` under `modules/` references a `tools/` project.

Note: `tools/` may not exist yet at this point in the plan. The check must skip silently when the directory is missing.

**Files:**
- Modify: `scripts/validate-framework-scope.mjs`

- [ ] **Step 1: Implement `checkToolsLayering`**

Replace `function checkToolsLayering() {}` with:

```javascript
function checkToolsLayering() {
    const toolsDir = join(repoRoot, 'tools');
    if (exists(toolsDir)) {
        const entries = readdirSync(toolsDir, { withFileTypes: true });
        for (const entry of entries) {
            if (!entry.isDirectory()) continue;
            if (!entry.name.startsWith('SimpleModule.')) {
                errors.push(
                    `tools/${entry.name}/ does not match required naming 'SimpleModule.{Name}'.`,
                );
                continue;
            }
            const toolPath = join(toolsDir, entry.name);
            const files = walkCsFiles(toolPath);
            for (const file of files) {
                const content = readFileSync(file, 'utf8');
                if (/\[\s*Module\s*\(/.test(content)) {
                    errors.push(
                        `tools/${entry.name} declares [Module] in ${file.substring(repoRoot.length + 1)}. ` +
                            `Tools are not modules and must not declare [Module].`,
                    );
                }
            }
        }
    }
    // Check no module csproj references a tools/ project.
    const modulesDir = join(repoRoot, 'modules');
    if (!exists(modulesDir)) return;
    const moduleDirs = readdirSync(modulesDir, { withFileTypes: true })
        .filter((e) => e.isDirectory());
    for (const moduleEntry of moduleDirs) {
        const srcDir = join(modulesDir, moduleEntry.name, 'src');
        if (!exists(srcDir)) continue;
        const projectDirs = readdirSync(srcDir, { withFileTypes: true })
            .filter((e) => e.isDirectory());
        for (const projectEntry of projectDirs) {
            const csprojPath = join(srcDir, projectEntry.name, `${projectEntry.name}.csproj`);
            if (!exists(csprojPath)) continue;
            const content = readFileSync(csprojPath, 'utf8');
            // Match ProjectReference paths containing tools/ or tools\
            if (/ProjectReference[^>]*Include="[^"]*[\\/]tools[\\/]/.test(content)) {
                errors.push(
                    `${csprojPath.substring(repoRoot.length + 1)} references a tools/ project. ` +
                        `Modules may not depend on tools/ — tools are for host/framework only.`,
                );
            }
        }
    }
}
```

- [ ] **Step 2: Verify the check passes**

Run: `node scripts/validate-framework-scope.mjs`
Expected: `✓ Framework scope validation passed` (tools/ does not exist yet).

- [ ] **Step 3: Commit the script and allowlist**

```bash
git add framework/.allowed-projects scripts/validate-framework-scope.mjs
git commit -m "feat: add framework scope validation script and allowlist

Scaffolds the invariant that framework/ contains only foundational
plumbing. Script enforces four rules:
- framework/ directory allowlisted in framework/.allowed-projects
- sub-projects named SimpleModule.{Module}[.{Suffix}]
- sub-projects do not declare [Module]
- tools/ flat-layout, no [Module], not referenced from modules/

Allowlist starts permissive (all 19 current framework projects) and
shrinks as projects migrate to modules/ or tools/."
```

---

## Task 6: Wire validation into CI and `npm run check`

Two places run the check: GitHub Actions (always on PRs) and the local `npm run check` (developer feedback before push).

**Files:**
- Modify: `.github/workflows/ci.yml`
- Modify: `package.json`

- [ ] **Step 1: Add the step to the lint job in ci.yml**

Read `.github/workflows/ci.yml` and find the `lint:` job. Insert a new step after `Validate i18n keys` and before `TypeScript type check`:

```yaml
      - name: Validate framework scope
        run: node scripts/validate-framework-scope.mjs
```

The result (showing context):

```yaml
      - name: Validate i18n keys
        run: npm run validate:i18n

      - name: Validate framework scope
        run: node scripts/validate-framework-scope.mjs

      - name: TypeScript type check
        run: npm run typecheck
```

- [ ] **Step 2: Append the check to `npm run check`**

Find this line in `package.json`:

```json
"check": "biome check . && npm run validate-pages && npm run validate:i18n && npm run typecheck",
```

Replace with:

```json
"check": "biome check . && npm run validate-pages && npm run validate:i18n && npm run validate:framework-scope && npm run typecheck",
```

Add the new script entry to `package.json` scripts block (alphabetically near other `validate:*` entries):

```json
"validate:framework-scope": "node scripts/validate-framework-scope.mjs",
```

- [ ] **Step 3: Verify `npm run check` works**

Run: `npm run check`
Expected: all sub-checks pass, including `✓ Framework scope validation passed`. Exit 0.

If biome reports formatting issues on the newly created `scripts/validate-framework-scope.mjs`, run `npm run check:fix` to fix them.

- [ ] **Step 4: Commit CI wiring**

```bash
git add .github/workflows/ci.yml package.json
git commit -m "ci: enforce framework scope validation in CI and npm check

Adds validate-framework-scope to the lint job and to the local
npm run check pipeline so violations fail fast before PR review."
```

---

## Task 7: Add Constitution Section 13

Document the invariant in the authoritative rules file. Existing Constitution sections are numbered 1-12; this adds Section 13 at the end.

**Files:**
- Modify: `docs/CONSTITUTION.md`

- [ ] **Step 1: Read the end of the Constitution to confirm insertion point**

Run: `tail -20 docs/CONSTITUTION.md`
Expected: Section 12 "Framework Contributor Guidelines" ends the file. Note the last line (should be the end of that section's content).

- [ ] **Step 2: Append Section 13**

Append the following to `docs/CONSTITUTION.md` (keep trailing newline):

```markdown

---

## 13. Framework Scope

The `framework/` directory contains foundational plumbing: module lifecycle, source generation, DbContext infrastructure, and host bootstrap. Nothing else.

Framework projects are explicitly allowlisted in `framework/.allowed-projects`. The target list contains exactly: `SimpleModule.Core`, `SimpleModule.Database`, `SimpleModule.Generator`, `SimpleModule.Hosting`. During the in-flight migration, the list is temporarily permissive and shrinks as projects migrate.

### Adding a project to `framework/`

Requires:

1. Justification that the project is foundational — referenced by the host bootstrap or by every module, with no domain or provider semantics.
2. A PR that updates `.allowed-projects`, names the reviewer, and documents why a module or `tools/` project is insufficient.

### `tools/` category

The `tools/` directory holds non-module .NET utilities consumed by the host, the framework bootstrap, or other tools. Rules:

- Flat layout: `tools/SimpleModule.{Name}/{Name}.csproj` — no `src/` subdirectory, no Contracts split.
- Tools never declare `[Module]`.
- Modules (anything under `modules/`) never reference a `tools/` project. The host and `framework/SimpleModule.Hosting` may.

### Sub-projects

A sub-project is an additional assembly inside a module, used when a module owns multiple optional providers (e.g., `SimpleModule.Agents.AI.Anthropic`). Rules:

- Lives at `modules/{Name}/src/SimpleModule.{Name}.{Suffix}/`.
- Name matches `SimpleModule.{Name}.{Suffix}`.
- Does not declare `[Module]` — only the main module assembly owns lifecycle.
- May not own a `DbContext`.
- Follows the same dependency rules as its module (Section 3).

### Enforcement

`scripts/validate-framework-scope.mjs` runs in CI and in `npm run check`. It fails on any violation of the rules above.
```

- [ ] **Step 3: Verify the file is well-formed**

Run: `grep -c "^## " docs/CONSTITUTION.md`
Expected: `13` (one heading per Constitution section).

- [ ] **Step 4: Commit Constitution update**

```bash
git add docs/CONSTITUTION.md
git commit -m "docs: add Constitution Section 13 on Framework Scope

Documents the framework allowlist, tools/ category, sub-project
convention, and validation mechanism that enforces them."
```

---

## Task 8: Create `tools/` and move DevTools into it

Moves `framework/SimpleModule.DevTools/` to `tools/SimpleModule.DevTools/` via `git mv` to preserve history. DevTools does not have a Contracts split, so it drops straight into the flat `tools/` layout.

**Files:**
- Move: `framework/SimpleModule.DevTools/` → `tools/SimpleModule.DevTools/`

- [ ] **Step 1: Verify DevTools' current state**

Run: `ls framework/SimpleModule.DevTools/`
Expected: `.csproj`, `.cs` files, `README.md`, no Contracts subdirectory.

- [ ] **Step 2: Move the directory with git mv**

Run:
```bash
git mv framework/SimpleModule.DevTools tools/SimpleModule.DevTools
git status --short | head -20
```

Expected output contains rename entries:
```
R  framework/SimpleModule.DevTools/DevToolsConstants.cs -> tools/SimpleModule.DevTools/DevToolsConstants.cs
... (one line per file)
```

---

## Task 9: Update ProjectReference paths

Two `.csproj` files reference DevTools via relative path:
- `framework/SimpleModule.Hosting/SimpleModule.Hosting.csproj` — was `..\SimpleModule.DevTools\...`, now needs `..\..\tools\SimpleModule.DevTools\...`
- `tests/SimpleModule.DevTools.Tests/SimpleModule.DevTools.Tests.csproj` — was `..\..\framework\SimpleModule.DevTools\...`, now needs `..\..\tools\SimpleModule.DevTools\...`

**Files:**
- Modify: `framework/SimpleModule.Hosting/SimpleModule.Hosting.csproj`
- Modify: `tests/SimpleModule.DevTools.Tests/SimpleModule.DevTools.Tests.csproj`

- [ ] **Step 1: Update Hosting csproj**

Find the existing line in `framework/SimpleModule.Hosting/SimpleModule.Hosting.csproj`:

```xml
<ProjectReference Include="..\SimpleModule.DevTools\SimpleModule.DevTools.csproj" />
```

Replace with:

```xml
<ProjectReference Include="..\..\tools\SimpleModule.DevTools\SimpleModule.DevTools.csproj" />
```

- [ ] **Step 2: Update DevTools.Tests csproj**

Find the existing line in `tests/SimpleModule.DevTools.Tests/SimpleModule.DevTools.Tests.csproj`:

```xml
<ProjectReference Include="..\..\framework\SimpleModule.DevTools\SimpleModule.DevTools.csproj" />
```

Replace with:

```xml
<ProjectReference Include="..\..\tools\SimpleModule.DevTools\SimpleModule.DevTools.csproj" />
```

---

## Task 10: Update the solution file

`SimpleModule.slnx` lists every project. DevTools is currently under the `/framework/` folder. Move it to a new `/tools/` folder entry.

**Files:**
- Modify: `SimpleModule.slnx`

- [ ] **Step 1: Inspect the solution file structure**

Run: `grep -n -B1 -A1 "DevTools\|<Folder" SimpleModule.slnx | head -30`
Expected: reveals the `<Folder Name="/framework/">` section containing `DevTools` and any existing `<Folder Name="/tools/">` or similar.

- [ ] **Step 2: Remove the DevTools line from the framework folder**

Find this line inside the `<Folder Name="/framework/">` block:

```xml
    <Project Path="framework/SimpleModule.DevTools/SimpleModule.DevTools.csproj" />
```

Delete it.

- [ ] **Step 3: Add DevTools to a `/tools/` folder**

If a `<Folder Name="/tools/">` block does not exist, add one near the `/framework/` block. Example insertion:

```xml
  <Folder Name="/tools/">
    <Project Path="tools/SimpleModule.DevTools/SimpleModule.DevTools.csproj" />
  </Folder>
```

If a `<Folder Name="/tools/">` block already exists (unlikely, but check), add the `<Project>` line inside it.

---

## Task 11: Verify the move built cleanly

Before removing the allowlist entry and committing, confirm nothing is broken.

- [ ] **Step 1: Build the affected projects**

Run: `dotnet build framework/SimpleModule.Hosting tools/SimpleModule.DevTools tests/SimpleModule.DevTools.Tests 2>&1 | tail -5`

Expected: `Build succeeded.  0 Error(s)`.

If restore fails because of pre-existing MailKit NU1902 warnings in unrelated projects, add `-p:NoWarn=NU1902` and try again — those errors are out of scope (flagged in the spec).

- [ ] **Step 2: Run DevTools tests**

Run: `dotnet test tests/SimpleModule.DevTools.Tests 2>&1 | tail -8`

Expected: all tests pass.

- [ ] **Step 3: Run the validation script**

Run: `node scripts/validate-framework-scope.mjs`

Expected: **this should still pass**. DevTools is still in `.allowed-projects`, and `framework/SimpleModule.DevTools/` no longer exists so the allowlist permissiveness is fine. The `tools/` layering check sees `tools/SimpleModule.DevTools/` — flat layout, no `[Module]` — and passes.

If the script fails with "tools/SimpleModule.DevTools declares [Module]" — that would be a bug in DevTools (it shouldn't have one). Inspect the offending file and confirm it's a false positive (e.g., a comment or string containing `[Module(`) or a genuine issue.

---

## Task 12: Remove DevTools from the allowlist

Now that DevTools has moved out of `framework/`, the allowlist must no longer include it. This is the edit that makes the script ACTIVELY enforce the new state.

**Files:**
- Modify: `framework/.allowed-projects`

- [ ] **Step 1: Remove the DevTools line**

Delete the line `SimpleModule.DevTools` from `framework/.allowed-projects`.

Verify with:

Run: `grep -c "^SimpleModule\." framework/.allowed-projects`
Expected: `17` (was 18, now 17).

- [ ] **Step 2: Run the validation script**

Run: `node scripts/validate-framework-scope.mjs`
Expected: `✓ Framework scope validation passed` (exit 0). framework/ no longer contains DevTools, and the allowlist no longer lists it. Consistent.

- [ ] **Step 3: Prove the guard now catches regression**

Temporarily restore DevTools to framework/ to confirm the allowlist rejects it:

```bash
mkdir -p framework/SimpleModule.DevTools
node scripts/validate-framework-scope.mjs
echo "Exit: $?"
rmdir framework/SimpleModule.DevTools
```

Expected (exit 1): `framework/SimpleModule.DevTools is not in framework/.allowed-projects. ...`

Expected after cleanup: `✓ Framework scope validation passed`.

---

## Task 13: Final commit for Phase 1

- [ ] **Step 1: Run `npm run check`**

Run: `npm run check`
Expected: all sub-checks pass.

- [ ] **Step 2: Commit the DevTools move**

```bash
git add -A
git status --short
git commit -m "refactor: move DevTools from framework/ to tools/

First application of the tools/ category. DevTools is a dev-time
utility (Vite dev middleware, live reload, file watchers), not
foundational plumbing, so it belongs in tools/ rather than framework/.

- git mv framework/SimpleModule.DevTools tools/SimpleModule.DevTools
- Updated ProjectReference paths in Hosting and DevTools.Tests
- Updated SimpleModule.slnx (moved under new /tools/ folder)
- Removed SimpleModule.DevTools from framework/.allowed-projects

Framework is now down to 17 allowlisted projects; phases 2-4 will
absorb the remaining provider projects into their owning modules."
```

- [ ] **Step 3: Verify final state**

Run: `git log --oneline -5`
Expected: the most recent commits are (newest first):
1. `refactor: move DevTools from framework/ to tools/`
2. `docs: add Constitution Section 13 on Framework Scope`
3. `ci: enforce framework scope validation in CI and npm check`
4. `feat: add framework scope validation script and allowlist`

Run: `ls framework/ | sort`
Expected: 18 directory entries (was 19, DevTools removed) + `Directory.Build.props`.

Run: `ls tools/`
Expected: `SimpleModule.DevTools` (the only entry).

---

## Summary

After this plan lands:

- `framework/.allowed-projects` explicitly lists every permitted framework project (now 17).
- `scripts/validate-framework-scope.mjs` enforces four rules on every CI run and every local `npm run check`.
- Constitution Section 13 documents the invariant.
- DevTools has migrated from `framework/` to `tools/`, proving the `tools/` category works end to end.
- Phases 2, 3, 4 (Storage, Agents, Rag absorptions) can now proceed, each with its own plan, each shrinking the allowlist by 4-5 entries.
