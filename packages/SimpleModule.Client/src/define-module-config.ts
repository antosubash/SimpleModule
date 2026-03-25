import { resolve, basename } from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig, type UserConfig } from 'vite';
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
 * export default mergeConfig(defineModuleConfig(__dirname), { ... });
 * ```
 */
export function defineModuleConfig(dir: string): UserConfig {
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
