#!/usr/bin/env node
// Build vendor ESM bundles from CJS packages with proper named exports.
// Uses esbuild API to create bundles, then post-processes to:
// 1. Replace __require("pkg") calls with ESM imports (CJS externals don't work in browsers)
// 2. Add named exports from the default export

import { build } from 'esbuild';
import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { createRequire } from 'module';

const __dirname = dirname(fileURLToPath(import.meta.url));
const outDir = resolve(__dirname, '../wwwroot/js/vendor');
const require_ = createRequire(import.meta.url);

if (!existsSync(outDir)) mkdirSync(outDir, { recursive: true });

async function getExports(pkg) {
  try {
    const mod = require_(pkg);
    return Object.keys(mod).filter(k => k !== 'default' && k !== '__esModule');
  } catch {
    try {
      const mod = await import(pkg);
      return Object.keys(mod).filter(k => k !== 'default' && k !== '__esModule');
    } catch {
      return [];
    }
  }
}

async function buildVendor(pkg, file, externals = []) {
  const outFile = resolve(outDir, file);
  const exports = await getExports(pkg);

  console.log(`Building ${pkg} → ${file} (${exports.length} named exports)`);

  await build({
    entryPoints: [pkg],
    bundle: true,
    format: 'esm',
    platform: 'browser',
    external: externals,
    outfile: outFile,
    logLevel: 'warning',
  });

  let content = readFileSync(outFile, 'utf-8');

  // Step 1: Replace __require("pkg") calls with ESM imports.
  // esbuild's CJS externals produce __require() calls which fail in browsers.
  // We add ESM imports at the top and replace __require("pkg") with the imported namespace.
  const importLines = [];
  for (let i = 0; i < externals.length; i++) {
    const ext = externals[i];
    const varName = `__ext${i}`;
    // Escape for regex: @ and /
    const escaped = ext.replace(/[.*+?^${}()|[\]\\\/]/g, '\\$&');
    const re = new RegExp(`__require\\("${escaped}"\\)`, 'g');
    if (re.test(content)) {
      importLines.push(`import * as ${varName} from "${ext}";`);
      content = content.replace(re, varName);
    }
  }
  if (importLines.length > 0) {
    content = importLines.join('\n') + '\n' + content;
    console.log(`  Replaced __require() calls for: ${externals.filter((_, i) => content.includes(`__ext${i}`)).join(', ') || externals.join(', ')}`);
  }

  // Step 2: Add named exports from the default export.
  // esbuild's CJS→ESM only creates `export default`, not individual named exports.
  if (exports.length > 0) {
    const re = /export\s+default\s+(.+?)\s*;\s*$/m;
    const match = content.match(re);
    if (match) {
      const callExpr = match[1];
      const namedExports = exports.map(e => `  ${e}`).join(',\n');
      content = content.replace(
        match[0],
        `var __mod = ${callExpr};\nexport default __mod;\nexport var {\n${namedExports}\n} = __mod;\n`
      );
      console.log(`  Added ${exports.length} named exports`);
    }
  }

  writeFileSync(outFile, content);
}

const vendors = [
  { pkg: 'react', file: 'react.js', externals: [] },
  { pkg: 'react-dom', file: 'react-dom.js', externals: ['react'] },
  { pkg: 'react/jsx-runtime', file: 'react-jsx-runtime.js', externals: ['react'] },
  { pkg: 'react-dom/client', file: 'react-dom-client.js', externals: ['react', 'react-dom'] },
  { pkg: '@inertiajs/react', file: 'inertiajs-react.js', externals: ['react', 'react-dom', 'react/jsx-runtime', 'react-dom/client'] },
];

for (const v of vendors) {
  await buildVendor(v.pkg, v.file, v.externals);
}

console.log('\nVendor bundles built successfully!');
