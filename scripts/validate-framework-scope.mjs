#!/usr/bin/env node
// Validates framework scope rules (see docs/CONSTITUTION.md Section 13).
// Usage: node scripts/validate-framework-scope.mjs
//
// Checks:
// 1. framework/ only contains projects listed in framework/.allowed-projects
// 2. Sub-projects under modules/{Name}/src/ match SimpleModule.{Name}.{Suffix}
// 3. Sub-projects do not declare [Module]
// 4. tools/ projects are flat-layout SimpleModule.{Name} and never declare [Module]
//    and no module csproj references a tools/ project

import { readdirSync, readFileSync, statSync } from 'fs';
import { resolve, join } from 'path';

const repoRoot = resolve(new URL('..', import.meta.url).pathname);

// Legacy sub-projects that declare [Module] in violation of the rule.
// These predate the framework scope minimization work and will be resolved
// by the phase that absorbs each module:
//   - SimpleModule.Agents.Module → Phase 3 (Agents absorption)
//   - SimpleModule.Rag.Module    → Phase 4 (Rag absorption)
// TODO: remove entries here as each phase lands. This set must be empty
// before the framework migration is declared complete.
// See: docs/superpowers/specs/2026-04-20-framework-scope-minimization-design.md
const LEGACY_MODULE_SUBPROJECTS_GRANDFATHERED = new Set([
  'SimpleModule.Agents.Module',
  'SimpleModule.Rag.Module',
]);

const errors = [];

function exists(path) {
  try {
    statSync(path);
    return true;
  } catch {
    return false;
  }
}

function walkCsFiles(dir) {
  const results = [];
  const stack = [dir];
  while (stack.length > 0) {
    const current = stack.pop();
    let entries;
    try {
      entries = readdirSync(current, { withFileTypes: true });
    } catch {
      continue;
    }
    for (const entry of entries) {
      const full = join(current, entry.name);
      if (entry.isDirectory()) {
        if (
          entry.name === 'bin' ||
          entry.name === 'obj' ||
          entry.name === 'node_modules'
        ) {
          continue;
        }
        stack.push(full);
      } else if (entry.isFile() && entry.name.endsWith('.cs')) {
        results.push(full);
      }
    }
  }
  return results;
}

function readAllowlist() {
  const path = join(repoRoot, 'framework', '.allowed-projects');
  if (!exists(path)) {
    errors.push(
      `framework/.allowed-projects is missing. Create it and list the ` +
        `projects currently under framework/ (one per line).`,
    );
    return [];
  }
  return readFileSync(path, 'utf8')
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.length > 0 && !line.startsWith('#'));
}

function checkFrameworkAllowlist() {
  const allowed = new Set(readAllowlist());
  const frameworkDir = join(repoRoot, 'framework');
  const entries = readdirSync(frameworkDir, { withFileTypes: true });
  for (const entry of entries) {
    if (!entry.isDirectory()) continue;
    if (!allowed.has(entry.name)) {
      errors.push(
        `framework/${entry.name} is not in framework/.allowed-projects. ` +
          `Add it with reviewer approval, or move it out of framework/.`,
      );
    }
  }
}

function checkSubProjectNaming() {
  const modulesDir = join(repoRoot, 'modules');
  if (!exists(modulesDir)) return;
  const moduleDirs = readdirSync(modulesDir, { withFileTypes: true }).filter(
    (e) => e.isDirectory(),
  );
  for (const moduleEntry of moduleDirs) {
    const moduleName = moduleEntry.name;
    const srcDir = join(modulesDir, moduleName, 'src');
    if (!exists(srcDir)) continue;
    const projectDirs = readdirSync(srcDir, { withFileTypes: true }).filter(
      (e) => e.isDirectory(),
    );
    const expectedPrefix = `SimpleModule.${moduleName}`;
    for (const projectEntry of projectDirs) {
      const name = projectEntry.name;
      // Must match SimpleModule.{ModuleName} or SimpleModule.{ModuleName}.{Suffix}
      if (name !== expectedPrefix && !name.startsWith(`${expectedPrefix}.`)) {
        errors.push(
          `modules/${moduleName}/src/${name}/ does not match required ` +
            `pattern '${expectedPrefix}[.*]'. Sub-projects must be named ` +
            `'${expectedPrefix}.{Suffix}'.`,
        );
      }
    }
  }
}

