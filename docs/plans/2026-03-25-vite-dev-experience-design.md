# Vite Build Process & JS Bundling DX Improvement Design

**Date:** 2026-03-25
**Goal:** Improve JavaScript bundling developer experience by removing minification in dev, adding source maps, and creating unified development workflow.

## Current Problem

- **Minification during dev:** All module vite configs hardcode `NODE_ENV='production'`, forcing minification even in development
- **Poor debugging:** Minified code in browser dev tools makes debugging difficult
- **Manual coordination:** Developers must manually start `dotnet run` + multiple watch commands in separate terminals
- **Slow feedback loop:** No automation for starting/stopping all processes together

## Desired Outcomes

1. **Fast, unminified dev builds** — readable code for debugging
2. **Automatic source maps** — browser dev tools show original TypeScript
3. **Single command startup** — `npm run dev` starts everything
4. **Unified process management** — coordinated dotnet + all npm watches
5. **Production builds still optimized** — `npm run build` creates minified output
6. **NuGet package support** — works with both local modules and consumed NuGet packages

## Solution Overview: Optimized Build-Watch Pipeline

### Approach Selection

**Why this approach over others:**
- **Simpler than Vite dev servers** — no proxy complexity or Inertia middleware changes
- **Better than current setup** — addresses all DX pain points (minification, source maps, coordination)
- **Pragmatic** — leverages existing build pipeline, minimal infrastructure
- **Scales well** — works for both local development and NuGet distribution

### Architecture

#### 1. Vite Configuration (Conditional NODE_ENV)

**Per-module vite configs** (`modules/*/src/*/vite.config.ts`):

```typescript
const isDev = process.env.VITE_MODE !== 'prod';

export default defineConfig({
  plugins: [react()],
  build: {
    lib: {
      entry: resolve(__dirname, 'Pages/index.ts'),
      formats: ['es'],
      fileName: () => '{ModuleName}.pages.js',
    },
    sourcemap: isDev,           // Generate .js.map files in dev
    minify: isDev ? false : 'esbuild', // No minification in dev
    outDir: 'wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      external: ['react', 'react-dom', 'react/jsx-runtime', '@inertiajs/react'],
    },
  },
});
```

**Environment variable strategy:**
- `VITE_MODE=dev` — unminified, source maps enabled
- `VITE_MODE=prod` or unset — minified, optimized for production
- Default (no env var) is production to prevent accidental shipping of unminified code

**Root ClientApp vite config** (`template/SimpleModule.Host/ClientApp/vite.config.ts`):
- Same pattern: conditional source maps and minification

#### 2. NPM Scripts

**Per-module** (`modules/*/src/*/package.json`):
```json
{
  "scripts": {
    "build": "vite build",
    "build:dev": "VITE_MODE=dev vite build",
    "watch": "VITE_MODE=dev vite build --watch"
  }
}
```

**Root** (`package.json`):
```json
{
  "scripts": {
    "dev": "node tools/dev-orchestrator.mjs",
    "dev:build": "npm run build:dev -w modules/*/src/* && npm run build:dev -w template/SimpleModule.Host/ClientApp",
    "build": "VITE_MODE=prod npm run build"
  }
}
```

#### 3. Dev Orchestration Script

**File:** `tools/dev-orchestrator.mjs`

**Responsibilities:**
- Start `dotnet run` in the host project
- Start `npm run watch` in all module workspaces (`modules/*/src/*`)
- Start `npm run watch` in ClientApp (`template/SimpleModule.Host/ClientApp`)
- Aggregate console output from all processes with prefixes
- Handle graceful shutdown (Ctrl+C terminates all child processes)
- Log process status and startup information

**Usage:**
```bash
npm run dev
# Output:
# [dotnet]      Starting https://localhost:5001...
# [Products]    Watching for changes...
# [Users]       Watching for changes...
# [ClientApp]   Watching for changes...
#
# Edit a file → rebuild automatically (fast, unminified)
# Browser refresh to see changes
```

