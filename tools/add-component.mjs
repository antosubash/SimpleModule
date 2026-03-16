#!/usr/bin/env node

import { readFileSync, writeFileSync, copyFileSync, existsSync, mkdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');
const UI_DIR = resolve(ROOT, 'src/SimpleModule.UI');
const REGISTRY_PATH = resolve(UI_DIR, 'registry/registry.json');
const TEMPLATES_DIR = resolve(UI_DIR, 'registry/templates');
const COMPONENTS_DIR = resolve(UI_DIR, 'components');
const INDEX_PATH = resolve(COMPONENTS_DIR, 'index.ts');

function loadRegistry() {
  return JSON.parse(readFileSync(REGISTRY_PATH, 'utf-8'));
}

function listComponents(registry) {
  const names = Object.keys(registry).sort();
  if (names.length === 0) {
    console.log('No components in registry.');
    return;
  }

  const installed = new Set();
  for (const name of names) {
    const dest = resolve(COMPONENTS_DIR, registry[name].file);
    if (existsSync(dest)) installed.add(name);
  }

  console.log('\nAvailable components:\n');
  for (const name of names) {
    const status = installed.has(name) ? ' (installed)' : '';
    const radix = registry[name].radixPackages?.length
      ? ` [${registry[name].radixPackages.join(', ')}]`
      : '';
    console.log(`  ${name}${status}${radix}`);
  }
  console.log('');
}

function resolveAllDependencies(registry, names) {
  const resolved = new Set();
  const queue = [...names];

  while (queue.length > 0) {
    const name = queue.shift();
    if (resolved.has(name)) continue;
    if (!registry[name]) {
      console.error(`Unknown component: "${name}"`);
      console.error(`Run with --list to see available components.`);
      process.exit(1);
    }
    resolved.add(name);
    for (const dep of registry[name].dependencies || []) {
      if (!resolved.has(dep)) queue.push(dep);
    }
  }

  return resolved;
}

function installRadixPackages(registry, names) {
  const packages = new Set();
  for (const name of names) {
    for (const pkg of registry[name].radixPackages || []) {
      packages.add(pkg);
    }
  }

  if (packages.size === 0) return;

  // Check which are already installed
  const pkgJsonPath = resolve(UI_DIR, 'package.json');
  const pkgJson = JSON.parse(readFileSync(pkgJsonPath, 'utf-8'));
  const allDeps = { ...pkgJson.dependencies, ...pkgJson.peerDependencies, ...pkgJson.devDependencies };

  const toInstall = [...packages].filter((p) => !allDeps[p]);
  if (toInstall.length === 0) return;

  console.log(`Installing: ${toInstall.join(', ')}`);
  execFileSync('npm', ['install', ...toInstall, '-w', '@simplemodule/ui'], {
    cwd: ROOT,
    stdio: 'inherit',
    shell: true,
  });
}

function copyComponents(registry, names) {
  mkdirSync(COMPONENTS_DIR, { recursive: true });

  const copied = [];
  for (const name of names) {
    const src = resolve(TEMPLATES_DIR, registry[name].file);
    const dest = resolve(COMPONENTS_DIR, registry[name].file);

    if (existsSync(dest)) {
      console.log(`  ${name} — already exists, skipping`);
      continue;
    }

    if (!existsSync(src)) {
      console.error(`  ${name} — template not found at ${src}`);
      process.exit(1);
    }

    copyFileSync(src, dest);
    copied.push(name);
    console.log(`  ${name} — added`);
  }

  return copied;
}

function updateBarrelExport(registry, names) {
  // Collect all installed components (check files on disk)
  const allInstalled = [];
  for (const [name, meta] of Object.entries(registry)) {
    const dest = resolve(COMPONENTS_DIR, meta.file);
    if (existsSync(dest)) {
      allInstalled.push({ name, meta });
    }
  }

  // Sort alphabetically
  allInstalled.sort((a, b) => a.name.localeCompare(b.name));

  // Generate index
  const lines = allInstalled.map(({ meta }) => {
    const exports = meta.exports.join(', ');
    const file = meta.file.replace('.tsx', '');
    return `export { ${exports} } from './${file}';`;
  });

  writeFileSync(INDEX_PATH, lines.join('\n') + '\n');
}

// --- Main ---

const args = process.argv.slice(2);

if (args.length === 0 || args.includes('--help') || args.includes('-h')) {
  console.log(`
Usage: npm run ui:add -- <component...>
       npm run ui:add -- --list

Examples:
  npm run ui:add -- button
  npm run ui:add -- dialog badge card
  npm run ui:add -- --list
`);
  process.exit(0);
}

const registry = loadRegistry();

if (args.includes('--list')) {
  listComponents(registry);
  process.exit(0);
}

const requested = args.filter((a) => !a.startsWith('-'));
const allNames = resolveAllDependencies(registry, requested);

console.log(`\nAdding ${allNames.size} component(s):\n`);

installRadixPackages(registry, allNames);
const copied = copyComponents(registry, allNames);
updateBarrelExport(registry, allNames);

if (copied.length > 0) {
  console.log(`\nDone! ${copied.length} component(s) added to src/SimpleModule.UI/components/`);
} else {
  console.log('\nAll requested components already installed.');
}
