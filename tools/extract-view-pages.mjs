#!/usr/bin/env node
// Extracts auto-generated Pages/index.ts from ViewPages_*.g.cs files
// Usage: node tools/extract-view-pages.mjs <generated-cs-file> <output-dir>
// Example: node tools/extract-view-pages.mjs obj/Debug/.../ViewPages_Products.g.cs src/modules/Products/src/Products/Pages

import { readFileSync, writeFileSync, mkdirSync } from 'fs';
import { dirname, resolve } from 'path';

const inputFile = process.argv[2];
const outputDir = process.argv[3];

if (!inputFile || !outputDir) {
  console.error(
    'Usage: node extract-view-pages.mjs <ViewPages_*.g.cs> <output-dir>',
  );
  process.exit(1);
}

const content = readFileSync(inputFile, 'utf-8');
const match = content.match(/\/\*\n([\s\S]*?)\*\//);

if (!match) {
  console.log('No view pages definitions found in generated file.');
  process.exit(0);
}

const tsContent = match[1];
const outPath = resolve(outputDir, 'index.ts');

mkdirSync(dirname(outPath), { recursive: true });
writeFileSync(
  outPath,
  `// Auto-generated from [View] types — do not edit\n${tsContent}`,
);
console.log(`Wrote view pages to ${outPath}`);
