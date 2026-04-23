#!/usr/bin/env node
// Render the main SimpleModule showcase + one video per core module.
// Output files land in out/simplemodule.mp4 and out/modules/<Name>.mp4.

import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const videoRoot = resolve(__dirname, '..');
const outDir = resolve(videoRoot, 'out');
const modulesOutDir = resolve(outDir, 'modules');
mkdirSync(modulesOutDir, { recursive: true });

const moduleSource = readFileSync(
  resolve(videoRoot, 'src', 'data', 'moduleShowcases.ts'),
  'utf8',
);
const moduleIds = [...moduleSource.matchAll(/id:\s*'([^']+)'/g)].map((m) => m[1]);

const targets = [
  { id: 'SimpleModule', out: resolve(outDir, 'simplemodule.mp4') },
  ...moduleIds.map((id) => ({
    id: `Module-${id}`,
    out: resolve(modulesOutDir, `${id}.mp4`),
  })),
];

// Fail fast if any referenced narration clip is missing — otherwise Remotion
// silently renders video with no voice-over for that clip.
const narrationDir = resolve(videoRoot, 'public', 'narration');
const missing = [];
const mainClips = ['s1', 's2', 's3', 's4', 's5', 's6', 's7', 's8', 's9'];
for (const c of mainClips) {
  const p = resolve(narrationDir, `${c}.mp3`);
  if (!existsSync(p)) missing.push(p);
}
for (const id of moduleIds) {
  const p = resolve(narrationDir, 'modules', `${id}.mp3`);
  if (!existsSync(p)) missing.push(p);
}
if (missing.length) {
  console.error('Missing narration files — run `npm run narrate` first:');
  for (const m of missing) console.error('  ' + m);
  process.exit(1);
}

const remotion = resolve(videoRoot, 'node_modules', '.bin', 'remotion');

for (const t of targets) {
  console.log(`\n>>> rendering ${t.id} -> ${t.out}`);
  execFileSync(remotion, ['render', t.id, t.out, '--codec=h264', '--log=info'], {
    cwd: videoRoot,
    stdio: 'inherit',
  });
}

console.log(`\n[done] ${targets.length} videos written under ${outDir}`);

// Normalize every video to -14 LUFS so narration + music don't overshoot.
console.log('\n>>> normalizing loudness');
execFileSync('node', [resolve(__dirname, 'normalize-loudness.mjs')], {
  cwd: videoRoot,
  stdio: 'inherit',
});

// Prove each per-module video carries its own narration, not a neighbour's.
console.log('\n>>> verifying narration-to-module mapping');
execFileSync('node', [resolve(__dirname, 'verify-narration.mjs')], {
  cwd: videoRoot,
  stdio: 'inherit',
});
