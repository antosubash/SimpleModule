#!/usr/bin/env node
// Synthesize a gentle ambient pad as a WAV file.
// Layered sine chord (Cmaj9 voicing) + slow LFO tremolo + simple 1-pole lowpass.

import { writeFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const SAMPLE_RATE = 44100;
const DURATION_S = 42;
const CHANNELS = 2;
const TOTAL_SAMPLES = SAMPLE_RATE * DURATION_S;

// Cmaj9 voicing (C3, E3, G3, C4, D4, E4) with amplitudes tuned so the low
// partials dominate and the upper ones only add sparkle.
const partials = [
  { freq: 130.81, amp: 0.55, pan: 0.0 }, // C3
  { freq: 164.81, amp: 0.32, pan: -0.3 }, // E3
  { freq: 196.0, amp: 0.38, pan: 0.3 }, // G3
  { freq: 261.63, amp: 0.22, pan: -0.15 }, // C4
  { freq: 293.66, amp: 0.12, pan: 0.15 }, // D4
  { freq: 329.63, amp: 0.14, pan: 0.0 }, // E4
];

// Tremolo LFO on overall amplitude — slow and shallow.
const tremoloHz = 0.28;
const tremoloDepth = 0.18;

// Tiny 1-pole lowpass to soften the highs.
const cutoffHz = 1400;
const rc = 1 / (2 * Math.PI * cutoffHz);
const dt = 1 / SAMPLE_RATE;
const alpha = dt / (rc + dt);

// Fade envelopes at the edges so the loop point is clean.
const fadeInSamples = Math.floor(SAMPLE_RATE * 1.5);
const fadeOutSamples = Math.floor(SAMPLE_RATE * 2.5);

function envelope(i) {
  if (i < fadeInSamples) return i / fadeInSamples;
  if (i > TOTAL_SAMPLES - fadeOutSamples) {
    return (TOTAL_SAMPLES - i) / fadeOutSamples;
  }
  return 1;
}

function pan(amp, p) {
  // equal-power pan: p in [-1,1] → (left, right) gains
  const angle = ((p + 1) / 2) * (Math.PI / 2);
  return { l: amp * Math.cos(angle), r: amp * Math.sin(angle) };
}

// Pre-compute per-partial pan gains.
const partialGains = partials.map((p) => ({ ...p, gains: pan(1, p.pan) }));

let lastL = 0;
let lastR = 0;

// PCM16 stereo interleaved
const pcm = new Int16Array(TOTAL_SAMPLES * CHANNELS);

for (let i = 0; i < TOTAL_SAMPLES; i++) {
  const t = i / SAMPLE_RATE;
  const env = envelope(i);
  const trem = 1 - tremoloDepth + tremoloDepth * Math.sin(2 * Math.PI * tremoloHz * t);

  let l = 0;
  let r = 0;
  for (const p of partialGains) {
    const s = Math.sin(2 * Math.PI * p.freq * t) * p.amp;
    l += s * p.gains.l;
    r += s * p.gains.r;
  }

  // apply tremolo + envelope
  l *= trem * env;
  r *= trem * env;

  // 1-pole lowpass per-channel
  lastL = lastL + alpha * (l - lastL);
  lastR = lastR + alpha * (r - lastR);

  // master gain to keep headroom
  const master = 0.85;
  let outL = lastL * master;
  let outR = lastR * master;

  // clamp
  if (outL > 1) outL = 1;
  else if (outL < -1) outL = -1;
  if (outR > 1) outR = 1;
  else if (outR < -1) outR = -1;

  pcm[i * 2] = Math.round(outL * 32767);
  pcm[i * 2 + 1] = Math.round(outR * 32767);
}

// Build a WAV container
const byteRate = SAMPLE_RATE * CHANNELS * 2;
const blockAlign = CHANNELS * 2;
const dataSize = pcm.byteLength;
const header = Buffer.alloc(44);
header.write('RIFF', 0);
header.writeUInt32LE(36 + dataSize, 4);
header.write('WAVE', 8);
header.write('fmt ', 12);
header.writeUInt32LE(16, 16); // PCM chunk size
header.writeUInt16LE(1, 20); // PCM format
header.writeUInt16LE(CHANNELS, 22);
header.writeUInt32LE(SAMPLE_RATE, 24);
header.writeUInt32LE(byteRate, 28);
header.writeUInt16LE(blockAlign, 32);
header.writeUInt16LE(16, 34); // bits per sample
header.write('data', 36);
header.writeUInt32LE(dataSize, 40);

const wavPath = resolve(__dirname, '..', 'public', 'background.wav');
writeFileSync(wavPath, Buffer.concat([header, Buffer.from(pcm.buffer)]));
console.log('wrote', wavPath, dataSize, 'bytes of PCM');
