# Vertical-Slice Feature Folders — Notifications Pilot Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the reusable folder-migration tooling (Phase 0) and convert the Notifications module to the vertical-slice feature-folder shape end-to-end (Phase 1), proving the pattern before any other module is touched.

**Architecture:** A Node script (`scripts/feature-folder-migrate.mjs`) consumes a per-module TSV manifest of `OLD_PATH<TAB>NEW_PATH` pairs, performs `git mv` for each pair, and rewrites the `namespace ...;` declaration in each moved `.cs` file based on the project root and the new path. Call-site `using` updates are handled manually by following compiler errors after each move batch (small per-module blast radius — Notifications has zero cross-module `using` consumers). The Notifications module's runtime contract `NotificationService` becomes a `public sealed partial class` whose root fragment (constructor + `db` field) lives at `Infrastructure/NotificationService.cs` and whose five operation methods each live next to their endpoint at `Features/Notifications/<Op>/NotificationService.<Op>.cs`.

**Tech Stack:** .NET 10, EF Core, xUnit.v3, FluentAssertions, Node 22+ (for migration script), git.

**Spec:** `docs/superpowers/specs/2026-05-14-vertical-slice-feature-folders-design.md`. Authority on conventions C1–C8 and on what is/isn't in scope.

---

## Pre-flight check

Before starting Task 1, confirm you are in the worktree at `/root/github/SimpleModule/.claude/worktrees/explore-feature-folders` on branch `worktree-explore-feature-folders` and that the spec commit (`6b975b75`) is present:

```bash
git rev-parse --abbrev-ref HEAD
# Expected: worktree-explore-feature-folders
git log --oneline -3
# Expected to include: 6b975b75 docs: design spec for vertical-slice feature folders refactor
```

If either check fails, stop and reconcile before continuing.

---

## File structure

### New files this plan creates

| Path | Purpose |
|---|---|
| `scripts/feature-folder-migrate.mjs` | Migration tool: reads manifest, performs `git mv` + namespace rewrite |
| `scripts/feature-folder-migrate.test.mjs` | Node `node:test` suite for the migrate tool |
| `scripts/manifests/notifications.tsv` | Move manifest for the Notifications pilot |
| `modules/Notifications/src/SimpleModule.Notifications.Contracts/Features/Notifications/List/QueryNotificationsRequest.cs` | (move target) |
| `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/List/ListNotificationsEndpoint.cs` | (move target) |
| `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/List/NotificationService.List.cs` | New partial-class fragment |
| `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/UnreadCount/UnreadCountEndpoint.cs` | (move target) |
| `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/UnreadCount/NotificationService.UnreadCount.cs` | New partial-class fragment |
| `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/MarkRead/MarkReadEndpoint.cs` | (move target) |
| `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/MarkRead/NotificationService.MarkRead.cs` | New partial-class fragment |
| `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/MarkAllRead/MarkAllReadEndpoint.cs` | (move target) |
| `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/MarkAllRead/NotificationService.MarkAllRead.cs` | New partial-class fragment |
| `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/GetById/NotificationService.GetById.cs` | New partial-class fragment (no endpoint — cross-module contract method only) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationsDbContext.cs` | (move target) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationService.cs` | Root partial (was `Services/NotificationService.cs`; bodies extracted) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Notifier.cs` | (move target) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationsLog.cs` | (move target) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/*` (5 files) | (move targets) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/EntityConfigurations/NotificationConfiguration.cs` | (move target) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Jobs/DispatchNotificationJob.cs` | (move target) |
| `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/List/ListAsyncTests.cs` | Split from `Unit/NotificationServiceTests.cs` |
| `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/UnreadCount/GetUnreadCountAsyncTests.cs` | Split from `Unit/NotificationServiceTests.cs` |
| `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/MarkRead/MarkReadAsyncTests.cs` | Split from `Unit/NotificationServiceTests.cs` |
| `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/MarkAllRead/MarkAllReadAsyncTests.cs` | Split from `Unit/NotificationServiceTests.cs` |
| `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/NotificationServiceTestFixture.cs` | Shared in-memory DB + SeedAsync helper |

### Files moved (also tracked by `notifications.tsv`)

The TSV manifest in Task 4 is the authoritative source. The table above lists the *destinations*; sources are listed in the manifest.

### Files explicitly NOT moved

These stay where they are:

- `modules/Notifications/src/SimpleModule.Notifications/Pages/InboxEndpoint.cs`
- `modules/Notifications/src/SimpleModule.Notifications/Pages/Inbox.tsx`
- `modules/Notifications/src/SimpleModule.Notifications/Pages/index.ts`
- `modules/Notifications/src/SimpleModule.Notifications/NotificationsModule.cs`
- `modules/Notifications/src/SimpleModule.Notifications/NotificationsModuleOptions.cs`
- `modules/Notifications/src/SimpleModule.Notifications/NotificationsPermissions.cs`
- `modules/Notifications/src/SimpleModule.Notifications/types.ts`, `vite.config.ts`
- All entity / shared / event types in `SimpleModule.Notifications.Contracts/` root and `Events/` (per the pilot's bounded scope — entities reference EF migrations)
- `SimpleModule.Notifications.Tests/Unit/NotifierTests.cs`, `Unit/TestBackgroundJobs.cs` (cross-cutting tests stay in `Unit/`)

---

## Task 1: Set up the migration-script test scaffold

**Files:**
- Create: `scripts/feature-folder-migrate.test.mjs`

The script's behaviour is small enough that a single Node test file using the built-in `node:test` runner is sufficient. Tests will exercise the pure functions before we add a CLI driver.

- [ ] **Step 1: Create the failing test file**

Create `scripts/feature-folder-migrate.test.mjs`:

```javascript
// scripts/feature-folder-migrate.test.mjs
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { deriveNamespace } from './feature-folder-migrate.mjs';

test('deriveNamespace: file at project root → assembly name only', () => {
  const ns = deriveNamespace({
    assemblyName: 'SimpleModule.Notifications',
    projectRoot: 'modules/Notifications/src/SimpleModule.Notifications',
    filePath: 'modules/Notifications/src/SimpleModule.Notifications/NotificationsModule.cs',
  });
  assert.equal(ns, 'SimpleModule.Notifications');
});

test('deriveNamespace: nested feature folder → dotted segments', () => {
  const ns = deriveNamespace({
    assemblyName: 'SimpleModule.Notifications',
    projectRoot: 'modules/Notifications/src/SimpleModule.Notifications',
    filePath:
      'modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/List/ListNotificationsEndpoint.cs',
  });
  assert.equal(ns, 'SimpleModule.Notifications.Features.Notifications.List');
});

test('deriveNamespace: Contracts assembly with Features subtree', () => {
  const ns = deriveNamespace({
    assemblyName: 'SimpleModule.Notifications.Contracts',
    projectRoot: 'modules/Notifications/src/SimpleModule.Notifications.Contracts',
    filePath:
      'modules/Notifications/src/SimpleModule.Notifications.Contracts/Features/Notifications/List/QueryNotificationsRequest.cs',
  });
  assert.equal(ns, 'SimpleModule.Notifications.Contracts.Features.Notifications.List');
});

test('deriveNamespace: throws when filePath is outside projectRoot', () => {
  assert.throws(
    () =>
      deriveNamespace({
        assemblyName: 'SimpleModule.Notifications',
        projectRoot: 'modules/Notifications/src/SimpleModule.Notifications',
        filePath: 'modules/Other/SomeFile.cs',
      }),
    /not under project root/,
  );
});
```

- [ ] **Step 2: Run tests, confirm they fail**

Run: `node --test scripts/feature-folder-migrate.test.mjs`

Expected: All 4 tests FAIL with `Cannot find module './feature-folder-migrate.mjs'` (the script doesn't exist yet).

- [ ] **Step 3: Commit the test scaffold**

```bash
git add scripts/feature-folder-migrate.test.mjs
git commit -m "test: scaffold feature-folder-migrate.mjs test suite"
```

---

## Task 2: Implement `deriveNamespace`

**Files:**
- Create: `scripts/feature-folder-migrate.mjs`

- [ ] **Step 1: Write the minimal implementation**

Create `scripts/feature-folder-migrate.mjs`:

```javascript
// scripts/feature-folder-migrate.mjs
import { posix as path } from 'node:path';

/**
 * Compute the C# namespace for a file based on its position relative to the
 * project root, using the folder-equals-namespace convention.
 *
 * @param {object} args
 * @param {string} args.assemblyName  e.g. "SimpleModule.Notifications"
 * @param {string} args.projectRoot   path to the .csproj directory (forward-slash)
 * @param {string} args.filePath      path to the .cs file (forward-slash)
 * @returns {string} the dotted namespace, e.g. "SimpleModule.Notifications.Features.Notifications.List"
 */
