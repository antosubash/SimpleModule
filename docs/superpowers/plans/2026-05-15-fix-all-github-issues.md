# Fix All Open GitHub Issues — Master Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to dispatch one subagent per issue. Each issue is its own self-contained PR. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close all 17 open GitHub issues by shipping a feature PR per issue, in priority order.

**Architecture:** Each issue → one branch, one PR, one merge. Scope is held to the issue's acceptance criteria. Tests, docs, and Constitution updates land alongside each change. Branches are isolated; PRs are small and reviewable.

**Tech Stack:** .NET 10 / ASP.NET Core, React 19 + Inertia.js, EF Core, Serilog, FluentValidation, Roslyn source generators, xUnit.v3 + FluentAssertions, NBomber, Biome, Vite.

---

## Scope check

The 17 open issues span: identity hardening (6), framework primitives (2), tier-1 ops features (2), tier-3 observability modules (4), tier-4 SaaS/devx modules (3). Each issue is its own subsystem. Per the writing-plans guidance, **one plan per subsystem**. This master plan acts as the index: it orders the issues, calls out cross-cutting dependencies, and points to per-issue sub-plans created on demand as each is started.

## Priority order

The order reflects (a) tier labels in the issues, (b) cross-issue dependencies, and (c) risk/value ratio:

| Order | Issue | Tier | Effort | Why this rank |
|------:|-------|------|--------|---------------|
|  1 | #160 Maintenance mode (`sm down`/`sm up`) | T1 | S | Smallest tier-1 win; unblocks safer deploys for everything that follows. |
|  2 | #159 Task scheduler on top of BackgroundJobs | T1 | M | Required by #167 (Horizon dashboard "Recurring" page) and many later cron-style features. |
|  3 | #199 Identity resend cooldown + verification throttling | sec | S | Direct security follow-up to PR #198; small surface, high value. |
|  4 | #180 Recovery codes status & download | identity | S | Smallest identity UX win; pure additive UI. |
|  5 | #177 Authentication tokens API | identity | M | Foundation for #182 (which references stored tokens for "API access" badge). |
|  6 | #182 Enrich external logins UI | identity | M | Builds on #177 token visibility + needs `ExternalLoginMetadata` table. |
|  7 | #175 Admin user claims management | identity/admin | M | Pairs with #176; ship together but landable independently. |
|  8 | #176 Admin role claims management | identity/admin | M | Pair of #175; same patterns, same review surface. |
|  9 | #163 Form Request classes | T2/framework | L | Source-generator + validation foundation; many later modules will adopt it. |
| 10 | #162 Policy classes | T2/framework | L | Entity-authorization layer over Permissions; orthogonal to Form Requests. |
| 11 | #173 `sm tail` log viewer | T4/cli | S | Smaller CLI feature; useful while developing larger modules below. |
| 12 | #172 `sm tinker` REPL | T4/cli | M | CLI dev tool; benefits from #163/#162 already in place. |
| 13 | #167 Horizon-style jobs dashboard | T3 | L | Now depends on #159 (Recurring tab). Lives under Admin module. |
| 14 | #166 Telescope-style debug panel | T3/module | L | Standalone module; reuses #167 interceptor pattern. |
| 15 | #168 Pulse-style perf dashboard | T3/module | L | Aggregates the same signals as #166 but stores trends. |
| 16 | #170 Scout-style search + Meilisearch driver | T3/module | XL | Cross-cuts every searchable module; biggest framework primitive remaining. |
| 17 | #171 Stripe billing module (Cashier-equivalent) | T4/module | XL | Largest single deliverable; isolated; ship last. |

**Effort key:** S ≈ 0.5 day, M ≈ 1–2 days, L ≈ 3–5 days, XL ≈ 1–2 weeks.

## Execution protocol

Per issue:

- [ ] **Branch off `main`** with name `issue-<n>-<slug>` (e.g. `issue-160-maintenance-mode`).
- [ ] **Write a per-issue sub-plan** under `docs/superpowers/plans/2026-05-15-issue-<n>-<slug>.md` using `superpowers:writing-plans`. Use the issue's "Acceptance criteria" verbatim as the spec.
- [ ] **Execute via subagents** (per memory: `prefer-subagent-driven-execution`).
- [ ] **CI green** locally via the `ci` skill before opening the PR.
- [ ] **PR opens with `Closes #<n>`** in the body so the issue auto-closes on merge.
- [ ] **Update this file's checklist** when the PR opens and again when it merges.

## Cross-cutting dependencies (build sequence reasoning)

- **#159 before #167** — Horizon dashboard's "Recurring" page renders scheduled jobs registered via `IScheduler`. Doing #167 first ships an empty tab.
- **#177 before #182** — External-logins UI shows an "API access" badge sourced from `IExternalProviderTokenStore`. Implement the store first.
- **#175/#176 together** — Same `Claims` tab pattern, same `RoleClaim<>/UserClaim<>` mechanics, same `permission`-claim filter. One reviewer pass covers both.
- **#163/#162 before #166/#168/#170** — Larger T3 modules expose endpoints that benefit from FormRequest binding and resource policies. Adopting them after the modules exist requires churn.
- **#170 stays late** — Search interceptor touches every `ISearchable` entity; landing it after the other T3 modules avoids re-indexing churn.
- **#171 last** — Standalone module, hardest to test, biggest scope; no other issue depends on it.

## Issue checklist (master)

Mark each row when the corresponding PR is merged.

- [ ] #160 — Maintenance mode
- [ ] #159 — Task scheduler
- [ ] #199 — Resend cooldown + throttling
- [ ] #180 — Recovery codes status & download
- [ ] #177 — Authentication tokens API
- [ ] #182 — External logins UI enrichment
- [ ] #175 — Admin user claims
- [ ] #176 — Admin role claims
- [ ] #163 — Form Request classes
- [ ] #162 — Policy classes
- [ ] #173 — `sm tail`
- [ ] #172 — `sm tinker`
- [ ] #167 — Jobs (Horizon) dashboard
- [ ] #166 — Telescope debug panel
- [ ] #168 — Pulse perf dashboard
- [ ] #170 — Scout / Meilisearch
- [ ] #171 — Stripe billing module

## Self-review notes

- Spec coverage: every open issue is in the table above with a tier and dependency note.
- No placeholders: per-issue sub-plans are explicitly deferred to issue-start, not stubbed here.
- Type consistency: the master plan only references identifiers from the issues themselves (e.g. `IScheduler`, `IExternalProviderTokenStore`); each per-issue sub-plan owns its own type design.

## What this plan does NOT do

- Does **not** attempt to fix all 17 in a single session — each is a separate PR with its own review cycle.
- Does **not** lock the sub-plan content in advance — those are written when an issue is started so they reflect the latest code state.
- Does **not** include code in this file — per-issue plans hold the TDD steps.
