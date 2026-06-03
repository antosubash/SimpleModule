# Data-correctness / concurrency bug fixes (#232, #230, #227, #233) [+ #229 separate]

Branch: fix/data-correctness-bugs (off main). One commit per issue.

## Plan
- [ ] **#230** `EntityInterceptor` overwrites `CreatedBy` from HttpContext → background jobs can't create user-owned rows
  - [ ] Failing test: pre-set `CreatedBy` with no HTTP user is preserved
  - [ ] Fix: only set `CreatedBy` when unset; guard `CreatedAt == default`
- [ ] **#227** `ApplyModuleSchema` ignores `DatabaseOptions.Provider`
  - [ ] Failing test: explicit Provider overrides SQLite-looking connection string
  - [ ] Fix: pass `dbOptions.Provider` + module's effective connection to `Detect`
- [ ] **#232** `AuditMiddleware` races `SettingsDbContext` (Task.WhenAll over 5 reads) → 500s
  - [ ] Failing test: concurrency-guard stub `ISettingsContracts` (fails if >1 in-flight)
  - [ ] Fix: read the 5 settings sequentially
- [ ] **#233** live-reload WS 302→infinite retry
  - [ ] Fix: `.AllowAnonymous()` on `/dev/live-reload` (root cause); + client give-up cap
- [ ] CI green → PR

## Deferred to its own PR
- **#229** child entity (no DbSet) gets wrong/no schema prefix in generated HostDbContext — latent (no in-tree repro); generator change emitting EF-model traversal, higher risk. Handle carefully after this PR.

## Review

Four fixes, one commit each, all CI green (build 0 errors, tests 0 failures, npm check + e2e 66/66).

- **#230** `EntityInterceptor.SetCreationFields` now only defaults `CreatedBy`/`CreatedAt` when unset → background jobs can set an owner. +2 tests (Database 89).
- **#227** `ApplyModuleSchema` passes `dbOptions.Provider` to `Detect` → explicit provider wins. +1 test.
- **#232** `AuditMiddleware` reads its 5 settings sequentially (was `Task.WhenAll` on one scoped DbContext → races). +1 concurrency-guard test (AuditLogs 38).
- **#233** `/dev/live-reload` endpoint `.AllowAnonymous()` (root cause: auth fallback 302'd the handshake) + client gives up after 60 attempts. +1 endpoint-metadata test (DevTools 35).

Deferred: **#229** (child entity schema prefix in generated HostDbContext) — latent, riskier generator change; its own PR.

## Verification (done)
- [x] `dotnet build` — 0 errors
- [x] `dotnet test --no-build` — 0 failures
- [x] `npm run check` — green; typecheck 13/13
- [x] `npm run build` — clean
- [x] `npm run test:smoke -w tests/e2e` — 66 passed
