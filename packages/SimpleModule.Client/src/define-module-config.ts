import { readdirSync, readFileSync } from 'node:fs';
import { basename, resolve } from 'node:path';
import react from '@vitejs/plugin-react';
import type { UserConfig } from 'vite';
import { defineConfig } from 'vite';
import { defaultVendors } from './vite-plugin-vendor.ts';

/**
 * Detect the case-insensitive filesystem trap where `Pages/index.ts` (the
 * framework-required barrel) sits next to a page like `Pages/Index.tsx` and
 * the barrel uses an extension-less dynamic import (`() => import('./Index')`).
 *
 * On macOS/Windows, Rolldown resolves `./Index` back to `./index.ts` (the
 * barrel itself), producing a self-referential chunk that throws at runtime
 * with `Cannot assign to property 'layout' of [object Module]`.
 *
 * Throw early with a clear, actionable error.
 */
function assertNoBarrelCollision(dir: string): void {
  const pagesDir = resolve(dir, 'Pages');

  let entries: string[];
  try {
    entries = readdirSync(pagesDir);
  } catch {
    return;
  }
  if (!entries.includes('index.ts')) return;

  const offending = entries.find((f) => /^index\.(tsx|jsx|js)$/i.test(f) && f !== 'index.ts');
  if (!offending) return;

  let barrel: string;
  try {
    barrel = readFileSync(resolve(pagesDir, 'index.ts'), 'utf8');
  } catch {
    return;
  }

  // Match extension-less dynamic imports whose specifier case-insensitively
  // resolves to the barrel itself (`import('./Index')` next to `index.ts`).
  // On case-insensitive filesystems Rolldown silently picks `./index.ts`
  // (the barrel) rather than the sibling `./Index.tsx`, producing a chunk
  // that re-exports the barrel and crashes at runtime.
  if (!/import\(\s*['"]\.\/index['"]\s*\)/i.test(barrel)) return;

  const specifier = offending.replace(/\.(tsx|jsx|js)$/i, '');
  throw new Error(
    `[@simplemodule/client] Pages/index.ts contains \`import('./${specifier}')\` which collides ` +
      `with the barrel on case-insensitive filesystems (macOS/Windows). Rolldown will silently ` +
      `emit a self-referential chunk and the page will fail at runtime with ` +
      `"Cannot assign to property 'layout' of [object Module]".\n\n` +
      `Fix: use the explicit file extension, e.g. \`import('./${offending}')\`, or rename ` +
      `Pages/${offending} to a name that does not case-insensitively match 'index'.`,
  );
}

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
  assertNoBarrelCollision(dir);

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
          find: '@',
          replacement: dir,
        },
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