export function deriveNamespace({ assemblyName, projectRoot, filePath }) {
  const normalizedRoot = path.normalize(projectRoot).replace(/\/+$/, '') + '/';
  const normalizedFile = path.normalize(filePath);
  if (!normalizedFile.startsWith(normalizedRoot)) {
    throw new Error(
      `File ${filePath} is not under project root ${projectRoot}`,
    );
  }
  const relative = normalizedFile.slice(normalizedRoot.length);
  const dir = path.dirname(relative);
  if (dir === '.' || dir === '') {
    return assemblyName;
  }
  const segments = dir.split('/').filter((s) => s.length > 0);
  return [assemblyName, ...segments].join('.');
}
```

- [ ] **Step 2: Run tests, confirm all 4 pass**

Run: `node --test scripts/feature-folder-migrate.test.mjs`

Expected output ends with `# pass 4` and `# fail 0`.

- [ ] **Step 3: Commit**

```bash
git add scripts/feature-folder-migrate.mjs
git commit -m "feat(scripts): add deriveNamespace for feature-folder migrate tool"
```

---

## Task 3: Implement namespace rewriter

**Files:**
- Modify: `scripts/feature-folder-migrate.mjs`
- Modify: `scripts/feature-folder-migrate.test.mjs`

This step adds `rewriteNamespace(source, newNamespace)`: takes the original `.cs` source text and returns it with its `namespace ...;` declaration replaced. Only the file-scoped form (`namespace X.Y;` at top level) is supported — every module file in the repo uses this style per `.editorconfig`.

- [ ] **Step 1: Add the failing tests**

Append to `scripts/feature-folder-migrate.test.mjs`:

```javascript
import { rewriteNamespace } from './feature-folder-migrate.mjs';

test('rewriteNamespace: replaces existing file-scoped namespace', () => {
  const before = [
    'using System;',
    '',
    'namespace SimpleModule.Notifications.Endpoints.Notifications;',
    '',
    'public class Foo {}',
  ].join('\n');
  const after = rewriteNamespace(before, 'SimpleModule.Notifications.Features.Notifications.List');
  assert.match(after, /^namespace SimpleModule\.Notifications\.Features\.Notifications\.List;$/m);
  assert.doesNotMatch(after, /Endpoints\.Notifications/);
});

test('rewriteNamespace: preserves trailing whitespace and other lines', () => {
  const before = 'namespace A.B;\n\npublic class X {}\n';
  const after = rewriteNamespace(before, 'A.C');
  assert.equal(after, 'namespace A.C;\n\npublic class X {}\n');
});

test('rewriteNamespace: no-op when target equals current', () => {
  const before = 'namespace A.B;\n\npublic class X {}\n';
  const after = rewriteNamespace(before, 'A.B');
  assert.equal(after, before);
});

test('rewriteNamespace: throws on block-scoped namespace (unsupported)', () => {
  const before = 'namespace A.B\n{\n    public class X {}\n}\n';
  assert.throws(
    () => rewriteNamespace(before, 'A.C'),
    /file-scoped namespace declaration/,
  );
});

test('rewriteNamespace: throws when no namespace declaration found', () => {
  const before = 'public class X {}\n';
  assert.throws(() => rewriteNamespace(before, 'A.B'), /no file-scoped namespace/);
});
```

- [ ] **Step 2: Run tests, confirm the 5 new tests fail**

Run: `node --test scripts/feature-folder-migrate.test.mjs`

Expected: 4 passing (from Task 2) + 5 failing (`rewriteNamespace is not a function`).

- [ ] **Step 3: Implement `rewriteNamespace`**

Append to `scripts/feature-folder-migrate.mjs`:

```javascript
const FILE_SCOPED_NS = /^namespace\s+([A-Za-z_][\w.]*)\s*;\s*$/m;
const BLOCK_SCOPED_NS = /^namespace\s+[A-Za-z_][\w.]*\s*\{/m;

/**
 * Replace the file-scoped namespace declaration in a .cs source string.
 * Throws if the file uses a block-scoped namespace or has none at all.
 */
export function rewriteNamespace(source, newNamespace) {
  const match = source.match(FILE_SCOPED_NS);
  if (!match) {
    if (BLOCK_SCOPED_NS.test(source)) {
      throw new Error(
        'rewriteNamespace requires a file-scoped namespace declaration (got block-scoped)',
      );
    }
    throw new Error('rewriteNamespace: no file-scoped namespace declaration found');
  }
  if (match[1] === newNamespace) {
    return source;
  }
  return source.replace(FILE_SCOPED_NS, `namespace ${newNamespace};`);
}
```

