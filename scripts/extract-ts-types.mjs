#!/usr/bin/env node
// Extracts TypeScript interfaces from per-module DtoTypeScript_*.g.cs files
// Usage: node scripts/extract-ts-types.mjs <generated-dir> <modules-dir>

import {
  readdirSync,
  readFileSync,
  writeFileSync,
  statSync,
  existsSync,
} from 'fs';
import { resolve, join } from 'path';

function writeIfChanged(path, contents) {
  if (existsSync(path) && readFileSync(path, 'utf-8') === contents) {
    return false;
  }
  writeFileSync(path, contents);
  return true;
}

function isDirectory(path) {
  try {
    return statSync(path).isDirectory();
  } catch {
    return false;
  }
}

/**
 * Resolves the directory of a module's primary source project, or null when the
 * module has no source under `modulesDir`.
 *
 * The generator emits a DtoTypeScript_*.g.cs for every module in the compilation,
 * which includes modules that arrived as NuGet packages. Those have no source in
 * the consumer's repo, and the directory layout is not guessable either: framework
 * modules use `src/SimpleModule.<Name>/` while `sm new module` scaffolds a bare
 * `src/<Name>/`. So only ever write into a project directory that already exists —
 * never create one.
 */
function findProjectDir(modulesDir, moduleName) {
  const srcDir = resolve(modulesDir, moduleName, 'src');
  if (!isDirectory(srcDir)) return null;

  for (const candidate of [`SimpleModule.${moduleName}`, moduleName]) {
    const dir = join(srcDir, candidate);
    if (existsSync(join(dir, `${candidate}.csproj`))) return dir;
  }

  // Non-conventional project name: accept it when there is exactly one candidate
  // left after excluding the companion Contracts and Tests projects.
  const projects = readdirSync(srcDir, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name)
    .filter((name) => !name.endsWith('.Contracts') && !name.endsWith('.Tests'))
    .filter((name) => existsSync(join(srcDir, name, `${name}.csproj`)));

  return projects.length === 1 ? join(srcDir, projects[0]) : null;
}

const generatedDir = process.argv[2];
const modulesDir = process.argv[3] || 'modules';

if (!generatedDir) {
  console.error(
    'Usage: node extract-ts-types.mjs <generated-dir> [modules-dir]',
  );
  process.exit(1);
}

const files = readdirSync(generatedDir).filter((f) =>
  f.match(/^DtoTypeScript_\w+\.g\.cs$/),
);

if (files.length === 0) {
  console.log('No per-module TypeScript definition files found.');
  process.exit(0);
}

const skipped = [];

for (const file of files) {
  const content = readFileSync(join(generatedDir, file), 'utf-8').replace(
    /\r\n/g,
    '\n',
  );

  // Extract module name from @module comment
  const moduleMatch = content.match(/\/\/ @module (\w+)/);
  if (!moduleMatch) continue;
  const moduleName = moduleMatch[1];

  // Extract TS interfaces from comment block
  const tsMatch = content.match(/\/\*\n\/\/ @module \w+\n\n([\s\S]*?)\*\//);
  if (!tsMatch) continue;

  const tsContent = tsMatch[1];
  const projectDir = findProjectDir(modulesDir, moduleName);

  if (!projectDir) {
    skipped.push(moduleName);
    continue;
  }

  const outPath = join(projectDir, 'types.ts');
  const next = `// Auto-generated from [Dto] types \u2014 do not edit\n${tsContent}`;
  if (writeIfChanged(outPath, next)) {
    console.log(`Wrote ${moduleName} types to ${outPath}`);
  }
}

if (skipped.length > 0) {
  console.log(
    `Skipped ${skipped.length} module(s) with no source project under ${modulesDir}: ${skipped.join(', ')}`,
  );
}
