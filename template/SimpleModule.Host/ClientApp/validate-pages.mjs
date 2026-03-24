#!/usr/bin/env node

/**
 * validate-pages.mjs
 *
 * Automated validation script that detects missing or extra page registrations
 * between C# endpoints and TypeScript Pages/index.ts files.
 *
 * This script:
 * 1. Scans all C# files in each module's src/{ModuleName} directory
 * 2. Finds all Inertia.Render("ComponentName/...") calls
 * 3. Scans the module's Pages/index.ts file
 * 4. Finds all keys in the pages object export
 * 5. Compares the two lists and reports mismatches
 *
 * Exit codes:
 *   0 = All modules have valid registrations
 *   1 = Mismatches found
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const projectRoot = path.resolve(__dirname, '../../..');
const modulesDir = path.resolve(projectRoot, 'modules');

/**
 * Recursively find all .cs files in a directory
 */
function findCSharpFiles(dir) {
  const files = [];

  function walk(currentPath) {
    const entries = fs.readdirSync(currentPath, { withFileTypes: true });

    for (const entry of entries) {
      const fullPath = path.join(currentPath, entry.name);

      if (entry.isDirectory()) {
        walk(fullPath);
      } else if (entry.isFile() && entry.name.endsWith('.cs')) {
        files.push(fullPath);
      }
    }
  }

  if (fs.existsSync(dir)) {
    walk(dir);
  }

  return files;
}

/**
 * Extract all Inertia.Render component names from a C# file
 * Pattern: Inertia\.Render\s*\(\s*"([^"]+)"
 */
function findCSharpEndpoints(content) {
  const pattern = /Inertia\.Render\s*\(\s*"([^"]+)"/g;
  const matches = new Set();
  let match;

  while ((match = pattern.exec(content)) !== null) {
    matches.add(match[1]);
  }

  return matches;
}

/**
 * Extract all page keys from a TypeScript Pages/index.ts file
 * Pattern: '([^']+)'\s*:\s*(?:\(\)|import)
 * Ignores commented lines
 */
function findTypeScriptPages(content) {
  const pattern = /'([^']+)'\s*:\s*(?:\(\)|import)/g;
  const matches = new Set();
  const lines = content.split('\n');

  for (const line of lines) {
    // Skip lines that are fully commented out
    const trimmed = line.trim();
    if (trimmed.startsWith('//')) continue;

    let match;
    while ((match = pattern.exec(line)) !== null) {
      matches.add(match[1]);
    }
  }

  return matches;
}

/**
 * Validate a single module
 */
function validateModule(modulePath) {
  const moduleName = path.basename(modulePath);
  const srcPath = path.join(modulePath, 'src', moduleName);

  // Find all C# endpoints
  const csharpFiles = findCSharpFiles(srcPath);
  const csharpEndpoints = new Set();

  for (const filePath of csharpFiles) {
    const content = fs.readFileSync(filePath, 'utf-8');
    const endpoints = findCSharpEndpoints(content);

    for (const endpoint of endpoints) {
      csharpEndpoints.add(endpoint);
    }
  }

  // Find all TS pages
  const pagesIndexPath = path.join(srcPath, 'Pages', 'index.ts');
  let tsPages = new Set();

  if (fs.existsSync(pagesIndexPath)) {
    const content = fs.readFileSync(pagesIndexPath, 'utf-8');
    tsPages = findTypeScriptPages(content);
  }

  // Compare
  const missing = Array.from(csharpEndpoints).filter(
    (ep) => !tsPages.has(ep)
  );
  const extra = Array.from(tsPages).filter((page) => !csharpEndpoints.has(page));

  return {
    moduleName,
    hasPages: fs.existsSync(pagesIndexPath),
    missing,
    extra,
    isValid: missing.length === 0 && extra.length === 0,
  };
}

/**
 * Main validation logic
 */
function main() {
  if (!fs.existsSync(modulesDir)) {
    console.error(`Error: modules directory not found at ${modulesDir}`);
    process.exit(1);
  }

  const results = [];
  const entries = fs.readdirSync(modulesDir, { withFileTypes: true });

  for (const entry of entries) {
    if (!entry.isDirectory()) continue;

    const modulePath = path.join(modulesDir, entry.name);
    const result = validateModule(modulePath);
    results.push(result);
  }

  // Print results
  console.log('\n=== Pages Registry Validation ===\n');

  const invalid = results.filter((r) => !r.isValid);

  if (invalid.length === 0) {
    console.log('✅ All modules have valid Pages/index.ts registrations\n');
    process.exit(0);
  }

  for (const result of invalid) {
    console.log(`❌ Module: ${result.moduleName}`);

    if (result.missing.length > 0) {
      console.log(`   Missing in Pages/index.ts: ${result.missing.join(', ')}`);
    }

    if (result.extra.length > 0) {
      console.log(`   Extra in Pages/index.ts: ${result.extra.join(', ')}`);
    }

    console.log();
  }

  console.log(
    `❌ Found ${invalid.length} module(s) with mismatches`
  );
  console.log(
    'Please update the Pages/index.ts files to match C# endpoints.\n'
  );

  process.exit(1);
}

main();