- [ ] **Step 4: Run tests, confirm all 9 pass**

Run: `node --test scripts/feature-folder-migrate.test.mjs`

Expected: `# pass 9`, `# fail 0`.

- [ ] **Step 5: Commit**

```bash
git add scripts/feature-folder-migrate.mjs scripts/feature-folder-migrate.test.mjs
git commit -m "feat(scripts): add rewriteNamespace for feature-folder migrate tool"
```

---

## Task 4: Implement CLI driver and manifest parser

**Files:**
- Modify: `scripts/feature-folder-migrate.mjs`
- Modify: `scripts/feature-folder-migrate.test.mjs`

The CLI takes one argument — the path to a TSV manifest — and applies it. Manifest format:

```
# Lines starting with # are comments. Blank lines ignored.
# Three tab-separated columns:
# OLD_PATH<TAB>NEW_PATH<TAB>ASSEMBLY_NAME[/PROJECT_ROOT]
# where ASSEMBLY_NAME is the C# assembly the file lives in and
# PROJECT_ROOT (after the slash) is the .csproj directory.
# If PROJECT_ROOT is omitted, it is inferred as the existing parent of OLD_PATH
# up to (and including) a directory matching ASSEMBLY_NAME.
modules/Notifications/src/SimpleModule.Notifications/Endpoints/Notifications/ListNotificationsEndpoint.cs\tmodules/Notifications/src/SimpleModule.Notifications/Features/Notifications/List/ListNotificationsEndpoint.cs\tSimpleModule.Notifications
```

For each entry:
1. Create the destination directory if missing.
2. `git mv <old> <new>`.
3. If the file is `.cs`, read it, rewrite its namespace using `deriveNamespace` + `rewriteNamespace`, write it back, and `git add` the modified file.

- [ ] **Step 1: Write failing test for `parseManifest`**

Append to `scripts/feature-folder-migrate.test.mjs`:

```javascript
import { parseManifest } from './feature-folder-migrate.mjs';

test('parseManifest: skips comments and blank lines, parses TSV rows', () => {
  const input = [
    '# header comment',
    '',
    'a/b.cs\ta/c/b.cs\tSimpleModule.X',
    '   ',
    'foo.cs\tbar/foo.cs\tSimpleModule.Y\tmodules/Y/src/SimpleModule.Y',
  ].join('\n');
  const rows = parseManifest(input);
  assert.deepEqual(rows, [
    { oldPath: 'a/b.cs', newPath: 'a/c/b.cs', assemblyName: 'SimpleModule.X', projectRoot: undefined },
    {
      oldPath: 'foo.cs',
      newPath: 'bar/foo.cs',
      assemblyName: 'SimpleModule.Y',
      projectRoot: 'modules/Y/src/SimpleModule.Y',
    },
  ]);
});

test('parseManifest: throws on malformed row (wrong column count)', () => {
  assert.throws(() => parseManifest('only-one-column.cs\n'), /expected 3 or 4 tab-separated columns/);
});
```

- [ ] **Step 2: Run tests, confirm 2 new failures**

Run: `node --test scripts/feature-folder-migrate.test.mjs`

Expected: `# pass 9`, `# fail 2` (`parseManifest is not a function`).

- [ ] **Step 3: Implement `parseManifest` and the CLI**

Append to `scripts/feature-folder-migrate.mjs`:

```javascript
import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

/**
 * Parse a TSV manifest. Returns an array of move directives.
 */
export function parseManifest(text) {
  return text
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0 && !line.startsWith('#'))
    .map((line, idx) => {
      const cols = line.split('\t');
      if (cols.length !== 3 && cols.length !== 4) {
        throw new Error(
          `line ${idx + 1}: expected 3 or 4 tab-separated columns, got ${cols.length}`,
        );
      }
      return {
        oldPath: cols[0],
        newPath: cols[1],
        assemblyName: cols[2],
        projectRoot: cols[3],
      };
    });
}

/**
 * Infer the .csproj directory by walking up from oldPath until we find a folder
 * named exactly assemblyName.
 */
function inferProjectRoot(oldPath, assemblyName) {
  const parts = path.normalize(oldPath).split('/');
  for (let i = parts.length - 1; i > 0; i -= 1) {
    if (parts[i] === assemblyName) {
      return parts.slice(0, i + 1).join('/');
    }
  }
  throw new Error(
    `cannot infer projectRoot for ${oldPath}; no path segment matches assembly ${assemblyName}`,
  );
}

function applyMove(directive) {
  const projectRoot = directive.projectRoot ?? inferProjectRoot(directive.oldPath, directive.assemblyName);
  const newDir = path.dirname(directive.newPath);
  mkdirSync(newDir, { recursive: true });
  execFileSync('git', ['mv', directive.oldPath, directive.newPath], { stdio: 'inherit' });

  if (directive.newPath.endsWith('.cs')) {
    const newNamespace = deriveNamespace({
      assemblyName: directive.assemblyName,
      projectRoot,
      filePath: directive.newPath,
    });
    const source = readFileSync(directive.newPath, 'utf8');
    const rewritten = rewriteNamespace(source, newNamespace);
    if (rewritten !== source) {
      writeFileSync(directive.newPath, rewritten);
      execFileSync('git', ['add', directive.newPath], { stdio: 'inherit' });
    }
  }
}

function main(argv) {
  const manifestPath = argv[2];
  if (!manifestPath) {
    console.error('Usage: node scripts/feature-folder-migrate.mjs <manifest.tsv>');
    process.exit(2);
  }
  const text = readFileSync(manifestPath, 'utf8');
  const directives = parseManifest(text);
  for (const d of directives) {
    applyMove(d);
  }
  console.log(`Applied ${directives.length} move(s) from ${manifestPath}`);
}

const isCli = process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1];
if (isCli) {
  main(process.argv);
}
```

- [ ] **Step 4: Run tests, confirm all 11 pass**

Run: `node --test scripts/feature-folder-migrate.test.mjs`

Expected: `# pass 11`, `# fail 0`.

- [ ] **Step 5: Smoke-test the CLI on a tiny throwaway manifest**

Create a temporary fixture and dry-run, then delete it. From the worktree root:

