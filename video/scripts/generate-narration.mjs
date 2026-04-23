#!/usr/bin/env node
// Generate scene narration with Piper (a local neural TTS) for a more natural
// voice than macOS `say`. Runs serially and writes per-scene MP3s plus a
// manifest so the Remotion composition can line them up by frame offset.

import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const videoRoot = resolve(__dirname, '..');
const publicDir = resolve(videoRoot, 'public');
const narrationDir = resolve(publicDir, 'narration');
const moduleNarrationDir = resolve(narrationDir, 'modules');
const tmpDir = resolve(videoRoot, '.tmp-narration');

const voiceModel = resolve(videoRoot, '.piper-voices', 'en_US-ryan-high.onnx');
if (!existsSync(voiceModel)) {
  console.error(`Missing Piper voice model: ${voiceModel}`);
  console.error('Download from https://huggingface.co/rhasspy/piper-voices (see README).');
  process.exit(1);
}

rmSync(narrationDir, { recursive: true, force: true });
mkdirSync(narrationDir, { recursive: true });
mkdirSync(moduleNarrationDir, { recursive: true });
rmSync(tmpDir, { recursive: true, force: true });
mkdirSync(tmpDir, { recursive: true });

const compositor = resolve(
  videoRoot,
  'node_modules',
  '@remotion',
  'compositor-darwin-arm64',
);
const ffmpeg = resolve(compositor, 'ffmpeg');
const ffprobe = resolve(compositor, 'ffprobe');

// `length_scale` > 1 slows speech, < 1 speeds it up. 0.95 is a touch quicker
// than default and keeps each line inside its scene budget.
const LENGTH_SCALE = '0.95';
const SENTENCE_SILENCE = '0.25';

// Copy polished for a more human cadence: contractions, shorter sentences,
// fewer acronym-as-word hacks. Piper handles ".NET" and "IEndpoint" better
// than `say` did, so we can drop the "dot NET" / "I Endpoint" spellings.
const lines = [
  { id: 's1', text: 'SimpleModule — the modular monolith for .NET.' },
  { id: 's2', text: "Monoliths tangle. Microservices complicate. There's a better way." },
  { id: 's3', text: 'Mark a class with Module, and the Roslyn generator wires it up at compile time.' },
  { id: 's4', text: 'Write one IEndpoint class. Routes and permissions register themselves.' },
  { id: 's5', text: 'MapCrud turns a single line into five REST endpoints.' },
  { id: 's6', text: 'Render React pages with Inertia, and broadcast events across modules.' },
  { id: 's7', text: 'Ten production-ready core modules ship in the box.' },
  { id: 's8', text: 'Built on .NET 10 and React 19, with fifty-five compile-time safety checks.' },
  { id: 's9', text: "Start building today. You'll find us on GitHub." },
];

function run(cmd, args, env = {}) {
  return execFileSync(cmd, args, {
    env: { ...process.env, ...env },
    stdio: ['pipe', 'pipe', 'inherit'],
  }).toString();
}

function speak(text, wavPath) {
  execFileSync(
    'piper',
    [
      '-m', voiceModel,
      '-f', wavPath,
      '--length-scale', LENGTH_SCALE,
      '--sentence-silence', SENTENCE_SILENCE,
    ],
    {
      input: text,
      stdio: ['pipe', 'inherit', 'pipe'],
    },
  );
}

function wavToMp3(wavPath, mp3Path) {
  run(
    ffmpeg,
    [
      '-y',
      '-i', wavPath,
      '-acodec', 'libmp3lame',
      '-b:a', '160k',
      '-ar', '44100',
      '-ac', '1',
      mp3Path,
    ],
    { DYLD_LIBRARY_PATH: compositor },
  );
}

function probeDuration(mp3Path) {
  const out = run(
    ffprobe,
    [
      '-v', 'error',
      '-show_entries', 'format=duration',
      '-of', 'default=noprint_wrappers=1:nokey=1',
      mp3Path,
    ],
    { DYLD_LIBRARY_PATH: compositor },
  );
  return parseFloat(out.trim());
}

function generate(id, text, outDir) {
  const wav = resolve(tmpDir, `${id}.wav`);
  const mp3 = resolve(outDir, `${id}.mp3`);
  speak(text, wav);
  wavToMp3(wav, mp3);
  return probeDuration(mp3);
}

// --- Main showcase narration ---------------------------------------------
const manifest = [];
for (const line of lines) {
  const durationS = generate(line.id, line.text, narrationDir);
  manifest.push({ id: line.id, durationS, text: line.text });
  console.log(`[main] ${line.id}: ${durationS.toFixed(2)}s — ${line.text}`);
}
writeFileSync(
  resolve(narrationDir, 'manifest.json'),
  JSON.stringify(manifest, null, 2) + '\n',
);

// --- Per-module showcase narration ---------------------------------------
// Extract narration strings from src/data/moduleShowcases.ts so we can stay
// in lockstep with the composition data without a transpile step.
const moduleSource = readFileSync(
  resolve(videoRoot, 'src', 'data', 'moduleShowcases.ts'),
  'utf8',
);
const moduleEntries = [...moduleSource.matchAll(
  /id:\s*'([^']+)',[\s\S]*?narration:\s*\n?\s*'([^']+)'/g,
)].map(([, id, narration]) => ({ id, narration }));

if (moduleEntries.length === 0) {
  throw new Error('Failed to parse moduleShowcases.ts — narration regex missed.');
}

const moduleManifest = [];
for (const mod of moduleEntries) {
  const durationS = generate(mod.id, mod.narration, moduleNarrationDir);
  moduleManifest.push({ id: mod.id, durationS, text: mod.narration });
  console.log(`[module:${mod.id}] ${durationS.toFixed(2)}s`);
}
writeFileSync(
  resolve(moduleNarrationDir, 'manifest.json'),
  JSON.stringify(moduleManifest, null, 2) + '\n',
);

rmSync(tmpDir, { recursive: true, force: true });
