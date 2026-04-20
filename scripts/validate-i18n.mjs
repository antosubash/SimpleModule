#!/usr/bin/env node
// Validates i18n locale files across modules.
// Usage: node scripts/validate-i18n.mjs [modules-dir]
//
// Checks:
// 1. Every module with Locales/ has an en.json (base locale)
// 2. Other locale files have the same keys as en.json
//    - Missing keys: warning
//    - Extra keys: error
// 3. Leaf/branch key path conflicts: error

import { readdirSync, readFileSync, existsSync } from 'fs';
import { join, basename } from 'path';
import { findModuleLocales } from './i18n-utils.mjs';

const modulesDir = process.argv[2] || 'modules';
let warnings = 0;
let errors = 0;

/**
 * Check for leaf/branch key path conflicts in a flat key set.
 */
function checkKeyConflicts(keys, file) {
  const branches = new Set();

  for (const key of keys) {
    const parts = key.split('.');
    for (let i = 1; i < parts.length; i++) {
      branches.add(parts.slice(0, i).join('.'));
    }
  }

  for (const key of keys) {
    if (branches.has(key)) {
      console.error(
        `  ERROR: '${key}' is both a leaf key and a branch prefix in ${file}`,
      );
      errors++;
    }
  }
}

/**
 * Parse a JSON file, returning null on failure.
 */
function parseJsonFile(filePath) {
  try {
    return JSON.parse(readFileSync(filePath, 'utf-8'));
  } catch (e) {
    console.error(`  ERROR: Failed to parse ${filePath}: ${e.message}`);
    errors++;
    return null;
  }
}

// Main
const localesDirs = findModuleLocales(modulesDir);

if (localesDirs.length === 0) {
  console.log('No Locales directories found. Nothing to validate.');
  process.exit(0);
}

for (const { localesDir, project } of localesDirs) {
  console.log(`\nValidating ${project}:`);

  const enJsonPath = join(localesDir, 'en.json');
  if (!existsSync(enJsonPath)) {
    console.error(`  ERROR: Missing en.json (base locale) in ${localesDir}`);
    errors++;
    continue;
  }

  const enJson = parseJsonFile(enJsonPath);
  if (enJson === null) continue;

  const enKeys = new Set(Object.keys(enJson));

  // Check for key conflicts in en.json
  checkKeyConflicts([...enKeys], 'en.json');

  // Find other locale files
  const localeFiles = readdirSync(localesDir).filter(
    (f) => f.endsWith('.json') && f !== 'en.json',
  );

  for (const localeFile of localeFiles) {
    const localePath = join(localesDir, localeFile);
    const localeJson = parseJsonFile(localePath);
    if (localeJson === null) continue;

    const localeKeys = new Set(Object.keys(localeJson));
    const locale = basename(localeFile, '.json');

    // Check for key conflicts in this locale
    checkKeyConflicts([...localeKeys], localeFile);

    // Missing keys (in en.json but not in this locale)
    for (const key of enKeys) {
      if (!localeKeys.has(key)) {
        console.warn(`  WARNING: [${locale}] Missing key '${key}'`);
        warnings++;
      }
    }

    // Extra keys (in this locale but not in en.json)
    for (const key of localeKeys) {
      if (!enKeys.has(key)) {
        console.error(`  ERROR: [${locale}] Extra key '${key}' not in en.json`);
        errors++;
      }
    }
  }

  if (localeFiles.length === 0) {
    console.log('  en.json only — no other locales to compare.');
  }
}

console.log(
  `\nValidation complete: ${errors} error(s), ${warnings} warning(s).`,
);

if (errors > 0) {
  process.exit(1);
}
