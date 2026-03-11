#!/usr/bin/env node
// Extracts TypeScript interfaces from generated DtoTypeScript.g.cs
// Usage: node tools/extract-ts-types.mjs <path-to-DtoTypeScript.g.cs> <output-dir>

import { readFileSync, writeFileSync, mkdirSync } from 'fs';
import { dirname, resolve } from 'path';

const inputFile = process.argv[2];
const outputDir = process.argv[3] || 'src/SimpleModule.Api/ClientApp/types';

if (!inputFile) {
  console.error('Usage: node extract-ts-types.mjs <DtoTypeScript.g.cs> <output-dir>');
  process.exit(1);
}

const content = readFileSync(inputFile, 'utf-8');
const match = content.match(/\/\*\n([\s\S]*?)\*\//);

if (!match) {
  console.log('No TypeScript definitions found in generated file.');
  process.exit(0);
}

const tsContent = match[1];
const outPath = resolve(outputDir, 'contracts.d.ts');

mkdirSync(dirname(outPath), { recursive: true });
writeFileSync(outPath, `// Auto-generated from [Dto] types — do not edit\n${tsContent}`);
console.log(`Wrote TypeScript definitions to ${outPath}`);
