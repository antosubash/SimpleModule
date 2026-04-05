#!/usr/bin/env node
// Extracts TypeScript route definitions from TypeScriptRoutes.g.cs
// Usage: node tools/extract-routes.mjs <generated-dir> <output-path>

import { readdirSync, readFileSync, writeFileSync, mkdirSync } from 'fs';
import { resolve, dirname, join } from 'path';

const generatedDir = process.argv[2];
const outputPath =
  process.argv[3] ||
  'template/SimpleModule.Host/ClientApp/routes.ts';

if (!generatedDir) {
  console.error(
    'Usage: node extract-routes.mjs <generated-dir> [output-path]',
  );
  process.exit(1);
}

const files = readdirSync(generatedDir).filter((f) =>
  f === 'TypeScriptRoutes.g.cs',
);

if (files.length === 0) {
  console.log('No TypeScriptRoutes.g.cs found — skipping route extraction.');
  process.exit(0);
}

const content = readFileSync(join(generatedDir, files[0]), 'utf-8').replace(
  /\r\n/g,
  '\n',
);

// Extract TS content from comment block (between /* and */)
const tsMatch = content.match(/\/\*\n\/\/ @routes\n\n([\s\S]*?)\*\//);
if (!tsMatch) {
  console.log('No route definitions found in generated file.');
  process.exit(0);
}

const tsContent = tsMatch[1];
const outPath = resolve(outputPath);

mkdirSync(dirname(outPath), { recursive: true });
writeFileSync(
  outPath,
  `// Auto-generated from endpoint Route constants \u2014 do not edit\n${tsContent}`,
);
console.log(`Wrote routes to ${outPath}`);
