# Pre-commit Formatting via Git Hooks — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Auto-format staged JS/TS/CSS/JSON (Biome) and C# (CSharpier) files on every git commit via a pre-commit hook.

**Architecture:** Husky manages the git hook lifecycle, lint-staged runs formatters only on staged files. CSharpier is installed as a dotnet local tool. Existing Makefile targets updated to use the local tool.

**Tech Stack:** Husky 9.x, lint-staged 16.x, CSharpier 1.2.x (dotnet local tool), Biome (already installed)

**Spec:** `docs/superpowers/specs/2026-04-01-git-hooks-precommit-formatting-design.md`

---

## Chunk 1: CSharpier Setup

### Task 1: Install CSharpier as a dotnet local tool

**Files:**
- Modify: `.config/dotnet-tools.json`

- [ ] **Step 1: Add CSharpier to dotnet local tools manifest**

Edit `.config/dotnet-tools.json` to add CSharpier:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "simplemodule.cli": {
      "version": "1.0.0",
      "commands": [
        "sm"
      ]
    },
    "csharpier": {
      "version": "1.2.6",
      "commands": [
        "dotnet-csharpier"
      ]
    }
  }
}
```

- [ ] **Step 2: Restore tools and verify CSharpier works**

Run: `dotnet tool restore`
Expected: Successfully restored `csharpier`.

Run: `dotnet csharpier --version`
Expected: `1.2.6`

- [ ] **Step 3: Commit**

```bash
git add .config/dotnet-tools.json
git commit -m "chore: add CSharpier as dotnet local tool"
```

### Task 2: Add CSharpier configuration

**Files:**
- Create: `.csharpierrc.yaml`

- [ ] **Step 1: Create `.csharpierrc.yaml` at repo root**

```yaml
printWidth: 100
```

This aligns with the Biome `lineWidth: 100` in `biome.json`.

- [ ] **Step 2: Verify CSharpier respects config**

Run: `dotnet csharpier --check framework/SimpleModule.Core/IModule.cs`
Expected: exits 0 (already formatted) or exits 1 (needs formatting) — either is fine, just confirming the tool runs.

- [ ] **Step 3: Commit**

```bash
git add .csharpierrc.yaml
git commit -m "chore: add CSharpier configuration (printWidth: 100)"
```

---

## Chunk 2: Husky + lint-staged Setup

### Task 3: Install Husky and lint-staged

**Files:**
- Modify: `package.json` (devDependencies, scripts, lint-staged config)
- Create: `.husky/pre-commit`

- [ ] **Step 1: Install npm dependencies**

Run: `npm install --save-dev husky@^9 lint-staged@^16`
Expected: Both packages added to `devDependencies` in `package.json`.

- [ ] **Step 2: Add `prepare` script to `package.json`**

Add to `"scripts"` in `package.json`:

```json
"prepare": "husky"
```

- [ ] **Step 3: Add lint-staged configuration to `package.json`**

Add top-level `"lint-staged"` key to `package.json`:

```json
"lint-staged": {
  "*.{js,jsx,ts,tsx,css,json}": "biome check --write --no-errors-on-unmatched",
  "*.cs": "dotnet csharpier"
}
```

- [ ] **Step 4: Initialize Husky**

Run: `npx husky`
Expected: Creates `.husky/` directory.

- [ ] **Step 5: Create the pre-commit hook**

Create `.husky/pre-commit` with contents:

```bash
npx lint-staged
```

- [ ] **Step 6: Make the hook executable**

Run: `chmod +x .husky/pre-commit`

- [ ] **Step 7: Verify Husky hook is installed**

Run: `cat .husky/pre-commit`
Expected: Shows `npx lint-staged`.

Run: `ls -la .husky/pre-commit`
Expected: Executable permissions.

- [ ] **Step 8: Commit**

```bash
git add package.json package-lock.json .husky/pre-commit
git commit -m "chore: add Husky + lint-staged for pre-commit formatting"
```

---

## Chunk 3: Makefile Updates

### Task 4: Update Makefile to use dotnet local tool

**Files:**
- Modify: `Makefile`

- [ ] **Step 1: Update `lint-cs` target**

Change from:
```makefile
lint-cs: ## Run CSharpier format check on C#
	csharpier check .
```

To:
```makefile
lint-cs: ## Run CSharpier format check on C#
	dotnet csharpier check .
```

- [ ] **Step 2: Update `format-cs` target**

Change from:
```makefile
format-cs: ## Run CSharpier formatter (writes changes)
	csharpier format .
```

To:
```makefile
format-cs: ## Run CSharpier formatter (writes changes)
	dotnet csharpier format .
```

- [ ] **Step 3: Update `check` target**

Change from:
```makefile
check: ## Run Biome check + CSharpier check + page validation
	npm run check
	csharpier check .
```

To:
```makefile
check: ## Run Biome check + CSharpier check + page validation
	npm run check
	dotnet csharpier check .
```

- [ ] **Step 4: Update `check-fix` target**

Change from:
```makefile
check-fix: ## Auto-fix Biome + CSharpier formatting issues
	npm run check:fix
	csharpier format .
```

To:
```makefile
check-fix: ## Auto-fix Biome + CSharpier formatting issues
	npm run check:fix
	dotnet csharpier format .
```

- [ ] **Step 5: Add `dotnet tool restore` to setup target**

Change from:
```makefile
setup: restore install ## Full project setup (dotnet restore + npm install)
```

To:
```makefile
setup: restore tool-restore install ## Full project setup (dotnet restore + tool restore + npm install)

.PHONY: tool-restore
tool-restore: ## Restore dotnet local tools (CSharpier, sm CLI)
	dotnet tool restore
```

- [ ] **Step 6: Verify Makefile targets work**

Run: `make format-cs` (on a single file or small directory to test)
Expected: CSharpier runs via dotnet local tool.

Run: `make lint-cs`
Expected: CSharpier check runs via dotnet local tool.

- [ ] **Step 7: Commit**

```bash
git add Makefile
git commit -m "chore: update Makefile to use dotnet local tool for CSharpier"
```

---

## Chunk 4: End-to-End Verification

### Task 5: Verify pre-commit hook works end-to-end

- [ ] **Step 1: Test JS formatting hook**

Create a deliberately unformatted test:
```bash
echo 'const   x   =    1;' > /tmp/test-hook.ts
cp /tmp/test-hook.ts modules/Products/src/SimpleModule.Products/test-hook.ts
git add modules/Products/src/SimpleModule.Products/test-hook.ts
git commit -m "test: verify pre-commit hook"
```

Expected: lint-staged runs Biome, formats the file, commit succeeds with formatted code.

- [ ] **Step 2: Verify the file was formatted**

Run: `cat modules/Products/src/SimpleModule.Products/test-hook.ts`
Expected: `const x = 1;` (properly formatted).

- [ ] **Step 3: Clean up test file**

```bash
git rm modules/Products/src/SimpleModule.Products/test-hook.ts
git commit -m "chore: remove pre-commit hook test file"
```

- [ ] **Step 4: Test C# formatting hook**

Stage a .cs file that's already in the repo:
```bash
git add framework/SimpleModule.Core/IModule.cs
git commit --allow-empty -m "test: verify CSharpier hook"
```

Expected: lint-staged runs CSharpier on the staged .cs file, commit succeeds.

- [ ] **Step 5: Final verification — `make format` still works**

Run: `make format`
Expected: Both `format-js` and `format-cs` run successfully using the local tools.
