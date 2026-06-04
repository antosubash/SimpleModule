# Downstream-blocking bug fixes (#236, #220, #222, #223, #224)

Goal: make the framework consumable by a downstream host again. One branch, one commit per issue, one PR closing all five (follows PR #231 precedent).

## Plan

- [ ] **#223** RateLimitRuleCache crashes host on missing `RateLimiting_Rules` table
  - [ ] Failing test: cache `RefreshAsync` against a context whose table was NOT created → must not throw, `FindForPath` returns null
  - [ ] Fix: `catch (DbException)` in `RefreshAsync` (provider-agnostic), log warning, keep empty rules
  - [ ] Add `ConfigureHost` to `RateLimitingModule` to create the table on legacy EnsureCreated DBs (mirror BackgroundJobs)
- [ ] **#222** auth-strict policy missing when RateLimiting not installed → login 500
  - [ ] Failing test: host pipeline without RateLimiting module, assert `auth-strict` policy resolvable
  - [ ] Fix: seed framework-default `auth-strict` in `RateLimitingSetup.AddSimpleModuleRateLimiting` only if absent
- [ ] **#224** Inertia resolver 404s on `/_content/<ShortName>/` before `SimpleModule.<Module>`
  - [ ] Fix: reverse candidate order in `packages/SimpleModule.Client/src/resolve-page.ts`
  - [ ] Check/adjust any resolve-page tests
- [ ] **#236 / #220** SM0026 (dup impl) + SM0028 (internal impl) make framework unconsumable
  - [ ] Add `[ManualContractRegistration]` attribute to SimpleModule.Core
  - [ ] Generator: record flag in ContractFinder; skip SM0025/0026/0028 + auto-registration for manual impls; contract still counts as satisfied
  - [ ] Generator unit test: two manual impls of one contract → no SM0026/0028, no auto-register, no SM0025
  - [ ] Mark the 8 provider-swappable impls (Users/OpenIddict) with the attribute
  - [ ] Verify Host build clean; confirm SM0028 from Users/OpenIddict gone

## Verification
- [ ] `dotnet build` clean
- [ ] `dotnet test` for touched modules + generator
- [ ] `npm run check` for resolver change
- [ ] Full local CI before PR

## Review

All five downstream-blocking bugs fixed, one commit each, all CI green.

- **#223** — `RefreshAsync` now catches `DbException` (provider-agnostic) and `RateLimitingModule.ConfigureHost` ensures the table exists. Regression test added. RateLimiting tests 28/28.
- **#222** — `RateLimitDefaults.EnsureFrameworkDefaults` (Core) seeds `auth-strict` in `AddSimpleModuleRateLimiting` (always emitted by the generator) only if absent; module definitions still win. Core tests +2.
- **#236 / #220** — `[ManualContractRegistration]` (Core); generator skips auto-registration + SM0026/SM0028 for marked impls while keeping the contract satisfied (no SM0025). Applied to the 8 Users/OpenIddict provider-swappable impls (they stay `internal`). Verified: with SM0028 un-suppressed, the 7 Users/OpenIddict errors are gone (only the pre-existing #97 BackgroundJobs `DefaultJobExecutionContext` remains — out of scope). Generator tests +2 (208 total).
- **#224** — resolver tries `SimpleModule.<Module>` first, then bare name; caches resolved assembly per module. Frontend check green; e2e smoke 66/66.

Notes / follow-ups:
- **#97** (`DefaultJobExecutionContext` internal → SM0028) is the same class of issue and could use `[ManualContractRegistration]`; left out of scope here. Once fixed, `SM0028` can be removed from the Host's `NoWarn` to guard against regressions.

## Verification (done)
- [x] `npm run check` — green (biome, validate-pages, i18n, framework-scope, typecheck 13/13)
- [x] `npm run build` — all module bundles built
- [x] `dotnet build` — 0 errors
- [x] `dotnet test --no-build` — 0 failures across all projects
