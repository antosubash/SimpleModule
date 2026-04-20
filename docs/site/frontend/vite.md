---
outline: deep
---

# Vite Configuration

SimpleModule uses **Vite 6** in library mode to build each module's frontend as a standalone ES module. This page covers the build configuration, the development workflow, and the orchestrators that coordinate everything.

## Library Mode for Modules

Each module builds its React pages into a single `{ModuleName}.pages.js` file using Vite's [library mode](https://vite.dev/guide/build.html#library-mode). This means:

- The output is a reusable ES module, not a full application bundle
- Shared dependencies (React, Inertia) are externalized, not bundled into each module
- The host application loads module bundles dynamically at runtime

### Why Library Mode?

In a modular monolith, each module is independently deployable. If every module bundled its own copy of React, you would ship React N times. Library mode externalizes shared dependencies so they are loaded once from vendored copies.

## Module `vite.config.ts`

Every module uses the `defineModuleConfig` helper from `@simplemodule/client/module`:

```ts
// modules/Products/src/Products/vite.config.ts
import { defineModuleConfig } from '@simplemodule/client/module';

export default defineModuleConfig(__dirname);
```

This single line generates a complete Vite configuration. The helper derives everything from the module directory:

| Setting | Value | Source |
|---|---|---|
| Entry point | `Pages/index.ts` | Convention |
| Output file | `{Name}.pages.js` | Directory name |
| Output directory | `wwwroot/` | Convention |
| Format | ES module | Fixed |
| Externals | React, React-DOM, Inertia | `defaultVendors` |
| Source maps | Enabled in dev, disabled in prod | `VITE_MODE` env var |
| Minification | Disabled in dev, esbuild in prod | `VITE_MODE` env var |

### Under the Hood

The `defineModuleConfig` function generates this Vite config:

```ts
function defineModuleConfig(dir: string): UserConfig {
  const name = basename(dir);
  const isDev = process.env.VITE_MODE !== 'prod';

  return defineConfig({
    plugins: [react()],
    define: {
      'process.env.NODE_ENV': JSON.stringify(isDev ? 'development' : 'production'),
    },
    build: {
      lib: {
        entry: resolve(dir, 'Pages/index.ts'),
        formats: ['es'],
        fileName: () => `${name}.pages.js`,
      },
      sourcemap: isDev,
      minify: isDev ? false : 'esbuild',
      outDir: 'wwwroot',
      emptyOutDir: false,
      rollupOptions: {
        external: defaultVendors.map((v) => v.pkg),
        output: {
          assetFileNames: `${name.toLowerCase()}[extname]`,
        },
      },
    },
  });
}
```

### Custom Overrides

For non-standard requirements, use Vite's `mergeConfig`:

```ts
import { mergeConfig } from 'vite';
import { defineModuleConfig } from '@simplemodule/client/module';

export default mergeConfig(defineModuleConfig(__dirname), {
  // Custom overrides here
});
```

## Externalization

Shared dependencies are externalized so they are not bundled into each module. The `defaultVendors` list defines what gets externalized:

```ts
export const defaultVendors: VendorEntry[] = [
  { pkg: 'react', file: 'react', externals: [] },
  { pkg: 'react-dom', file: 'react-dom', externals: ['react'] },
  { pkg: 'react/jsx-runtime', file: 'react-jsx-runtime', externals: ['react'] },
  { pkg: 'react-dom/client', file: 'react-dom-client', externals: ['react', 'react-dom'] },
  {
    pkg: '@inertiajs/react',
    file: 'inertiajs-react',
    externals: ['react', 'react-dom', 'react/jsx-runtime', 'react-dom/client'],
  },
];
```

The `vendorBuildPlugin` builds these shared libraries into standalone ESM files under `wwwroot/js/vendor/`. Each module's `import` statements for React or Inertia are rewritten to import maps or resolved paths pointing at these vendored copies.

## Module `package.json`

Each module declares React and React-DOM as **peer dependencies** since they are provided by the host:

```json
{
  "private": true,
  "name": "@simplemodule/products",
  "version": "0.0.0",
  "scripts": {
    "build": "cross-env VITE_MODE=prod vite build",
    "build:dev": "cross-env VITE_MODE=dev vite build",
    "watch": "cross-env VITE_MODE=dev vite build --watch"
  },
  "peerDependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  }
}
```

Three scripts are standard:
- **`build`** -- Production build (minified, no source maps)
- **`build:dev`** -- Development build (unminified, with source maps)
- **`watch`** -- Development build with file watching

## Development Workflow

### `npm run dev`

The `npm run dev` command starts the complete development environment using the **dev orchestrator** (`scripts/dev-orchestrator.mjs`). It launches three types of processes in parallel:

1. **`dotnet run`** -- The ASP.NET backend on `https://localhost:5001`
2. **Module watches** -- `vite build --watch` for every module with a `vite.config.ts`
3. **ClientApp watch** -- `vite build --watch` for the host ClientApp

```
npm run dev
  |
  ├── dotnet run --project template/SimpleModule.Host
  ├── npm run watch  (in modules/Products/src/Products/)
  ├── npm run watch  (in modules/Orders/src/Orders/)
  ├── npm run watch  (in modules/Users/src/Users/)
  └── npm run watch  (in template/SimpleModule.Host/ClientApp/)
```

The orchestrator auto-discovers all modules that have a Vite config, so adding a new module automatically includes it in the dev workflow.

::: tip Development experience
- **Edit a module file** -- Vite rebuilds that module in milliseconds (unminified, readable output)
- **Edit ClientApp** -- Vite rebuilds the host app
- **Refresh browser** -- See changes immediately
- **Browser dev tools** -- Source maps let you debug original TypeScript
- **Ctrl+C** -- Gracefully stops all processes (dotnet, all watchers)
:::

### Error Handling

The orchestrator treats `dotnet run` as critical -- if the backend crashes, all processes shut down. Module watch failures are non-fatal; other modules and the backend continue running.

## Build Modes

### Development Build

```bash
npm run dev:build
# or
npm run build:dev
```

- Unminified output (readable JavaScript)
- Source maps enabled
- `process.env.NODE_ENV` set to `'development'`
- Controlled by `VITE_MODE=dev`

### Production Build

```bash
npm run build
```

- Minified with esbuild
- No source maps
- `process.env.NODE_ENV` set to `'production'`
- Controlled by `VITE_MODE=prod`

## Build Orchestrator

The production build uses the **build orchestrator** (`scripts/build-orchestrator.mjs`) which:

1. Discovers all buildable workspaces (modules + ClientApp)
2. Builds all workspaces **in parallel** for performance
3. Passes the `VITE_MODE` environment variable to each build
4. Reports success or failure for each workspace
5. Exits with code 1 if any build fails

```bash
# Build all modules in production mode
npm run build

# Build all modules in development mode
npm run dev:build
```

## Build Output

After building, each module's `wwwroot/` directory contains:

```
modules/Products/src/Products/wwwroot/
  Products.pages.js        # The module's page bundle
  products.css             # Any CSS assets (named from module)
```

These files are served as static content via ASP.NET's `_content/{ModuleName}/` path, which is how `resolvePage` finds them at `/_content/Products/Products.pages.js`.

## Next Steps

- [Testing Overview](/testing/overview) -- test strategies for your modules
- [CLI Overview](/cli/overview) -- scaffold modules and features with the `sm` command
- [Deployment](/advanced/deployment) -- production builds and Docker configuration