#### 4. Source Maps in Development

**When `VITE_MODE=dev`:**
- Vite generates `.js.map` files alongside bundles
- Browser dev tools automatically map minified code to source
- Stack traces point to original TypeScript locations
- No performance penalty (only used locally)

**When `VITE_MODE=prod`:**
- No source maps generated
- Minimal build size for distribution

#### 5. Developer Workflow

**Before (Current):**
```bash
dotnet run
# Separately in other terminals:
npm run watch -w modules/Products/src/Products
npm run watch -w modules/Users/src/Users
# Manual coordination, multiple windows
```

**After (New):**
```bash
npm run dev
# Single command, all processes coordinated
# Ctrl+C stops everything gracefully
```

**Iteration cycle:**
1. Make a JS/TS change in a module
2. Vite watch detects change, rebuilds (unminified)
3. Browser refresh shows changes (manual, but instant)
4. Debugging in dev tools uses source maps

#### 6. CI/CD Integration

**No changes required.** Existing CI/CD pipeline works unchanged:

```bash
# In CI:
npm run build              # Produces minified, optimized JS
dotnet build               # Includes the built JS files
dotnet test                # Tests run against optimized builds
```

**For NuGet publishing:**
```bash
npm run build              # Create optimized JS
dotnet pack                # Package includes wwwroot/ with built files
dotnet nuget push ...      # Publish to NuGet feed
```

Consumer projects using published modules:
- NuGet package includes pre-built JS in `wwwroot/`
- No Vite, no npm, no build step needed
- JS files served as-is

#### 7. Deployment Behavior

**Local monorepo development:**
- `npm run dev` → unminified, debuggable JS
- Fast rebuilds for quick feedback

**Production/Distribution:**
- `npm run build` → minified, optimized JS
- Shipped in NuGet packages or deployed to production

**NuGet consumers:**
- Receive pre-built JS files
- No build process needed
- Works alongside locally developed modules

## Benefits

| Aspect | Before | After |
|--------|--------|-------|
| **Dev minification** | Always minified | Never minified ✅ |
| **Source maps** | None | Automatic in dev ✅ |
| **Startup** | Multi-terminal | Single `npm run dev` ✅ |
| **Process coordination** | Manual | Automated ✅ |
| **Rebuild feedback** | Multiple logs to monitor | Unified output ✅ |
| **Production builds** | Same config | Separate, optimized ✅ |
| **Debug experience** | Difficult (minified) | Good (readable + maps) ✅ |
| **NuGet support** | Not designed for | Seamless ✅ |

## Implementation Steps

1. Create `tools/dev-orchestrator.mjs` orchestration script
2. Update all module vite configs (conditional NODE_ENV)
3. Update all module package.json scripts
4. Update root package.json with `dev`, `dev:build`, and modified `build` scripts
5. Update ClientApp vite config and package.json
6. Test the complete workflow locally
7. Update documentation (README, CLAUDE.md)
8. Update CI/CD pipelines if needed (should work unchanged)

## Testing & Validation

- [ ] `npm run dev` starts all processes without errors
- [ ] Editing a module file triggers rebuild
- [ ] Rebuilt JS is not minified (readable in dev tools)
- [ ] Browser refresh shows updated module
- [ ] Ctrl+C gracefully shuts down all processes
- [ ] Source maps load correctly in browser dev tools
- [ ] `npm run build` produces minified output
- [ ] `dotnet test` passes with production builds
- [ ] NuGet package contains pre-built JS files

## Future Enhancements (Out of Scope)

- Vite dev servers with hot reload (more complex, different approach)
- Automatic browser refresh on rebuild
- Custom file watching for C# changes
- Development mode configuration UI

## Notes

- The orchestration script uses standard Node.js `child_process` APIs
- Environment variable `VITE_MODE` is a single source of truth for build mode
- No breaking changes to existing CI/CD or build pipeline
- Works seamlessly with NuGet package distribution
