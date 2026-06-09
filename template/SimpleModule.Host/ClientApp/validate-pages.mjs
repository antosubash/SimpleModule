#!/usr/bin/env node

/**
 * validate-pages.mjs
 *
 * Automated validation script that detects missing or extra page registrations
 * between C# endpoints and TypeScript Pages/index.ts files.
 *
 * This script:
 * 1. Scans all C# files in each module's src/ directory (the implementation
 *    project lives at src/SimpleModule.{ModuleName}; obj/bin/wwwroot are skipped)
 * 2. Finds all Inertia.Render("ComponentName/...") calls
 * 3. Scans the module's Pages/index.ts file
 * 4. Finds all keys in the pages object export
 * 5. Compares the two lists and reports mismatches
 *
 * Self-check: if zero C# files or zero Inertia.Render endpoints are found
 * across the whole repo, the script fails. A layout change must never be able
 * to silently turn this guard into a no-op again (it did once: the script
 * scanned src/{ModuleName} while the real layout is src/SimpleModule.{ModuleName},
 * so it validated zero files and always reported success).
 *
 * Exit codes:
 *   0 = All modules have valid registrations
 *   1 = Mismatches found, or self-check failed
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const projectRoot = path.resolve(__dirname, '../../..');
const modulesDir = path.resolve(projectRoot, 'modules');

// Build-output and vendor directories that must never be scanned — stale
// artifacts in obj/bin can contain Inertia.Render strings for deleted pages.
const SKIPPED_DIRS = new Set(['obj', 'bin', 'node_modules', 'wwwroot', 'dist']);

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
        if (SKIPPED_DIRS.has(entry.name)) continue;
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
 * Extract all Inertia.Render component names from a C# file.
 * Handles both inline literals — Inertia.Render("Module/Page", ...) — and
 * identifiers resolved against `const string Name = "Module/Page";`
 * declarations in the same file (the ComponentName pattern).
 * Returns { names, unresolved } where unresolved lists identifier arguments
 * that could not be resolved to a string in this file.
 */
function findCSharpEndpoints(content) {
  const names = new Set();
  const unresolved = new Set();

  const literalPattern = /Inertia\.Render\s*\(\s*"([^"]+)"/g;
  let match = literalPattern.exec(content);
  while (match !== null) {
    names.add(match[1]);
    match = literalPattern.exec(content);
  }

  const identifierPattern = /Inertia\.Render\s*\(\s*([A-Za-z_][A-Za-z0-9_.]*)\s*[,)]/g;
  match = identifierPattern.exec(content);
  while (match !== null) {
    const identifier = match[1];
    const constName = identifier.split('.').pop();
    const constPattern = new RegExp(`const\\s+string\\s+${constName}\\s*=\\s*"([^"]+)"`);
    const constMatch = constPattern.exec(content);
    if (constMatch) {
      names.add(constMatch[1]);
    } else {
      unresolved.add(identifier);
    }
    match = identifierPattern.exec(content);
  }

  return { names, unresolved };
}

/**
 * Extract all page keys from a TypeScript Pages/index.ts file
 * Handles: 'key': () => import(...), 'key': import(...), "key": import(...), etc.
 * Ignores commented lines
 */
