import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import path from 'node:path';
import * as esbuild from 'esbuild';
import type { Plugin } from 'vite';

export interface VendorEntry {
  pkg: string;
  file: string;
  externals: string[];
}

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

export function vendorPaths(
  vendors: VendorEntry[] = defaultVendors,
  prefix = '/js/vendor',
): Record<string, string> {
  return Object.fromEntries(vendors.map((v) => [v.pkg, `${prefix}/${v.file}.js`]));
}

function getExportNames(require_: NodeRequire, pkg: string): string[] {
  try {
    return Object.keys(require_(pkg)).filter((k) => k !== 'default' && k !== '__esModule');
  } catch {
    return [];
  }
}

export function vendorBuildPlugin(options?: { vendors?: VendorEntry[]; outDir: string }): Plugin {
  const vendors = options?.vendors ?? defaultVendors;

  return {
    name: 'build-vendors',
    apply: 'build',
    async buildStart() {
      const outDir = options?.outDir ?? path.resolve(process.cwd(), '../wwwroot/js/vendor');
      const require_ = createRequire(import.meta.url);

      if (vendors.every((v) => existsSync(path.join(outDir, `${v.file}.js`)))) return;
      mkdirSync(outDir, { recursive: true });

      for (const v of vendors) {
        const outfile = path.join(outDir, `${v.file}.js`);

        await esbuild.build({
          entryPoints: [v.pkg],
          bundle: true,
          format: 'esm',
          platform: 'browser',
          external: v.externals,
          outfile,
          logLevel: 'warning',
        });

        let code = readFileSync(outfile, 'utf-8');

        // esbuild emits __require("pkg") for CJS externals — replace with ESM imports
        const imports: string[] = [];
        for (let i = 0; i < v.externals.length; i++) {
          const ext = v.externals[i];
          const re = new RegExp(
            `__require\\("${ext.replace(/[.*+?^${}()|[\]\\/]/g, '\\$&')}"\\)`,
            'g',
          );
          if (re.test(code)) {
            imports.push(`import * as __ext${i} from "${ext}";`);
            code = code.replace(re, `__ext${i}`);
          }
        }
        if (imports.length) code = `${imports.join('\n')}\n${code}`;

        // esbuild CJS→ESM only produces `export default` — add named exports
        const exportNames = getExportNames(require_, v.pkg);
        if (exportNames.length) {
          const match = code.match(/export\s+default\s+(.+?)\s*;\s*$/m);
          if (match) {
            const named = exportNames.map((e) => `  ${e}`).join(',\n');
            code = code.replace(
              match[0],
              `var __mod = ${match[1]};\nexport default __mod;\nexport var {\n${named}\n} = __mod;\n`,
            );
          }
        }

        writeFileSync(outfile, code);
      }
    },
  };
}