```bash
mkdir -p /tmp/ffm-smoke/src/SimpleModule.Smoke
cat > /tmp/ffm-smoke/src/SimpleModule.Smoke/Foo.cs <<'EOF'
namespace SimpleModule.Smoke;

public class Foo {}
EOF
cd /tmp/ffm-smoke && git init -q && git add . && git -c user.email=a@b -c user.name=a commit -q -m init
printf 'src/SimpleModule.Smoke/Foo.cs\tsrc/SimpleModule.Smoke/Features/Bar/Foo.cs\tSimpleModule.Smoke\n' > manifest.tsv
node /root/github/SimpleModule/.claude/worktrees/explore-feature-folders/scripts/feature-folder-migrate.mjs manifest.tsv
cat src/SimpleModule.Smoke/Features/Bar/Foo.cs
```

Expected: the file is moved to `Features/Bar/Foo.cs` and its namespace is now `namespace SimpleModule.Smoke.Features.Bar;`. Clean up: `cd / && rm -rf /tmp/ffm-smoke` and `cd` back to the worktree.

- [ ] **Step 6: Commit**

```bash
git add scripts/feature-folder-migrate.mjs scripts/feature-folder-migrate.test.mjs
git commit -m "feat(scripts): add CLI driver and manifest parser for feature-folder migrate"
```

---

## Task 5: Write the Notifications manifest

**Files:**
- Create: `scripts/manifests/notifications.tsv`

This task only creates the manifest. Application happens in Tasks 6 and 7, split by concern so each commit is reviewable.

- [ ] **Step 1: Create `scripts/manifests/` and write the manifest**

```bash
mkdir -p scripts/manifests
```

