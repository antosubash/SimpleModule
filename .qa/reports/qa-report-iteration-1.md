# QA Report: Module packaging (manifest emission, manifest-driven loading, bundled migrations, sm CLI)
**Date:** 2026-06-10 · **Tester:** Claude QA (Senior) · **Target:** https://localhost:5003 + sm CLI · **Depth:** normal · **Iteration:** 1 of 3

## Summary
| Category | Passed | Failed |
|----------|--------|--------|
| Browser (happy path + edge) | 13 | 1 (P3) |
| CLI packaging commands | 14 | 1 (P3) |
| **Total** | **27** | **2 (both P3)** |

## Critical/Major/Minor issues (P0–P2)
None.

## Observations (P3)
### [OBS-001] /admin/audit-logs 404s — actual route is /audit-logs/browse
App behaves correctly (styled 404); the QA inventory route was stale. Optional: add an alias.
### [OBS-002] sm publish success line showed the [Module] manifest version instead of the requested package version
Fixed inline during QA (PackCommand success line now prints the package version).

## Feature verdicts
- **Manifest-driven frontend loading: VERIFIED** — sm-module-assets present exactly once, 13 modules mapped, every module page renders, bundle-cache navigation works, zero console errors, graceful 404s.
- **Bundled module migrations: VERIFIED** — FeatureFlags_FeatureFlagChangeLog created via MigrateAsync at startup (schema-prefix convention respected), history row recorded; doctor reports "1 migration(s) applied".
- **sm CLI: VERIFIED** — all 14 adversarial scenarios pass with correct exit codes, clean errors, zero unintended writes (byte-identical csproj/props after refused add), registry abstraction honored, dry-run side-effect-free.
