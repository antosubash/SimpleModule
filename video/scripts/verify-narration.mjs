#!/usr/bin/env node
// Verify each rendered per-module .mp4 actually carries its own module's
// narration. Builds a speech envelope for every narration source MP3, a
// narration-band envelope for every video, and checks that each video's
// shape matches its expected source better than any other module's.

import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readdirSync, readFileSync, rmSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { tmpdir } from 'node:os';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const videoRoot = resolve(__dirname, '..');
const compositor = resolve(
  videoRoot,
  'node_modules',
  '@remotion',
  'compositor-darwin-arm64',
);
const ffmpeg = resolve(compositor, 'ffmpeg');

// Narration in each Module-<Name>.mp4 starts at frame 18 (0.6s) per
// ModuleShowcase's <Sequence from={narrationFrom}>.
const NARRATION_OFFSET_S = 18 / 30;
const WINDOW_S = 0.1; // 100 ms RMS window
const SAMPLE_RATE = 16000;
const WINDOW_SAMPLES = Math.round(SAMPLE_RATE * WINDOW_S);

const workDir = resolve(tmpdir(), `simplemodule-verify-${process.pid}`);
mkdirSync(workDir, { recursive: true });
process.on('exit', () => rmSync(workDir, { recursive: true, force: true }));

function decodeToPcm(mp4OrMp3, startS = 0, durS = 8) {
  const wav = resolve(workDir, `dec-${Math.random().toString(36).slice(2)}.wav`);
  execFileSync(
    ffmpeg,
    [
      '-y',
      '-v', 'error',
      '-ss', String(startS),
      '-t', String(durS),
      '-i', mp4OrMp3,
      '-vn',
      '-acodec', 'pcm_s16le',
      '-ar', String(SAMPLE_RATE),
      '-ac', '1',
      wav,
    ],
    {
      env: { ...process.env, DYLD_LIBRARY_PATH: compositor },
      stdio: ['ignore', 'ignore', 'inherit'],
    },
  );
  const buf = readFileSync(wav);
  rmSync(wav);
  // Strip 44-byte WAV header; everything after is little-endian int16 PCM.
  return new Int16Array(buf.buffer, buf.byteOffset + 44, (buf.byteLength - 44) / 2);
}

function envelope(samples) {
  const out = new Float32Array(Math.floor(samples.length / WINDOW_SAMPLES));
  for (let w = 0; w < out.length; w++) {
    let sum = 0;
    for (let i = 0; i < WINDOW_SAMPLES; i++) {
      const s = samples[w * WINDOW_SAMPLES + i] / 32768;
      sum += s * s;
    }
    out[w] = Math.sqrt(sum / WINDOW_SAMPLES);
  }
  return out;
}

function correlate(a, b) {
  const n = Math.min(a.length, b.length);
  let meanA = 0;
  let meanB = 0;
  for (let i = 0; i < n; i++) {
    meanA += a[i];
    meanB += b[i];
  }
  meanA /= n;
  meanB /= n;
  let num = 0;
  let denA = 0;
  let denB = 0;
  for (let i = 0; i < n; i++) {
    const da = a[i] - meanA;
    const db = b[i] - meanB;
    num += da * db;
    denA += da * da;
    denB += db * db;
  }
  const den = Math.sqrt(denA * denB);
  return den === 0 ? 0 : num / den;
}

const narrationDir = resolve(videoRoot, 'public', 'narration', 'modules');
const modulesOutDir = resolve(videoRoot, 'out', 'modules');
const ids = readdirSync(narrationDir)
  .filter((f) => f.endsWith('.mp3'))
  .map((f) => f.replace(/\.mp3$/, ''));

const sources = {};
for (const id of ids) {
  const src = resolve(narrationDir, `${id}.mp3`);
  sources[id] = envelope(decodeToPcm(src, 0, 8));
}

let ok = 0;
let fail = 0;
for (const id of ids) {
  const vid = resolve(modulesOutDir, `${id}.mp4`);
  if (!existsSync(vid)) {
    console.log(`skip ${id}: ${vid} missing`);
    continue;
  }
  const vidEnv = envelope(decodeToPcm(vid, NARRATION_OFFSET_S, 8));

  // Score each source against this video's narration-band envelope.
  let bestId = null;
  let bestCorr = -Infinity;
  const scores = {};
  for (const sid of ids) {
    const c = correlate(vidEnv, sources[sid]);
    scores[sid] = c;
    if (c > bestCorr) {
      bestCorr = c;
      bestId = sid;
    }
  }
  const ownCorr = scores[id];
  const status = bestId === id ? 'ok' : 'MISMATCH';
  if (bestId === id) ok++;
  else fail++;
  console.log(
    `${status.padEnd(9)} ${id.padEnd(16)} own=${ownCorr.toFixed(3)}  best=${bestId}(${bestCorr.toFixed(3)})`,
  );
}
console.log(`\n${ok} ok, ${fail} mismatch`);
process.exit(fail === 0 ? 0 : 1);
