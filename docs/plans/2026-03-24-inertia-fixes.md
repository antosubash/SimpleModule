# Inertia.js Integration Fixes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Fix deployment cache-coherence issues with the Inertia.js integration and add validation to prevent Pages/index.ts sync errors.

**Architecture:**
1. Replace per-instance `CacheBuster` with stable, deployment-aware identifier (environment variable with fallback)
2. Replace hardcoded `Version` with dynamic value matching deployment identifier
3. Document the Pages/index.ts sync requirement in CLAUDE.md
4. Create a validation script that runs post-build to catch missing endpoint registrations
5. Verify all changes work end-to-end

**Tech Stack:** C# (.NET 10), Node.js, environment variables, git (optional for hashing)

---

## Task 1: Update InertiaPageRenderer.cs - Replace CacheBuster

**Files:**
- Modify: `framework/SimpleModule.Blazor/Inertia/InertiaPageRenderer.cs`
- Test: No automated test (manual verification in Task 6)

**Objective:** Replace Unix timestamp-based CacheBuster with deployment-aware identifier to prevent cache desync in rolling deployments.

**Implementation:** Replace static field with method that checks DEPLOYMENT_VERSION env var first, falls back to assembly version.

**Success criteria:**
- Code compiles without errors
- CacheBuster uses env var when set
- Fallback to assembly version in dev
- Value is stable across instance restarts

---

## Task 2: Update InertiaMiddleware.cs - Replace Version

**Files:**
- Modify: `framework/SimpleModule.Core/Inertia/InertiaMiddleware.cs`
- Test: No automated test (manual verification in Task 6)

**Objective:** Replace hardcoded Version string with dynamic value that matches CacheBuster for 409 stale-version detection to work.

**Implementation:** Replace const with static readonly field populated by method that uses DEPLOYMENT_VERSION env var, fallback to assembly version.

**Success criteria:**
- Code compiles without errors
- Version uses env var when set
- Fallback to assembly version in dev
- Version is stable across instance restarts

---

## Task 3: Update CLAUDE.md - Document Pages/index.ts Sync Requirement

**Files:**
- Modify: `CLAUDE.md`
- Test: No automated test (documentation only)

**Objective:** Document the critical Pages/index.ts sync requirement to prevent silent failures when endpoints are added.

**Implementation:** Add new section after "Module Communication" explaining the pattern, why it's critical, and the validation workflow.

**Success criteria:**
- Section is clear and actionable
- Pattern is documented with examples
- Validation script reference is included
- Consequences of missing sync are explained

---

## Task 4: Create Validation Script - validate-pages.mjs

**Files:**
- Create: `template/SimpleModule.Host/ClientApp/validate-pages.mjs`
- Modify: `template/SimpleModule.Host/package.json` (add npm script entry)
- Test: Run script manually in Task 5

**Objective:** Create automated validation script that detects missing or extra page registrations.

**Implementation:** Node.js script that:
1. Scans C# files for `Inertia.Render("ComponentName/...")` calls
2. Scans `Pages/index.ts` for page registry entries
3. Reports mismatches with clear error messages
4. Exits with code 1 on errors (CI-friendly)
5. Add `validate-pages` npm script

**Success criteria:**
- Script runs without errors
- Finds all Inertia.Render calls in C# correctly
- Parses Pages/index.ts correctly
- Reports clear error messages
- Exits with correct codes
- npm script is callable

---

## Task 5: Test the Validation Script

**Files:**
- No files to modify (verification only)

**Objective:** Verify validation script works correctly on current codebase and catches missing registrations.

**Implementation:**
1. Run validation script - should pass
2. Temporarily comment out a page entry
3. Run validation script - should fail with clear error
4. Uncomment and verify passes again

**Success criteria:**
- Script passes on clean codebase
- Script detects intentional missing entries
- Error messages are clear and actionable
- Script recovers after fixes

---

## Task 6: Build and Run - Verify All Changes

**Files:**
- No files to modify (verification only)

**Objective:** Verify the application builds, runs, and headers are correctly set.

**Implementation:**
1. Clean build with dotnet build
2. Run application
3. Check X-Inertia-Version header shows assembly version (not timestamp)
4. Test DEPLOYMENT_VERSION env var override
5. Run full test suite
6. Run validation script
7. Verify git commits are clean

**Success criteria:**
- Application builds without errors
- Application runs on https://localhost:5001
- X-Inertia-Version header shows version (not Unix timestamp)
- DEPLOYMENT_VERSION env var override works
- All tests pass
- Validation script passes
- 4 clean commits are created
