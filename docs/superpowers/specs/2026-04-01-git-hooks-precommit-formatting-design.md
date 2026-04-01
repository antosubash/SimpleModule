# Pre-commit Formatting via Git Hooks

**Date:** 2026-04-01
**Status:** Approved

## Problem

Code formatting is not enforced at commit time. Developers can commit unformatted JS/TS or C# code, leading to noisy diffs and inconsistent style.

## Solution

Add a pre-commit git hook using **Husky + lint-staged** that automatically formats staged files before each commit:

- **JS/TS/CSS/JSON** files: formatted and lint-fixed via Biome (`biome check --write`)
- **C# (.cs)** files: formatted via CSharpier (`dotnet csharpier <file args>`)

## Components

### 1. CSharpier (dotnet local tool)

- Register in `.config/dotnet-tools.json` so `dotnet csharpier` works without global install
- Add `.csharpierrc.yaml` at repo root with default settings

### 2. Husky

- npm devDependency
- `"prepare": "husky"` script in root `package.json` auto-installs hooks on `npm install`
- `.husky/pre-commit` script runs `npx lint-staged`

### 3. lint-staged

- npm devDependency
- Configuration in root `package.json` under `"lint-staged"` key:
  - `"*.{js,jsx,ts,tsx,css,json}"` → `"biome check --write --no-errors-on-unmatched"`
  - `"*.cs"` → `"dotnet csharpier"`

### 4. Makefile updates

- Update `format-cs` and `lint-cs` targets to use `dotnet csharpier` (local tool) instead of bare `csharpier`
- Add `dotnet tool restore` to the `setup` target so local tools are restored alongside npm/dotnet packages

## Developer Experience

- **On `npm install`**: Husky's `prepare` script installs the pre-commit hook automatically
- **On `git commit`**: lint-staged runs formatters on staged files only, re-stages them, then commits
- **On format failure**: commit is blocked with clear error output
- **Manual formatting**: `make format` (already exists) runs both Biome and CSharpier on the whole repo
- **New clones**: `dotnet tool restore && npm install` sets up everything

## Notes

- **Bypass**: `git commit --no-verify` skips the pre-commit hook when needed (e.g., WIP commits)
- **Partial staging**: lint-staged v15+ handles `git add -p` safely via stashing
- **Source-generated files**: Not an issue — lint-staged only passes staged files, and generated files in `obj/` are gitignored

## What Does NOT Change

- Existing `npm run check` / `npm run check:fix` commands
- CI pipeline (local-only enforcement)
- biome.json configuration
- .editorconfig conventions
