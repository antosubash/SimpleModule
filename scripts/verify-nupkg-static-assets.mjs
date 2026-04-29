#!/usr/bin/env node
// Verify packed module .nupkgs contain their built static web assets.
//
// For every UI-shipping module (modules/*/src/*/ with a package.json next to
// the .csproj), assert the corresponding `{Id}.{version}.nupkg` contains
// `staticwebassets/{Id}.pages.js` (the SDK strips the `wwwroot/` prefix
// when packaging) and at least one .mjs code-split chunk — that combination
// matches the failure mode where 0.0.33 shipped only the .dll and content/.
//
// Usage: node scripts/verify-nupkg-static-assets.mjs <nupkg-dir>

import { execFileSync } from 'node:child_process';
import { existsSync, readdirSync, statSync } from 'node:fs';
import { basename, dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');

const nupkgDir = process.argv[2];
if (!nupkgDir) {
  console.error('Usage: verify-nupkg-static-assets.mjs <nupkg-dir>');
  process.exit(2);
}

const modulesDir = join(repoRoot, 'modules');
const moduleDirs = readdirSync(modulesDir)
  .map((name) => join(modulesDir, name, 'src'))
  .filter((p) => existsSync(p) && statSync(p).isDirectory())
  .flatMap((srcDir) =>
    readdirSync(srcDir)
      .map((name) => join(srcDir, name))
      .filter((p) => statSync(p).isDirectory()),
  );

const uiModules = moduleDirs.filter((dir) => {
  const csproj = readdirSync(dir).find((f) => f.endsWith('.csproj'));
  return csproj && existsSync(join(dir, 'package.json')) && existsSync(join(dir, 'Pages', 'index.ts'));
});

if (uiModules.length === 0) {
  console.error('No UI-shipping modules discovered under modules/.');
  process.exit(2);
}

const nupkgs = readdirSync(nupkgDir).filter((f) => f.endsWith('.nupkg') && !f.endsWith('.symbols.nupkg'));

let failures = 0;

const escapeRegExp = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

for (const moduleDir of uiModules) {
  const id = basename(moduleDir);
  const matcher = new RegExp(`^${escapeRegExp(id)}\\.\\d+\\.\\d+\\.\\d+(?:[-+][\\w.+-]+)?\\.nupkg$`);
  const nupkg = nupkgs.find((f) => matcher.test(f));
  if (!nupkg) {
    console.error(`FAIL ${id}: no matching .nupkg in ${nupkgDir}`);
    failures++;
    continue;
  }

  const listing = execFileSync('unzip', ['-Z1', join(nupkgDir, nupkg)], { encoding: 'utf8' })
    .split('\n')
    .filter(Boolean);

  const pagesJs = `staticwebassets/${id}.pages.js`;
  const hasPagesJs = listing.includes(pagesJs);
  const hasMjsChunk = listing.some((p) => p.startsWith('staticwebassets/') && p.endsWith('.mjs'));
  const hasBuildProps = listing.includes(`build/${id}.props`);

  if (hasPagesJs && hasMjsChunk && hasBuildProps) {
    console.log(`OK   ${id}: ${nupkg}`);
  } else {
    console.error(`FAIL ${id}: ${nupkg}`);
    if (!hasPagesJs) console.error(`     missing ${pagesJs}`);
    if (!hasMjsChunk) console.error('     no .mjs chunks under staticwebassets/');
    if (!hasBuildProps) console.error(`     missing build/${id}.props`);
    failures++;
  }
}

if (failures > 0) {
  console.error(`\n${failures} module package(s) missing static web assets.`);
  process.exit(1);
}

console.log(`\nVerified ${uiModules.length} module package(s).`);
