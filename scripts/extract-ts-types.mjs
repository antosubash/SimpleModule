#!/usr/bin/env node
// Extracts TypeScript interfaces from per-module DtoTypeScript_*.g.cs files
// Usage: node scripts/extract-ts-types.mjs <generated-dir> <modules-dir>

import {
  readdirSync,
  readFileSync,
  writeFileSync,
  mkdirSync,
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
  const projectDir = `SimpleModule.${moduleName}`;

  // Package-installed modules have no source directory here — creating one
  // would make the module look like a source module to tooling (sm list,
  // doctor). Their TS types ship prebuilt inside the package instead.
  if (!existsSync(resolve(modulesDir, moduleName))) continue;

  const outPath = resolve(
    modulesDir,
    moduleName,
    'src',
    projectDir,
    'types.ts',
  );

  mkdirSync(resolve(modulesDir, moduleName, 'src', projectDir), {
    recursive: true,
  });
  const next = `// Auto-generated from [Dto] types \u2014 do not edit\n${tsContent}`;
  if (writeIfChanged(outPath, next)) {
    console.log(`Wrote ${moduleName} types to ${outPath}`);
  }
}
