#!/usr/bin/env node
// Generates TypeScript key constants from i18n en.json files.
// Usage: node tools/generate-i18n-keys.mjs [modules-dir]

import { readdirSync, readFileSync, writeFileSync, existsSync, statSync } from 'fs';
import { resolve, join, basename, dirname } from 'path';

const modulesDir = process.argv[2] || 'modules';
let hasErrors = false;

/**
 * Find all en.json files in module Locales directories,
 * skipping *.Contracts projects.
 */
function findEnJsonFiles() {
  const results = [];
  if (!existsSync(modulesDir)) return results;

  for (const moduleDir of readdirSync(modulesDir)) {
    const srcDir = join(modulesDir, moduleDir, 'src');
    if (!existsSync(srcDir) || !statSync(srcDir).isDirectory()) continue;

    for (const project of readdirSync(srcDir)) {
      if (project.endsWith('.Contracts')) continue;

      const localesDir = join(srcDir, project, 'Locales');
      const enJson = join(localesDir, 'en.json');
      if (existsSync(enJson)) {
        results.push({ enJson, localesDir, project });
      }
    }
  }

  return results;
}

/**
 * Build a nested object from dot-separated keys.
 * Returns null if a leaf/branch conflict is detected.
 */
function buildNestedKeys(flatKeys) {
  const root = {};

  for (const key of flatKeys) {
    const parts = key.split('.');
    let current = root;

    for (let i = 0; i < parts.length; i++) {
      const part = parts[i];
      const isLast = i === parts.length - 1;

      if (isLast) {
        if (current[part] !== undefined && typeof current[part] === 'object') {
          console.error(
            `ERROR: Key conflict — '${key}' is a leaf but '${parts.slice(0, i + 1).join('.')}' is already a branch.`,
          );
          hasErrors = true;
          return null;
        }
        current[part] = key;
      } else {
        if (current[part] !== undefined && typeof current[part] !== 'object') {
          console.error(
            `ERROR: Key conflict — '${parts.slice(0, i + 1).join('.')}' is a leaf but '${key}' needs it as a branch.`,
          );
          hasErrors = true;
          return null;
        }
        if (current[part] === undefined) {
          current[part] = {};
        }
        current = current[part];
      }
    }
  }

  return root;
}

/**
 * Render a nested key object as a TypeScript constant string.
 */
function renderObject(obj, indent = '  ') {
  const entries = [];
  const sortedKeys = Object.keys(obj).sort();

  for (const key of sortedKeys) {
    const value = obj[key];
    const safeKey = /^[a-zA-Z_$][a-zA-Z0-9_$]*$/.test(key) ? key : `'${key}'`;

    if (typeof value === 'string') {
      const escaped = value.replaceAll("'", "\\'");
      entries.push(`${indent}${safeKey}: '${escaped}',`);
    } else {
      entries.push(
        `${indent}${safeKey}: {\n${renderObject(value, `${indent}  `)}\n${indent}},`,
      );
    }
  }

  return entries.join('\n');
}

/**
 * Derive PascalCase constant name from project directory name.
 * e.g., "SimpleModule.Products" -> "ProductsKeys"
 */
function deriveConstantName(project) {
  const parts = project.split('.');
  const name = parts[parts.length - 1];
  return `${name}Keys`;
}

// Main
const files = findEnJsonFiles();

if (files.length === 0) {
  console.log('No Locales/en.json files found. Nothing to generate.');
  process.exit(0);
}

for (const { enJson, localesDir, project } of files) {
  const content = readFileSync(enJson, 'utf-8');
  let json;
  try {
    json = JSON.parse(content);
  } catch (e) {
    console.error(`ERROR: Failed to parse ${enJson}: ${e.message}`);
    hasErrors = true;
    continue;
  }

  const flatKeys = Object.keys(json).sort();
  if (flatKeys.length === 0) {
    console.log(`Skipping ${project} — no keys in en.json.`);
    continue;
  }

  const nested = buildNestedKeys(flatKeys);
  if (nested === null) continue;

  const constName = deriveConstantName(project);
  const ts = `export const ${constName} = {\n${renderObject(nested)}\n} as const;\n`;

  const outPath = join(localesDir, 'keys.ts');
  writeFileSync(outPath, ts, 'utf-8');
  console.log(`Generated ${outPath} (${flatKeys.length} keys)`);
}

if (hasErrors) {
  console.error('\nKey generation completed with errors.');
  process.exit(1);
}
