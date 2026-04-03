Run all CI checks locally, mirroring the GitHub Actions CI pipeline. This catches issues before creating a PR.

Execute these steps in order, stopping on first failure. Report results as a summary table at the end.

## Step 1: Lint & Format Check

Run `npm run check` from the project root. This runs biome lint, format check, page validation, and i18n validation.

## Step 2: Frontend Build

Run `npm run build` from the project root. This builds all module frontend assets via Vite in production mode.

## Step 3: .NET Build

Run `dotnet build` from the project root. This compiles all .NET projects including source generators.

## Step 4: .NET Tests

Run `dotnet test --no-build` from the project root. This runs all unit and integration tests (excluding load tests which are filtered out in CI).

## Step 5: E2E Smoke Tests

Run `npm run test:smoke -w tests/e2e` to execute Playwright smoke tests. Playwright browsers are already installed.

## Reporting

After all steps complete (or on first failure), print a summary table:

| Step | Status |
|------|--------|
| Lint & Format | pass/fail |
| Frontend Build | pass/fail |
| .NET Build | pass/fail |
| .NET Tests | pass/fail |
| E2E Smoke Tests | pass/fail |

If any step fails, show the relevant error output and suggest a fix.