function findTypeScriptPages(content) {
  const matches = new Set();
  const lines = content.split('\n');

  for (const line of lines) {
    // Skip lines that are fully commented out
    const trimmed = line.trim();
    if (trimmed.startsWith('//')) continue;

    // Match single or double quoted keys with various import syntaxes
    const pattern = /['"`]([^'"`]+)['"`]\s*:\s*(?:\(\s*\)|(?:async\s*)?\(\s*\)\s*=>|import)/g;
    let match = pattern.exec(line);
    while (match !== null) {
      matches.add(match[1]);
      match = pattern.exec(line);
    }
  }

  return matches;
}

/**
 * Validate a single module
 */
function validateModule(modulePath) {
  const moduleName = path.basename(modulePath);
  const srcRoot = path.join(modulePath, 'src');

  // Implementation projects live at src/SimpleModule.{ModuleName} (plus a
  // Contracts sibling). Scan every project directory under src/ rather than
  // hard-coding one name, so a layout rename cannot silently skip files.
  const projectDirs = fs.existsSync(srcRoot)
    ? fs
        .readdirSync(srcRoot, { withFileTypes: true })
        .filter((e) => e.isDirectory() && !SKIPPED_DIRS.has(e.name))
        .map((e) => path.join(srcRoot, e.name))
    : [];

  // Find all C# endpoints
  const csharpEndpoints = new Set();
  const unresolvedRenders = [];
  let csharpFileCount = 0;

  for (const projectDir of projectDirs) {
    for (const filePath of findCSharpFiles(projectDir)) {
      csharpFileCount += 1;
      const content = fs.readFileSync(filePath, 'utf-8');
      const { names, unresolved } = findCSharpEndpoints(content);

      for (const endpoint of names) {
        csharpEndpoints.add(endpoint);
      }

      for (const identifier of unresolved) {
        unresolvedRenders.push(`${path.relative(modulePath, filePath)}: ${identifier}`);
      }
    }
  }

  // Find all TS pages (Pages/index.ts in the implementation project)
  const tsPages = new Set();
  let hasPages = false;

  for (const projectDir of projectDirs) {
    const pagesIndexPath = path.join(projectDir, 'Pages', 'index.ts');

    try {
      const content = fs.readFileSync(pagesIndexPath, 'utf-8');
      for (const page of findTypeScriptPages(content)) {
        tsPages.add(page);
      }
      hasPages = true;
    } catch (err) {
      if (err.code !== 'ENOENT') throw err; // Re-throw non-file-not-found errors
    }
  }

  // Compare
  const missing = Array.from(csharpEndpoints).filter((ep) => !tsPages.has(ep));
  const extra = Array.from(tsPages).filter((page) => !csharpEndpoints.has(page));

  return {
    moduleName,
    hasPages,
    csharpFileCount,
    endpointCount: csharpEndpoints.size,
    missing,
    extra,
    unresolvedRenders,
    isValid: missing.length === 0 && extra.length === 0 && unresolvedRenders.length === 0,
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

  // Self-check: this guard once silently validated nothing because the module
  // layout changed underneath it. If the scan finds no C# files or no
  // Inertia.Render endpoints at all, the paths are wrong — fail loudly.
  const totalCsFiles = results.reduce((sum, r) => sum + r.csharpFileCount, 0);
  const totalEndpoints = results.reduce((sum, r) => sum + r.endpointCount, 0);

  if (totalCsFiles === 0) {
    console.error('❌ Self-check failed: scanned 0 C# files across all modules.');
    console.error('   The module source layout has likely changed — update validate-pages.mjs.\n');
    process.exit(1);
  }

  if (totalEndpoints === 0) {
    console.error('❌ Self-check failed: found 0 Inertia.Render endpoints across all modules.');
    console.error('   The module source layout has likely changed — update validate-pages.mjs.\n');
    process.exit(1);
  }

  const invalid = results.filter((r) => !r.isValid);

  if (invalid.length === 0) {
    console.log(
      `✅ All modules valid (${totalEndpoints} endpoints across ${totalCsFiles} C# files)\n`,
    );
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

    if (result.unresolvedRenders.length > 0) {
      console.log(
        '   Inertia.Render arguments that could not be resolved to a string ' +
          '(use a literal or a same-file const):',
      );
      for (const entry of result.unresolvedRenders) {
        console.log(`     ${entry}`);
      }
    }

    console.log();
  }

  console.log(`❌ Found ${invalid.length} module(s) with mismatches`);
  console.log('Please update the Pages/index.ts files to match C# endpoints.\n');

  process.exit(1);
}

main();
