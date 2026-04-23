#!/usr/bin/env node
// Post-process rendered MP4s so their audio hits -14 LUFS (streaming
// standard). Remotion just mixes sources verbatim, so a hot TTS track can
// push the integrated loudness well over -10 LUFS without this step.

import { execFileSync } from 'node:child_process';
import { readdirSync, renameSync, rmSync, statSync } from 'node:fs';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const videoRoot = resolve(__dirname, '..');
const outDir = resolve(videoRoot, 'out');
const compositor = resolve(
  videoRoot,
  'node_modules',
  '@remotion',
  'compositor-darwin-arm64',
);
const ffmpeg = resolve(compositor, 'ffmpeg');

function findMp4s(dir, acc = []) {
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) {
      findMp4s(full, acc);
    } else if (entry.endsWith('.mp4')) {
      acc.push(full);
    }
  }
  return acc;
}

function normalize(mp4Path) {
  const tmp = mp4Path.replace(/\.mp4$/, '.norm.mp4');
  // Copy video, re-encode audio through loudnorm. Target -14 LUFS with
  // -1.5 dBTP ceiling, matches Spotify/YouTube delivery specs.
  execFileSync(
    ffmpeg,
    [
      '-y',
      '-i', mp4Path,
      '-c:v', 'copy',
      '-af', 'loudnorm=I=-14:TP=-1.5:LRA=11',
      '-c:a', 'aac',
      '-b:a', '192k',
      '-ar', '48000',
      '-movflags', '+faststart',
      tmp,
    ],
    {
      env: { ...process.env, DYLD_LIBRARY_PATH: compositor },
      stdio: ['ignore', 'ignore', 'inherit'],
    },
  );
  rmSync(mp4Path);
  renameSync(tmp, mp4Path);
}

const targets = findMp4s(outDir);
if (targets.length === 0) {
  console.error(`No mp4s found under ${outDir}. Run "npm run render:all" first.`);
  process.exit(1);
}

for (const t of targets) {
  process.stdout.write(`normalizing ${t.replace(outDir + '/', '')} … `);
  normalize(t);
  console.log('ok');
}
console.log(`\nnormalized ${targets.length} videos to -14 LUFS`);
