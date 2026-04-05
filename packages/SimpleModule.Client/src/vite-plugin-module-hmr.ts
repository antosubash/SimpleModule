import { existsSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Plugin, ViteDevServer } from 'vite';

interface ModuleEntry {
  /** Module name, e.g. "Products" */
  name: string;
  /** Assembly name, e.g. "SimpleModule.Products" */
  assemblyName: string;
  /** Absolute path to the module source, e.g. /repo/modules/Products/src/SimpleModule.Products */
  dir: string;
  /** Absolute path to Pages/index.ts */
  pagesEntry: string;
}

/**
 * Discovers all SimpleModule modules that have a Pages/index.ts entry.
 */
function discoverModules(repoRoot: string): ModuleEntry[] {
  const modulesDir = resolve(repoRoot, 'modules');
  const entries: ModuleEntry[] = [];

  if (!existsSync(modulesDir)) return entries;

  for (const group of readdirSync(modulesDir, { withFileTypes: true })) {
    if (!group.isDirectory()) continue;
    const srcDir = resolve(modulesDir, group.name, 'src');
    if (!existsSync(srcDir)) continue;

    for (const mod of readdirSync(srcDir, { withFileTypes: true })) {
      if (!mod.isDirectory()) continue;
      const pagesEntry = resolve(srcDir, mod.name, 'Pages', 'index.ts');
      if (!existsSync(pagesEntry)) continue;

      const assemblyName = mod.name;
      // Derive the short module name (e.g. "SimpleModule.Products" → "Products")
      const name = assemblyName.startsWith('SimpleModule.')
        ? assemblyName.slice('SimpleModule.'.length)
        : assemblyName;

      entries.push({
        name,
        assemblyName,
        dir: resolve(srcDir, mod.name),
        pagesEntry,
      });
    }
  }

  return entries;
}

/**
 * Vite plugin that enables HMR for SimpleModule module pages.
 *
 * In production, module pages are loaded from static files:
 *   /_content/SimpleModule.Products/SimpleModule.Products.pages.js
 *
 * In dev mode, this plugin resolves those paths to the actual source files
 * (e.g. modules/Products/src/SimpleModule.Products/Pages/index.ts) so Vite
 * can serve them with full HMR / React Fast Refresh support.
 */
export function moduleHmrPlugin(repoRoot: string): Plugin {
  let modules: ModuleEntry[] = [];

  return {
    name: 'simplemodule:module-hmr',
    enforce: 'pre',

    configResolved() {
      modules = discoverModules(repoRoot);
    },

    configureServer(server: ViteDevServer) {
      // Intercept /_content/ requests and rewrite to source files
      server.middlewares.use((req, _res, next) => {
        if (!req.url?.startsWith('/_content/')) return next();

        for (const mod of modules) {
          const prefix = `/_content/${mod.assemblyName}/${mod.assemblyName}.pages.js`;
          if (req.url === prefix || req.url.startsWith(`${prefix}?`)) {
            // Rewrite to the Pages/index.ts source file
            req.url = `/@fs/${mod.pagesEntry}`;
            return next();
          }

          // Handle module CSS requests
          const cssPrefix = `/_content/${mod.assemblyName}/${mod.assemblyName.toLowerCase()}.css`;
          if (req.url === cssPrefix || req.url.startsWith(`${cssPrefix}?`)) {
            const cssPath = resolve(mod.dir, 'Pages', 'styles.css');
            if (existsSync(cssPath)) {
              req.url = `/@fs/${cssPath}`;
            }
            return next();
          }
        }

        next();
      });
    },

    resolveId(source: string) {
      // Handle virtual /_content/ imports
      for (const mod of modules) {
        const prefix = `/_content/${mod.assemblyName}/${mod.assemblyName}.pages.js`;
        if (source === prefix || source.startsWith(`${prefix}?`)) {
          return mod.pagesEntry;
        }
      }
      return undefined;
    },
  };
}
