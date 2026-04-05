#!/usr/bin/env node

/**
 * typecheck.mjs
 *
 * Runs `tsc --noEmit` in every module and package that has a tsconfig.json.
 * Each project is checked independently so @/* path aliases resolve correctly.
 *
 * Exit codes:
 *   0 = All projects pass type checking
 *   1 = Type errors found
 */

import { execSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const projectRoot = path.resolve(import.meta.dirname, '..');
const modulesDir = path.join(projectRoot, 'modules');
const packagesDir = path.join(projectRoot, 'packages');

function findTsConfigs(baseDir, depth) {
  const dirs = [];
  if (!fs.existsSync(baseDir)) return dirs;

  for (const entry of fs.readdirSync(baseDir)) {
    const full = path.join(baseDir, entry);
    if (!fs.statSync(full).isDirectory()) continue;

    if (depth === 1) {
      // packages/Foo — check directly
      if (fs.existsSync(path.join(full, 'tsconfig.json'))) {
        dirs.push(full);
      }
    } else {
      // modules/Foo/src/Bar — recurse into src/*
      const srcDir = path.join(full, 'src');
      if (!fs.existsSync(srcDir)) continue;
      for (const sub of fs.readdirSync(srcDir)) {
        const subFull = path.join(srcDir, sub);
        if (
          fs.statSync(subFull).isDirectory() &&
          fs.existsSync(path.join(subFull, 'tsconfig.json'))
        ) {
          dirs.push(subFull);
        }
      }
    }
  }
  return dirs;
}

const projects = [
  ...findTsConfigs(modulesDir, 2),
  ...findTsConfigs(packagesDir, 1),
];

let failed = false;
const failures = [];

for (const dir of projects) {
  const label = path.relative(projectRoot, dir);
  try {
    execSync('npx tsc --noEmit', { cwd: dir, stdio: 'pipe' });
    console.log(`  \u2713 ${label}`);
  } catch (err) {
    failed = true;
    const output = err.stdout?.toString() || err.stderr?.toString() || '';
    failures.push({ label, output });
    console.log(`  \u2717 ${label}`);
  }
}

if (failures.length > 0) {
  console.log('\n--- Type errors ---\n');
  for (const { label, output } of failures) {
    console.log(`${label}:`);
    console.log(output);
  }
}

console.log(
  `\nTypecheck: ${projects.length - failures.length}/${projects.length} passed`,
);
process.exit(failed ? 1 : 0);
