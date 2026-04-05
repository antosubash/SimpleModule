#!/usr/bin/env node

/**
 * typecheck.mjs
 *
 * Runs `tsc --noEmit` in every module and package that has a tsconfig.json.
 * Each project is checked independently so @/* path aliases resolve correctly.
 * All checks run in parallel for speed.
 *
 * Exit codes:
 *   0 = All projects pass type checking
 *   1 = Type errors found
 */

import { spawn } from 'node:child_process';
import { createRequire } from 'node:module';
import fs from 'node:fs';
import path from 'node:path';

const projectRoot = path.resolve(import.meta.dirname, '..');
const modulesDir = path.join(projectRoot, 'modules');
const packagesDir = path.join(projectRoot, 'packages');

const require = createRequire(import.meta.url);
const tscBin = require.resolve('typescript/bin/tsc');

function findProjects(baseDir, layout) {
  const dirs = [];
  if (!fs.existsSync(baseDir)) return dirs;

  for (const entry of fs.readdirSync(baseDir)) {
    const full = path.join(baseDir, entry);
    if (!fs.statSync(full).isDirectory()) continue;

    if (layout === 'flat') {
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

function checkProject(dir) {
  return new Promise((resolve) => {
    const proc = spawn(process.execPath, [tscBin, '--noEmit'], {
      cwd: dir,
      stdio: 'pipe',
    });
    let output = '';
    proc.stdout.on('data', (d) => (output += d));
    proc.stderr.on('data', (d) => (output += d));
    proc.on('close', (code) => resolve({ dir, code, output }));
  });
}

const projects = [
  ...findProjects(modulesDir, 'nested'),
  ...findProjects(packagesDir, 'flat'),
];

const results = await Promise.all(projects.map(checkProject));

const failures = [];
for (const { dir, code, output } of results) {
  const label = path.relative(projectRoot, dir);
  if (code === 0) {
    console.log(`  \u2713 ${label}`);
  } else {
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
process.exit(failures.length > 0 ? 1 : 0);
