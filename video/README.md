# SimpleModule Showcase Video

A ~50 second Remotion video that showcases the SimpleModule framework:
its compile-time module discovery, `IEndpoint` auto-registration,
`CrudEndpoints` helper, Inertia.js bridge, and cross-module event bus.

## Prerequisites

- Node.js 20+
- Linux/macOS/Windows with the system libs Chromium Headless Shell needs
  (on Ubuntu/Debian these are: `libnss3 libgbm1 libasound2t64
  libatk-bridge2.0-0t64 libatk1.0-0t64 libdrm2 libxkbcommon0 libxcomposite1
  libxdamage1 libxfixes3 libxrandr2`). The `@remotion/renderer` package
  bundles its own ffmpeg, so the system `ffmpeg` binary is not required.

## Install

```bash
cd video
npm install
```

## Add audio (optional)

Audio is **opt-in**. Drop a track at `public/background.mp3` and set the
`REMOTION_ENABLE_AUDIO=1` environment variable when rendering.

Suggested CC0 tracks (verify the license on the linked page before
shipping):

- Pixabay — "Corporate Upbeat" <https://pixabay.com/music/corporate-corporate-168379/>
- Bensound — "Inspire" <https://www.bensound.com/royalty-free-music/track/inspire>
- Uppbeat — filter by "CC0 / No attribution" <https://uppbeat.io/>

```bash
# Save your chosen track as public/background.mp3, then render with audio:
REMOTION_ENABLE_AUDIO=1 npm run render
```

The `<Audio>` component fades in during the first second and fades out in
the last two, so any upbeat 50-second track will work.

## Develop

```bash
npm run start
# opens the Remotion studio at http://localhost:3000
# pick the "SimpleModule" composition
```

## Render

First render downloads the Chrome Headless Shell (~170 MB) to
`~/.cache/remotion/`. On hosts running as root you must disable the
Chromium sandbox — the scripts below already set the flag:

```bash
# pre-warm the Chrome download (recommended)
npx remotion browser ensure

# render (no audio)
REMOTION_CHROME_FLAGS="--no-sandbox --disable-setuid-sandbox" npm run render

# render with audio (after adding public/background.mp3)
REMOTION_ENABLE_AUDIO=1 \
  REMOTION_CHROME_FLAGS="--no-sandbox --disable-setuid-sandbox" \
  npm run render

# output: out/simplemodule.mp4
```

If your host sits behind an SSL-intercepting proxy (e.g. some corporate
networks), Google Fonts may fail to load. The `remotion.config.ts` in
this project already sets `setChromiumIgnoreCertificateErrors(true)` so
the render still succeeds — fonts simply fall back to the system stack.

## Structure

```
video/
├── public/                  # logos + optional background.mp3
└── src/
    ├── Root.tsx             # <Composition> definition (1500 frames, 30fps, 1920x1080)
    ├── Video.tsx            # top-level <Series> sequencer + <Audio>
    ├── scenes/              # one file per storyboard scene
    ├── components/          # Logo, CodeBlock, SplitCodePair, ModuleChip, ...
    └── data/                # module list, tech stack, code snippets
```

## Storyboard

| # | Scene | Duration |
|---|---|---|
| 1 | Opening logo + tagline | 4s |
| 2 | Problem → Solution | 5s |
| 3 | `[Module]` attribute + Roslyn generator | 6s |
| 4 | `IEndpoint` auto-discovery | 6s |
| 5 | `CrudEndpoints` helper | 7s |
| 6 | Inertia.Render + IEventBus | 6s |
| 7 | 20 module inventory grid | 6s |
| 8 | Tech stack + stats | 6s |
| 9 | Call to action | 4s |