Create `scripts/manifests/notifications.tsv` (use literal tabs between columns — most editors expand on save; if yours doesn't, use `printf '%s\t%s\t%s\n'` to generate lines):

```
# Notifications pilot — feature-folder migration manifest.
# Columns: OLD_PATH<TAB>NEW_PATH<TAB>ASSEMBLY_NAME

# --- Infrastructure moves (impl project) ---
modules/Notifications/src/SimpleModule.Notifications/NotificationsDbContext.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationsDbContext.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Services/NotificationService.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationService.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Services/Notifier.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Notifier.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Services/NotificationsLog.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationsLog.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Channels/INotificationChannel.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/INotificationChannel.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Channels/INotificationChannelRegistry.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/INotificationChannelRegistry.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Channels/DatabaseChannel.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/DatabaseChannel.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Channels/MailChannel.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/MailChannel.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Channels/LogSmsChannel.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/LogSmsChannel.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/EntityConfigurations/NotificationConfiguration.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/EntityConfigurations/NotificationConfiguration.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Jobs/DispatchNotificationJob.cs	modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Jobs/DispatchNotificationJob.cs	SimpleModule.Notifications

# --- Feature moves: endpoints (impl project) ---
modules/Notifications/src/SimpleModule.Notifications/Endpoints/Notifications/ListNotificationsEndpoint.cs	modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/List/ListNotificationsEndpoint.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Endpoints/Notifications/UnreadCountEndpoint.cs	modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/UnreadCount/UnreadCountEndpoint.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Endpoints/Notifications/MarkReadEndpoint.cs	modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/MarkRead/MarkReadEndpoint.cs	SimpleModule.Notifications
modules/Notifications/src/SimpleModule.Notifications/Endpoints/Notifications/MarkAllReadEndpoint.cs	modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/MarkAllRead/MarkAllReadEndpoint.cs	SimpleModule.Notifications

# --- Feature-scoped DTO move (contracts project) ---
modules/Notifications/src/SimpleModule.Notifications.Contracts/QueryNotificationsRequest.cs	modules/Notifications/src/SimpleModule.Notifications.Contracts/Features/Notifications/List/QueryNotificationsRequest.cs	SimpleModule.Notifications.Contracts
```

> **Pilot scope note** (also enforced by what's *not* in this manifest): entity types (`Notification`, `NotificationId`, `NotificationRecipient`) and channel-payload types (`MailMessage`, `SmsMessage`, `DatabaseNotificationPayload`, `INotification`) stay at `Contracts/` root because they are referenced by full namespace in `template/SimpleModule.Host/Migrations/*` snapshots. Moving them would force migration regeneration, which is out of scope for the pilot. Revisit once the pattern is proven and we have a migration-rewrite story.

- [ ] **Step 2: Commit the manifest**

```bash
git add scripts/manifests/notifications.tsv
git commit -m "chore(notifications): add feature-folder migration manifest"
```

---

## Task 6: Apply manifest — Infrastructure moves only

**Files:**
- Modify (via `git mv` + namespace rewrite): the 11 Infrastructure rows of `notifications.tsv`
- Modify: all files containing `using SimpleModule.Notifications.Channels`, `using SimpleModule.Notifications.Endpoints.Notifications`, `using SimpleModule.Notifications.EntityConfigurations`, `using SimpleModule.Notifications.Jobs`, `using SimpleModule.Notifications.Services`, and the bare module-root namespace where appropriate.

This task moves only the Infrastructure rows (rows 1–11) of the manifest. Features and Contracts come in Task 7. This is split to keep the diff readable and the failure surface smaller.

- [ ] **Step 1: Apply Infrastructure rows**

Create a temporary partial manifest with only the Infrastructure rows, run the migrate tool, then delete the partial:

```bash
sed -n '/# --- Infrastructure moves/,/# --- Feature moves: endpoints/p' scripts/manifests/notifications.tsv \
  | grep -v '^#' | grep -v '^$' > /tmp/notifications-infra.tsv
node scripts/feature-folder-migrate.mjs /tmp/notifications-infra.tsv
rm /tmp/notifications-infra.tsv
```

Expected: 11 files moved, 11 namespace rewrites, all staged with `git add`.

- [ ] **Step 2: Verify the build fails as expected**

Run: `dotnet build modules/Notifications/src/SimpleModule.Notifications/SimpleModule.Notifications.csproj`

Expected: compilation FAILS with `CS0234` errors (`The type or namespace name 'X' does not exist in the namespace 'SimpleModule.Notifications.<old-folder>'`) and/or `CS0246` (type not found). These come from `NotificationsModule.cs` and the four endpoint files still using the old `Services`/`Channels`/`Jobs` namespaces.

This failure confirms the rewrite tool didn't silently miss anything — it moved 11 files and the consumers haven't been updated yet.

- [ ] **Step 3: Fix `using` statements in consumers**

Update `using` statements across the Notifications module so the new namespaces resolve. The following table lists every consumer file and the exact `using` line edits:

| File | Remove `using` | Add `using` |
|---|---|---|
| `modules/Notifications/src/SimpleModule.Notifications/NotificationsModule.cs` | `using SimpleModule.Notifications.Channels;` `using SimpleModule.Notifications.Jobs;` `using SimpleModule.Notifications.Services;` | `using SimpleModule.Notifications.Infrastructure;` `using SimpleModule.Notifications.Infrastructure.Channels;` `using SimpleModule.Notifications.Infrastructure.Jobs;` |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/DatabaseChannel.cs` | (none — file moved, internal references stay valid since they reference Contracts types) | (none; verify build) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/MailChannel.cs` | (none) | (none; verify build) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/LogSmsChannel.cs` | (none) | (none; verify build) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/INotificationChannel.cs` | (none) | (none) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Channels/INotificationChannelRegistry.cs` | (none) | (none) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationService.cs` | (none — references its own DbContext via Contracts, which didn't move) | (none) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Notifier.cs` | If it referenced `Channels` types in its `using`s, replace `using SimpleModule.Notifications.Channels;` with `using SimpleModule.Notifications.Infrastructure.Channels;` | (as needed) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationsLog.cs` | (none expected) | (none expected) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationsDbContext.cs` | If references `EntityConfigurations` namespace, replace with `Infrastructure.EntityConfigurations` | (as needed) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/Jobs/DispatchNotificationJob.cs` | If references `Channels` or `Services`, update to `Infrastructure.Channels` / `Infrastructure` | (as needed) |
| `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/EntityConfigurations/NotificationConfiguration.cs` | (none — uses Contracts types) | (none) |
| `modules/Notifications/src/SimpleModule.Notifications/Endpoints/Notifications/ListNotificationsEndpoint.cs` | (none — endpoints still reference contract interface; service is injected, not used by type) | (none) |
| `modules/Notifications/src/SimpleModule.Notifications/Endpoints/Notifications/UnreadCountEndpoint.cs` | (none) | (none) |
| `modules/Notifications/src/SimpleModule.Notifications/Endpoints/Notifications/MarkReadEndpoint.cs` | (none) | (none) |
| `modules/Notifications/src/SimpleModule.Notifications/Endpoints/Notifications/MarkAllReadEndpoint.cs` | (none) | (none) |
| `modules/Notifications/src/SimpleModule.Notifications/Pages/InboxEndpoint.cs` | (none) | (none) |
| `modules/Notifications/tests/SimpleModule.Notifications.Tests/Unit/NotificationServiceTests.cs` | `using SimpleModule.Notifications.Services;` | `using SimpleModule.Notifications.Infrastructure;` |
| `modules/Notifications/tests/SimpleModule.Notifications.Tests/Unit/NotifierTests.cs` | `using SimpleModule.Notifications.Services;` (if present) | `using SimpleModule.Notifications.Infrastructure;` |

For each "as needed" row above, open the file, read its `using` list, and apply the substitution mechanically. **Do not change anything else.** Use search-and-replace at the directive level (find exact `using` lines, not types).

- [ ] **Step 4: Re-run the build**

Run: `dotnet build modules/Notifications/src/SimpleModule.Notifications/SimpleModule.Notifications.csproj`

Expected: 0 errors, 0 warnings (warnings would also fail given `TreatWarningsAsErrors=true`).

- [ ] **Step 5: Run the test project**

Run: `dotnet test modules/Notifications/tests/SimpleModule.Notifications.Tests/SimpleModule.Notifications.Tests.csproj`

Expected: all tests pass — at least the 7 in `NotificationServiceTests`, plus whatever is in `NotifierTests.cs` and `TestBackgroundJobs.cs`. Confirm count matches pre-migration baseline (capture by running once before Task 6 if not already known).

- [ ] **Step 6: Run the full solution build**

Run: `dotnet build` (from the worktree root).

Expected: 0 errors. This validates that no other module accidentally referenced the moved namespaces.

- [ ] **Step 7: Commit**

```bash
git add modules/Notifications/
git commit -m "refactor(notifications): move infrastructure files into Infrastructure/ subfolder

Move NotificationsDbContext, NotificationService, Notifier, NotificationsLog,
Channels/*, EntityConfigurations/*, and Jobs/* into Infrastructure/ to start
the feature-folder migration. Update internal using statements; no public
contract changes. See spec: docs/superpowers/specs/2026-05-14-vertical-slice-feature-folders-design.md"
```

---

## Task 7: Apply manifest — Features moves + Contracts move

**Files:**
- Modify (via `git mv` + namespace rewrite): the Features and Contracts rows of `notifications.tsv`
- Modify: any consumer that now needs an extra `using` for the new Features namespaces.

- [ ] **Step 1: Apply the remaining rows**

```bash
sed -n '/# --- Feature moves/,$p' scripts/manifests/notifications.tsv \
  | grep -v '^#' | grep -v '^$' > /tmp/notifications-features.tsv
node scripts/feature-folder-migrate.mjs /tmp/notifications-features.tsv
rm /tmp/notifications-features.tsv
```

Expected: 5 files moved (4 endpoints + 1 contracts request), 5 namespace rewrites.

- [ ] **Step 2: Verify build fails with expected breaks**

Run: `dotnet build`

Expected: compilation FAILS because `QueryNotificationsRequest` is now in `SimpleModule.Notifications.Contracts.Features.Notifications.List` but consumers in the Notifications module and host still import only `SimpleModule.Notifications.Contracts`.

Compilation errors are expected in (at minimum): `Infrastructure/NotificationService.cs`, `Features/Notifications/List/ListNotificationsEndpoint.cs`, `Pages/InboxEndpoint.cs`, `NotificationServiceTests.cs`.

- [ ] **Step 3: Add the new `using` to consumers of `QueryNotificationsRequest`**

In each file that uses `QueryNotificationsRequest`, add a `using` for the new namespace **after** the existing `using SimpleModule.Notifications.Contracts;`:

```diff
 using SimpleModule.Notifications.Contracts;
+using SimpleModule.Notifications.Contracts.Features.Notifications.List;
```

Files needing this edit (verify via `grep -rln "QueryNotificationsRequest" modules/Notifications/ template/SimpleModule.Host/`):

- `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationService.cs`
- `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/List/ListNotificationsEndpoint.cs` (the file was just moved; the `using` for its own namespace neighbour is automatic, but it consumes `QueryNotificationsRequest` from Contracts so the new `using` is required)
- `modules/Notifications/src/SimpleModule.Notifications/Pages/InboxEndpoint.cs`
- `modules/Notifications/tests/SimpleModule.Notifications.Tests/Unit/NotificationServiceTests.cs`

The endpoint files for `UnreadCount`, `MarkRead`, `MarkAllRead` do NOT use `QueryNotificationsRequest` and need no edits.

- [ ] **Step 4: Re-run the build**

Run: `dotnet build`

Expected: 0 errors.

- [ ] **Step 5: Run all tests**

Run: `dotnet test modules/Notifications/tests/SimpleModule.Notifications.Tests/SimpleModule.Notifications.Tests.csproj`

Expected: all tests pass — same count as after Task 6.

- [ ] **Step 6: Commit**

```bash
git add modules/Notifications/
git commit -m "refactor(notifications): move endpoints to Features/ and slice Contracts

Endpoints/Notifications/* moved to Features/Notifications/<Op>/. The feature-
scoped QueryNotificationsRequest moved to Contracts/Features/Notifications/List/.
Entity, value-object, and channel-payload contracts remain at Contracts root
to avoid migration-snapshot churn (revisit in a follow-up phase)."
```

---

## Task 8: Split `NotificationService` into partial-class fragments

**Files:**
- Modify: `modules/Notifications/src/SimpleModule.Notifications/Infrastructure/NotificationService.cs` (becomes the root partial — drops methods, keeps constructor)
- Create: `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/List/NotificationService.List.cs`
- Create: `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/UnreadCount/NotificationService.UnreadCount.cs`
- Create: `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/MarkRead/NotificationService.MarkRead.cs`
- Create: `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/MarkAllRead/NotificationService.MarkAllRead.cs`
- Create: `modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/GetById/NotificationService.GetById.cs`

The class becomes `public sealed partial class NotificationService`. Each operation method moves to its feature folder. The root partial declares the primary constructor `(NotificationsDbContext db)` and the implements clause; the fragments add methods only.

- [ ] **Step 1: Rewrite `Infrastructure/NotificationService.cs` as the root partial**

Replace the file content with:

```csharp
using SimpleModule.Notifications.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService(NotificationsDbContext db) : INotificationsContracts
{
}
```

The `NotificationsDbContext db` primary constructor parameter is in scope for every partial fragment in the same class (C# 12 primary constructor semantics).

> **Heads-up on namespaces:** partial-class fragments under `Features/<Agg>/<Op>/` declare the *owning class's* namespace (the `Infrastructure/` namespace where the root partial lives), **not** the folder-derived namespace. C# requires every partial declaration of the same class to share a namespace; otherwise they become distinct types and SM0025 fires. This is a deliberate exception to spec Convention C1; the file's *folder* identifies the slice, and the *namespace* identifies the type. Task 11 amends the spec to record this. Every `NotificationService.<Op>.cs` fragment below uses `namespace SimpleModule.Notifications.Infrastructure;`.

- [ ] **Step 2: Create `Features/Notifications/List/NotificationService.List.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Contracts.Features.Notifications.List;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService
{
    public async Task<PagedResult<Notification>> ListAsync(
        UserId userId,
        QueryNotificationsRequest request
    )
    {
        var query = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        if (request.UnreadOnly == true)
        {
            query = query.Where(n => n.ReadAt == null);
        }
        if (!string.IsNullOrWhiteSpace(request.Channel))
        {
            query = query.Where(n => n.Channel == request.Channel);
        }
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(n => n.Type == request.Type);
        }

        var totalCount = await query.CountAsync();
        var page = request.EffectivePage;
        var pageSize = request.EffectivePageSize;

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Notification>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }
}
```

- [ ] **Step 3: Create the remaining 4 fragments**

`Features/Notifications/UnreadCount/NotificationService.UnreadCount.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService
{
    public Task<int> GetUnreadCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    ) =>
        db.Notifications.CountAsync(
            n => n.UserId == userId && n.ReadAt == null,
            cancellationToken
        );
}
```

`Features/Notifications/GetById/NotificationService.GetById.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService
{
    public async Task<Notification?> GetByIdAsync(NotificationId id, UserId userId) =>
        await db.Notifications.AsNoTracking().FirstOrDefaultAsync(n =>
            n.Id == id && n.UserId == userId
        );
}
```

`Features/Notifications/MarkRead/NotificationService.MarkRead.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService
{
    public async Task<bool> MarkReadAsync(NotificationId id, UserId userId)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(n =>
            n.Id == id && n.UserId == userId
        );
        if (notification is null)
        {
            return false;
        }

        if (notification.ReadAt is not null)
        {
            return true;
        }

        notification.ReadAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}
```

`Features/Notifications/MarkAllRead/NotificationService.MarkAllRead.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public sealed partial class NotificationService
{
    public async Task<int> MarkAllReadAsync(UserId userId)
    {
        var now = DateTimeOffset.UtcNow;
        return await db
            .Notifications.Where(n => n.UserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build`

Expected: 0 errors. If the source generator complains that `INotificationsContracts` is not fully implemented (SM0025 or CS0535), one of the partial fragments has a typo in its method signature or wrong namespace. Verify each fragment compiles before moving on.

- [ ] **Step 5: Run tests**

Run: `dotnet test modules/Notifications/tests/SimpleModule.Notifications.Tests/SimpleModule.Notifications.Tests.csproj`

Expected: same test count as before, all passing. The partial-class split is a pure refactor — no behaviour change.

- [ ] **Step 6: Commit**

```bash
git add modules/Notifications/
git commit -m "refactor(notifications): split NotificationService into per-feature partials

NotificationService becomes a sealed partial class. Each cross-module contract
method (ListAsync, GetUnreadCountAsync, GetByIdAsync, MarkReadAsync,
MarkAllReadAsync) moves to a fragment file co-located with its feature folder.
The root partial in Infrastructure/ retains the constructor and implements
clause. Partial fragments deliberately keep the owning class's namespace
(Infrastructure) rather than matching folder; their *folder* identifies
the slice."
```

---

## Task 9: Split `NotificationServiceTests` into per-feature test files

**Files:**
- Modify: `modules/Notifications/tests/SimpleModule.Notifications.Tests/Unit/NotificationServiceTests.cs` → delete after extracting
- Create: `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/NotificationServiceTestFixture.cs`
- Create: `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/List/ListAsyncTests.cs`
- Create: `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/UnreadCount/GetUnreadCountAsyncTests.cs`
- Create: `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/MarkRead/MarkReadAsyncTests.cs`
- Create: `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/MarkAllRead/MarkAllReadAsyncTests.cs`

The original `NotificationServiceTests.cs` has 7 tests across 4 operations. The shared in-memory DbContext setup and `SeedAsync` helper move into a `NotificationServiceTestFixture` base class. Each operation's tests become a small file.

- [ ] **Step 1: Create the shared fixture**

Create `modules/Notifications/tests/SimpleModule.Notifications.Tests/Features/Notifications/NotificationServiceTestFixture.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Infrastructure;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Features.Notifications;

public abstract class NotificationServiceTestFixture : IDisposable
{
    protected readonly NotificationsDbContext Db;
    protected readonly NotificationService Sut;
    protected readonly UserId CurrentUserId = UserId.From("user-1");

    protected NotificationServiceTestFixture()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["Notifications"] = "Data Source=:memory:",
                },
            }
        );
        Db = new NotificationsDbContext(options, dbOptions);
        Db.Database.OpenConnection();
        Db.Database.EnsureCreated();
        Sut = new NotificationService(Db);
    }

    public void Dispose()
    {
        Db.Dispose();
        GC.SuppressFinalize(this);
    }

    protected async Task<Notification> SeedAsync(UserId? userId = null, DateTimeOffset? readAt = null)
    {
        var n = new Notification
        {
            Id = NotificationId.From(Guid.CreateVersion7()),
            UserId = userId ?? CurrentUserId,
            Type = "test.event",
            Channel = NotificationsConstants.Channels.Database,
            Title = "Title",
            Body = "Body",
            DataJson = "{}",
            ReadAt = readAt,
        };
        Db.Notifications.Add(n);
        await Db.SaveChangesAsync();
        return n;
    }
}
```

- [ ] **Step 2: Create `ListAsyncTests.cs`**

```csharp
using FluentAssertions;
using SimpleModule.Notifications.Contracts.Features.Notifications.List;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Features.Notifications.List;

public sealed class ListAsyncTests : NotificationServiceTestFixture
{
    [Fact]
    public async Task ListAsync_ReturnsOnlyOwnNotifications()
    {
        await SeedAsync();
        await SeedAsync();
        await SeedAsync(userId: UserId.From("other-user"));

        var result = await Sut.ListAsync(CurrentUserId, new QueryNotificationsRequest());

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListAsync_UnreadOnly_FiltersReadNotifications()
    {
        await SeedAsync();
        await SeedAsync(readAt: DateTimeOffset.UtcNow);

        var result = await Sut.ListAsync(
            CurrentUserId,
            new QueryNotificationsRequest { UnreadOnly = true }
        );

        result.TotalCount.Should().Be(1);
    }
}
```

- [ ] **Step 3: Create `GetUnreadCountAsyncTests.cs`**

```csharp
using FluentAssertions;

namespace SimpleModule.Notifications.Tests.Features.Notifications.UnreadCount;

public sealed class GetUnreadCountAsyncTests : NotificationServiceTestFixture
{
    [Fact]
    public async Task GetUnreadCountAsync_ReturnsUnreadOnly()
    {
        await SeedAsync();
        await SeedAsync();
        await SeedAsync(readAt: DateTimeOffset.UtcNow);

        var count = await Sut.GetUnreadCountAsync(CurrentUserId);

        count.Should().Be(2);
    }
}
```

- [ ] **Step 4: Create `MarkReadAsyncTests.cs`**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Features.Notifications.MarkRead;

public sealed class MarkReadAsyncTests : NotificationServiceTestFixture
{
    [Fact]
    public async Task MarkReadAsync_SetsReadAt()
    {
        var n = await SeedAsync();

        var result = await Sut.MarkReadAsync(n.Id, CurrentUserId);

        result.Should().BeTrue();
        var refreshed = await Db.Notifications.AsNoTracking().FirstAsync(x => x.Id == n.Id);
        refreshed.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkReadAsync_WithDifferentUser_ReturnsFalse()
    {
        var n = await SeedAsync();

        var result = await Sut.MarkReadAsync(n.Id, UserId.From("not-the-owner"));

        result.Should().BeFalse();
    }
}
```

- [ ] **Step 5: Create `MarkAllReadAsyncTests.cs`**

```csharp
using FluentAssertions;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Tests.Features.Notifications.MarkAllRead;

public sealed class MarkAllReadAsyncTests : NotificationServiceTestFixture
{
    [Fact]
    public async Task MarkAllReadAsync_MarksAllUnreadForUser()
    {
        await SeedAsync();
        await SeedAsync();
        await SeedAsync(readAt: DateTimeOffset.UtcNow);
        await SeedAsync(userId: UserId.From("other"));

        var marked = await Sut.MarkAllReadAsync(CurrentUserId);

        marked.Should().Be(2);
        var remainingUnread = await Sut.GetUnreadCountAsync(CurrentUserId);
        remainingUnread.Should().Be(0);
        var otherUserUnread = await Sut.GetUnreadCountAsync(UserId.From("other"));
        otherUserUnread.Should().Be(1);
    }
}
```

- [ ] **Step 6: Delete the original combined test file**

```bash
git rm modules/Notifications/tests/SimpleModule.Notifications.Tests/Unit/NotificationServiceTests.cs
```

- [ ] **Step 7: Build and test**

Run: `dotnet build`

Expected: 0 errors.

Run: `dotnet test modules/Notifications/tests/SimpleModule.Notifications.Tests/SimpleModule.Notifications.Tests.csproj`

Expected: all 7 `NotificationService*` tests pass (now spread across 4 files) plus the unchanged `NotifierTests.cs` and `TestBackgroundJobs.cs`.

- [ ] **Step 8: Commit**

```bash
git add modules/Notifications/
git commit -m "test(notifications): split NotificationServiceTests by feature

Replace Unit/NotificationServiceTests.cs with per-feature files under
Features/Notifications/<Op>/, sharing a NotificationServiceTestFixture base.
Cross-cutting tests (NotifierTests, TestBackgroundJobs) remain in Unit/."
```

---

## Task 10: Full pilot verification

**Files:** (no edits — this task only runs commands)

Validate the pilot end-to-end: build, test, frontend build, page-registry validation, and CI parity.

- [ ] **Step 1: Capture pre-pilot baselines (if not already captured)**

If a baseline wasn't captured before Task 6, capture one now from `main` by stashing the worktree's changes into a temp branch and checking out main in a separate clone. **Skip this step if the baseline counts are already known** — it exists only to make the comparison concrete in Step 4.

- [ ] **Step 2: Build the entire solution**

Run: `dotnet build`

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Run all .NET tests**

Run: `dotnet test`

Expected: all tests pass, count >= pre-pilot baseline (we added test files but didn't remove behavioural coverage).

- [ ] **Step 4: Validate the page registry**

Run: `npm run validate-pages`

Expected: exit code 0, no mismatches reported. (The Inbox page wasn't moved, so this should be a no-op — but we verify it explicitly because the script is the canonical check for SM0042/IViewEndpoint correctness.)

- [ ] **Step 5: Build the frontend**

Run: `npm run build:dev`

Expected: every workspace builds cleanly, including `@simplemodule/notifications`.

- [ ] **Step 6: Lint and format check**

Run: `npm run check`

Expected: 0 issues.

- [ ] **Step 7: Run the CI skill locally (if available)**

If the `ci` skill is installed, invoke it: it mirrors the GitHub Actions pipeline.

Expected: green across all CI steps.

- [ ] **Step 8: Inspect the source-generator output for Notifications**

Run:

```bash
dotnet build modules/Notifications/src/SimpleModule.Notifications/SimpleModule.Notifications.csproj /p:EmitCompilerGeneratedFiles=true /p:CompilerGeneratedFilesOutputPath=obj/generated
grep -l "NotificationsModule\|INotificationsContracts" \
  modules/Notifications/src/SimpleModule.Notifications/obj/generated/**/*.cs 2>/dev/null || true
```

Expected: the generated files list the 4 endpoint classes and the 1 view endpoint by their **new** namespaces (`SimpleModule.Notifications.Features.Notifications.List.ListNotificationsEndpoint`, etc.). If any old `Endpoints.Notifications` reference appears, the generator is caching stale paths — clean `bin`/`obj` and rebuild.

- [ ] **Step 9: Smoke-test the running app (optional but recommended)**

If a dev environment is available:

```bash
dotnet run --project template/SimpleModule.Host
```

In a browser, hit `/notifications` (the Inbox view) and confirm: page loads, an unread count is shown, and at least one of `GET /api/notifications/`, `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`, `GET /api/notifications/unread-count` returns a non-error status in network inspector.

Skip this step if no test data exists; the unit/integration tests in Step 3 cover the same endpoints.

- [ ] **Step 10: Commit the verification log (if anything was tweaked)**

If Steps 2–9 surfaced any issue and a fix was applied, commit it now with a precise message. If no changes were needed, skip the commit — the pilot is verified.

---

## Task 11: Document the pilot finding and amend the spec

**Files:**
- Modify: `docs/superpowers/specs/2026-05-14-vertical-slice-feature-folders-design.md` (Convention C1 + C2 clarification)
- Create: `tasks/lessons.md` entry under a new "Vertical-slice pilot" heading (or append if file exists)

The Task 8 step uncovered a real conflict between C1 (folder = namespace) and C2 (partial-class split): a partial fragment's namespace must match the root partial's namespace, regardless of folder. The spec needs to record this exception so future module migrations don't trip over it.

- [ ] **Step 1: Amend Convention C1 in the spec**

In `docs/superpowers/specs/2026-05-14-vertical-slice-feature-folders-design.md`, locate Convention C1 and append:

```markdown
**Exception:** partial-class fragments under `Features/<Agg>/<Op>/<Service>.<Op>.cs` declare the *owning class's* namespace (the `Infrastructure/` namespace where the root partial lives), not the folder-derived namespace. The folder identifies the slice; the namespace identifies the type. This is required by C# — a partial declaration in a different namespace is a different type.
```

- [ ] **Step 2: Add a cross-reference to C2**

Locate Convention C2 and append a final sentence:

```markdown
See the C1 exception: fragment namespaces match the owning class, not the folder.
```

- [ ] **Step 3: Append a pilot-results entry to `tasks/lessons.md`**

If `tasks/lessons.md` doesn't exist, create it. Append:

```markdown
## Vertical-slice feature-folder pilot (Notifications, 2026-05-14)

- Folder-equals-namespace conflicts with partial-class fragments. Resolved: fragments keep the owning class's namespace; folder is purely organizational. Spec C1 amended.
- EF migration snapshots reference contract types by full namespace. Moving entity/value-object types into `Contracts/Shared/` would force migration regeneration — out of scope for the pilot. Future phases must decide: keep entities at Contracts root forever, or accept migration churn.
- Cross-module `using SimpleModule.Notifications.Contracts;` count: zero. Per-module Contracts reshape is internal-only for this module.
- `npm run validate-pages` is a no-op for modules where `Pages/` is untouched. Still worth running in CI as the canonical gate.
- Migration script approach (TSV manifest + `git mv` + namespace rewrite) worked cleanly. ~16 moves, two small batches per commit. No surprises from the source generator.
```

- [ ] **Step 4: Commit**

```bash
git add docs/superpowers/specs/2026-05-14-vertical-slice-feature-folders-design.md tasks/lessons.md
git commit -m "docs: amend feature-folder spec C1 with partial-class exception + log pilot lessons"
```

---

## Exit criteria

The pilot is **successful** when **all** of the following hold simultaneously:

1. `dotnet build` from the worktree root passes with 0 errors and 0 warnings.
2. `dotnet test` runs to completion with all tests passing, including the 7 split `NotificationService*` tests and the unchanged `NotifierTests` / `TestBackgroundJobs`.
3. `npm run build:dev` builds every workspace.
4. `npm run validate-pages` exits 0.
5. `npm run check` exits 0.
6. The generated source files (Step 8 of Task 10) reference endpoints by their **new** `Features.Notifications.<Op>` namespaces.
7. `git log --oneline worktree-explore-feature-folders ^main` shows ~10 commits with clean, scoped messages and no `wip` / `fixup` clutter.
8. The Notifications module's `src/SimpleModule.Notifications/` directory has no remaining `Endpoints/`, `Services/`, `Channels/`, `EntityConfigurations/`, or `Jobs/` folders — those names exist only under `Infrastructure/` (Channels, EntityConfigurations, Jobs) or are renamed to `Features/`.
9. Spec C1 carries the partial-class exception note; `tasks/lessons.md` carries the pilot summary.

If any of (1)–(6) fails, the pilot is **not** ready to propagate. Diagnose the failure, fix it, and update this plan (or the spec) with the lesson before declaring success.

If (7) is cluttered, do an interactive rebase BEFORE handing off — but only if explicitly asked. By default leave the commit history as written.

---

## Out of scope (for follow-up plans)

- Converting any other module to the new shape — each gets its own plan after the pilot succeeds.
- Moving entity types or shared value objects into `Contracts/Shared/`. Requires an EF-migration story.
- Updating the `sm new module` / `sm new feature` CLI scaffolds.
- Adding any SM diagnostic to enforce the new layout.
- Reshaping `Pages/` (view endpoints + React) — explicitly out per Convention C4.
