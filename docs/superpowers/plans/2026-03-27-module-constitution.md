# Module Constitution Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Write the Module Constitution document and trim CLAUDE.md to reference it for architectural rules.

**Architecture:** Two files change: create `docs/CONSTITUTION.md` (the authoritative rules document), then edit `CLAUDE.md` to remove sections now covered by the Constitution and add a reference link.

**Tech Stack:** Markdown only. No code changes.

**Spec:** `docs/superpowers/specs/2026-03-27-module-constitution-design.md`

---

## Chunk 1: Write the Constitution

### Task 1: Write docs/CONSTITUTION.md

**Files:**

- Create: `docs/CONSTITUTION.md`

**Source material:** The spec at `docs/superpowers/specs/2026-03-27-module-constitution-design.md` contains the full content for all 12 sections. Write it as a clean, final document — not a spec format.

- [ ] **Step 1: Write the Constitution document**

Create `docs/CONSTITUTION.md` with all 12 sections from the spec. Format as a rules document, not a spec. Use clear headers, bullet points, and tables. No code examples (per design decision). The document should read as the authoritative reference for module development and framework contribution.

**Important framing note:** The existing CLAUDE.md calls the shared database a "Known Limitation." The Constitution reframes this as a deliberate design choice (spec Section 1, line 29). Do not carry over the limitation framing.

Sections to include (all content is in the spec):
1. Founding Principles
2. Module Boundaries (including all 9 lifecycle hooks)
3. Dependencies (including Vogen exception for Contracts)
4. Data Ownership
5. Communication (including forward-compat handler ordering note, PublishAsync vs PublishInBackground guidance)
6. Endpoints (including specific REST conventions and status codes)
7. Frontend (including corrected dynamic imports rule, within-module vs between-module)
8. Permissions & Authorization
9. Settings & Configuration (including scope definitions)
10. Testing
11. Compiler-Enforced Rules (SM diagnostic table)
12. Framework Contributor Guidelines (using "highest existing SM number")

- [ ] **Step 2: Verify the document**

Read the written file and confirm:
- All 12 sections present
- All SM diagnostics listed (SM0001 through SM0043, excluding gaps)
- All 9 IModule lifecycle hooks documented
- No code examples
- Consistent formatting

- [ ] **Step 3: Commit**

```bash
git add docs/CONSTITUTION.md
git commit -m "Add Module Constitution — authoritative rules for module development and framework contribution"
```

---

## Chunk 2: Trim CLAUDE.md

### Task 2: Remove sections moved to Constitution

**Files:**

- Modify: `CLAUDE.md`

- [ ] **Step 1: Remove the following sections from CLAUDE.md**

Remove these sections entirely (their content now lives in the Constitution):

- **"Module Communication"** (lines 99-102) — covered by Constitution Section 5
- **"Event Handler Patterns & Exception Isolation"** (lines 104-152) — covered by Constitution Section 5
- **"EF Core Interceptor DI Patterns"** (lines 227-268) — covered by Constitution Section 4 and 12
- **"Minimal API Parameter Binding"** (lines 292-378) — covered by Constitution Section 6
- **"Database" section** (lines 206-225, including "Unified HostDbContext" subsection) — covered by Constitution Sections 1 and 4

- [ ] **Step 2: Add Constitution reference**

After the "Architecture" section (after "Request Flow"), add:

```markdown
## Module Rules & Architecture

See [docs/CONSTITUTION.md](docs/CONSTITUTION.md) for the authoritative reference on:
- Module boundaries, dependencies, and data ownership
- Communication patterns (contracts and events)
- Endpoint, frontend, and authorization rules
- Compiler-enforced diagnostics (SM0001–SM0043)
- Framework contributor guidelines
```

- [ ] **Step 3: Update Key Constraints**

In the "Key Constraints" section, update line 90 from:
> Module Vite builds use library mode — externalize React, React-DOM, @inertiajs/react. Inline dynamic imports.

To:
> Module Vite builds use library mode — externalize React, React-DOM, @inertiajs/react.

(Removes the incorrect "Inline dynamic imports" claim.)

- [ ] **Step 4: Verify CLAUDE.md**

Read the updated file and confirm:
- Removed sections are gone
- Constitution reference is present and links correctly
- Remaining sections are intact: What This Is, Build & Run, Frontend, Testing, Architecture, Key Constraints, C# Conventions, Pages Registry Pattern, Test Infrastructure, Frontend Packages, CLI, Adding a New Module, Linting & Formatting, AI workflow sections (Plan Node Default through Core Principles)
- No orphaned headers or broken formatting

- [ ] **Step 5: Commit**

```bash
git add CLAUDE.md
git commit -m "Trim CLAUDE.md — move architectural rules to Constitution, add reference link"
```
