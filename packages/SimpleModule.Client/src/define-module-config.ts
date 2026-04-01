import { basename, resolve } from 'node:path';
import react from '@vitejs/plugin-react';
import type { UserConfig } from 'vite';
import { defineConfig } from 'vite';
import { defaultVendors } from './vite-plugin-vendor.ts';

/**
 * Unified Vite config for SimpleModule modules.
 *
 * Derives everything from the module directory:
 * - Module name from the directory name (e.g. `Products/` → `Products`)
 * - Entry point at `Pages/index.ts`
 * - Output as `{Name}.pages.js` into `wwwroot/`
 * - Externals from `defaultVendors`
 *
 * For non-standard overrides, use Vite's `mergeConfig`:
 * ```ts
 * import { mergeConfig } from 'vite';
 * export default mergeConfig(defineModuleConfig(import.meta.dirname), { ... });
 * ```
 */
export function defineModuleConfig(dir: string): UserConfig {
  const name = basename(dir);
  const isDev = process.env.VITE_MODE !== 'prod';

  const externalPkgs = defaultVendors.map((v) => v.pkg);

  // Alias CJS-only packages that use `require('react')` to ESM shims.
  // Rolldown (Vite 8) can't convert CJS require() calls for external
  // packages to ESM imports, which causes runtime errors in the browser.
  const shimDir = resolve(import.meta.dirname, 'shims');

  return defineConfig({
    plugins: [react()],
    resolve: {
      alias: [
        {
          find: /^use-sync-external-store\/shim\/with-selector(\.js)?$/,
          replacement: resolve(shimDir, 'use-sync-external-store-with-selector.ts'),
        },
        {
          find: /^use-sync-external-store\/with-selector(\.js)?$/,
          replacement: resolve(shimDir, 'use-sync-external-store-with-selector.ts'),
        },
        {
          find: /^use-sync-external-store(\/shim)?(\/index(\.js)?)?$/,
          replacement: resolve(shimDir, 'use-sync-external-store-shim.ts'),
        },
      ],
    },
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
      rolldownOptions: {
        external: externalPkgs,
        output: {
          assetFileNames: `${name.toLowerCase()}[extname]`,
        },
      },
    },
  });
}
