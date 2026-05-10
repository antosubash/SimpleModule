---
name: verify-feature
description: End-to-end verification of a feature implementation before opening a PR. Starts the SimpleModule.Host (killing port 5001 if occupied), drives the feature in a real browser via playwright-cli, runs every local CI step, and only then opens a pull request. Use when the user asks to "verify the feature", "test and ship", "run e2e + CI + PR", or any variation that means "prove it works, then PR it".
allowed-tools: Bash, Read, Edit, Write
---

# verify-feature

Run this skill after a feature is implemented and committed locally. It is a gated pipeline — each stage must pass before the next runs. If any stage fails, stop, surface the failure, and do NOT open a PR.

## Inputs to gather first

Before starting, confirm these from conversation context (do not ask the user unless missing):

- **Feature description** — what was implemented (used to pick the page and assertions for stage 2)
- **Page route** — the Inertia route (e.g. `Products/Browse`) or URL path to exercise
- **Branch** — current branch (must not be `main`)

If route/path is unknown, grep the diff for `Inertia.Render("...")` calls to infer it.

## Stage 1 — Start the application

The host listens on `https://localhost:5001` (see `CLAUDE.md`). Free the port if held, then start the app in the background.

```bash
# 1. Kill anything bound to 5001 (TCP). lsof returns nothing if the port is free.
PORT_PIDS=$(lsof -ti tcp:5001 2>/dev/null || true)
if [ -n "$PORT_PIDS" ]; then
  echo "Port 5001 occupied by PID(s): $PORT_PIDS — killing"
  kill -9 $PORT_PIDS
  sleep 1
fi

# 2. Start the host. Use Bash with run_in_background=true so the process keeps running.
dotnet run --project template/SimpleModule.Host
```

Run the `dotnet run` command via Bash with `run_in_background: true`. Capture the shell ID — you will need it to stop the app later.

**Wait for readiness.** Poll the health endpoint with curl (`-k` because the cert is self-signed) until it returns 200, then continue. Cap the wait at 90 seconds.

```bash
for i in $(seq 1 45); do
  if curl -sk -o /dev/null -w "%{http_code}" https://localhost:5001/ | grep -qE '^(200|302|401)$'; then
    echo "App is up"; break
  fi
  sleep 2
done
```

If readiness times out, read the background shell's output, surface the error, and abort the skill.

## Stage 2 — Verify the feature with playwright-cli

Use the `playwright-cli` skill's commands directly (already documented in `.claude/skills/playwright-cli/SKILL.md`). The cert is self-signed; open with `--ignore-https-errors` via the run-code escape if needed, otherwise just navigate — `playwright-cli` accepts self-signed certs by default.

Pattern:

```bash
playwright-cli open https://localhost:5001/<route-path>
playwright-cli snapshot                     # confirm the page rendered
# drive the feature: click, fill, submit, etc.
playwright-cli click eN
playwright-cli snapshot                     # confirm post-interaction state
playwright-cli console                      # check for client errors
playwright-cli close
```

**Assertions to make from snapshots and console:**

1. The expected page route is in the URL.
2. Key UI affordances from the feature are present in the snapshot (form, button, table row, etc.).
3. After exercising the feature (submit/save/etc.), the resulting state is correct (success toast, new row, navigation).
4. `playwright-cli console` returns no `error`-level entries related to the feature.

If any assertion fails, stop and report. Do not proceed to CI.

## Stage 3 — Stop the app before CI

CI's `npm run build` + `dotnet build` will conflict with the running host. Kill the background shell now.

```bash
PORT_PIDS=$(lsof -ti tcp:5001 2>/dev/null || true)
[ -n "$PORT_PIDS" ] && kill -9 $PORT_PIDS
```

Also call `KillShell` on the background Bash shell from stage 1.

## Stage 4 — Run local CI

Execute the steps from `.claude/commands/ci.md` in order. Stop on first failure.

```bash
npm run check          # biome + page/i18n validation
npm run build          # production frontend build
dotnet build           # .NET build
dotnet test --no-build # unit + integration tests (load tests excluded by CI filter)
npm run test:smoke -w tests/e2e   # Playwright smoke tests
```

Print this table at the end of stage 4:

| Step | Status |
|------|--------|
| Lint & Format | pass/fail |
| Frontend Build | pass/fail |
| .NET Build | pass/fail |
| .NET Tests | pass/fail |
| E2E Smoke Tests | pass/fail |

If any step fails, surface the relevant error output, suggest a fix, and abort — do not open a PR.

## Stage 5 — Create the PR

Only reached when stages 1–4 all passed.

1. Check there are commits ahead of `main`:

   ```bash
   git log --oneline main..HEAD
   ```

   If empty, stop — there is nothing to PR.

2. Push the branch:

   ```bash
   git push -u origin HEAD
   ```

3. Open the PR with `gh`, using a HEREDOC for the body. Follow the project's commit message style (see `git log`).

   ```bash
   gh pr create --title "<concise title, <70 chars>" --body "$(cat <<'EOF'
   ## Summary
   - <what changed>
   - <why>

   ## Verification
   - Manually exercised <feature> at https://localhost:5001/<route> via playwright-cli
   - All local CI steps passed (lint, frontend build, .NET build, tests, smoke tests)

   ## Test plan
   - [ ] CI green on PR
   - [ ] Reviewer spot-checks <area>
   EOF
   )"
   ```

4. Return the PR URL.

## Hard rules

- **Never** add `Co-authored-by: Claude` or any AI attribution to the commit, PR body, or PR title (see `CLAUDE.md` Attribution Policy).
- **Never** force-push, never push to `main`, never use `--no-verify`.
- **Stop on first failure.** Do not paper over a broken stage to get to the PR.
- **The PR step is gated.** If stages 1–4 didn't all pass, surface the failure instead and exit.
- Before killing PIDs on port 5001, confirm they're processes you started (or that the user expects to be killable) — don't blindly kill an unrelated long-lived process if the user has something else bound there.