function checkSubProjectNoModuleAttribute() {
  const modulesDir = join(repoRoot, 'modules');
  if (!exists(modulesDir)) return;
  const moduleDirs = readdirSync(modulesDir, { withFileTypes: true }).filter(
    (e) => e.isDirectory(),
  );
  for (const moduleEntry of moduleDirs) {
    const moduleName = moduleEntry.name;
    const srcDir = join(modulesDir, moduleName, 'src');
    if (!exists(srcDir)) continue;
    const projectDirs = readdirSync(srcDir, { withFileTypes: true }).filter(
      (e) => e.isDirectory(),
    );
    for (const projectEntry of projectDirs) {
      const name = projectEntry.name;
      const isMain = name === `SimpleModule.${moduleName}`;
      const isContracts = name === `SimpleModule.${moduleName}.Contracts`;
      if (isMain || isContracts) continue;
      if (LEGACY_MODULE_SUBPROJECTS_GRANDFATHERED.has(name)) continue;
      // This is a sub-project. Scan its .cs files for [Module(
      const files = walkCsFiles(join(srcDir, name));
      for (const file of files) {
        const content = readFileSync(file, 'utf8');
        if (/\[\s*Module\s*\(/.test(content)) {
          errors.push(
            `Sub-project ${name} declares [Module] in ${file.substring(repoRoot.length + 1)}. ` +
              `Only the main module assembly (SimpleModule.${moduleName}) may declare [Module].`,
          );
        }
      }
    }
  }
}

function checkToolsLayering() {
  const toolsDir = join(repoRoot, 'tools');
  if (exists(toolsDir)) {
    const entries = readdirSync(toolsDir, { withFileTypes: true });
    for (const entry of entries) {
      if (!entry.isDirectory()) continue;
      if (!entry.name.startsWith('SimpleModule.')) {
        errors.push(
          `tools/${entry.name}/ does not match required naming 'SimpleModule.{Name}'.`,
        );
        continue;
      }
      const toolPath = join(toolsDir, entry.name);
      const files = walkCsFiles(toolPath);
      for (const file of files) {
        const content = readFileSync(file, 'utf8');
        if (/\[\s*Module\s*\(/.test(content)) {
          errors.push(
            `tools/${entry.name} declares [Module] in ${file.substring(repoRoot.length + 1)}. ` +
              `Tools are not modules and must not declare [Module].`,
          );
        }
      }
    }
  }
  // Check no module csproj references a tools/ project.
  const modulesDir = join(repoRoot, 'modules');
  if (!exists(modulesDir)) return;
  const moduleDirs = readdirSync(modulesDir, { withFileTypes: true }).filter(
    (e) => e.isDirectory(),
  );
  for (const moduleEntry of moduleDirs) {
    const srcDir = join(modulesDir, moduleEntry.name, 'src');
    if (!exists(srcDir)) continue;
    const projectDirs = readdirSync(srcDir, { withFileTypes: true }).filter(
      (e) => e.isDirectory(),
    );
    for (const projectEntry of projectDirs) {
      const csprojPath = join(
        srcDir,
        projectEntry.name,
        `${projectEntry.name}.csproj`,
      );
      if (!exists(csprojPath)) continue;
      const content = readFileSync(csprojPath, 'utf8');
      // Match ProjectReference paths containing tools/ or tools\
      if (/ProjectReference[^>]*Include="[^"]*[\\/]tools[\\/]/.test(content)) {
        errors.push(
          `${csprojPath.substring(repoRoot.length + 1)} references a tools/ project. ` +
            `Modules may not depend on tools/ — tools are for host/framework only.`,
        );
      }
    }
  }
}

checkFrameworkAllowlist();
checkSubProjectNaming();
checkSubProjectNoModuleAttribute();
checkToolsLayering();

if (errors.length > 0) {
  console.error('Framework scope validation failed:\n');
  for (const err of errors) console.error(`  ✗ ${err}`);
  process.exit(1);
}

console.log('✓ Framework scope validation passed');
