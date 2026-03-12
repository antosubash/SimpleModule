import { defineConfig, type Plugin } from 'vite';
import react from '@vitejs/plugin-react';
import * as esbuild from 'esbuild';
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'fs';
import { createRequire } from 'module';
import path from 'path';

const require_ = createRequire(import.meta.url);
const vendorDir = path.resolve(__dirname, '../wwwroot/js/vendor');

const vendors = [
  { pkg: 'react', file: 'react', externals: [] as string[] },
  { pkg: 'react-dom', file: 'react-dom', externals: ['react'] },
  { pkg: 'react/jsx-runtime', file: 'react-jsx-runtime', externals: ['react'] },
  { pkg: 'react-dom/client', file: 'react-dom-client', externals: ['react', 'react-dom'] },
  {
    pkg: '@inertiajs/react', file: 'inertiajs-react',
    externals: ['react', 'react-dom', 'react/jsx-runtime', 'react-dom/client'],
  },
];

const vendorPaths: Record<string, string> = Object.fromEntries(
  vendors.map(v => [v.pkg, `/js/vendor/${v.file}.js`]),
);

function getExportNames(pkg: string): string[] {
  try {
    return Object.keys(require_(pkg)).filter(k => k !== 'default' && k !== '__esModule');
  } catch {
    return [];
  }
}

function vendorBuildPlugin(): Plugin {
  return {
    name: 'build-vendors',
    apply: 'build',
    async buildStart() {
      if (vendors.every(v => existsSync(path.join(vendorDir, `${v.file}.js`)))) return;
      mkdirSync(vendorDir, { recursive: true });

      for (const v of vendors) {
        const outfile = path.join(vendorDir, `${v.file}.js`);

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
          const re = new RegExp(`__require\\("${ext.replace(/[.*+?^${}()|[\]\\/]/g, '\\$&')}"\\)`, 'g');
          if (re.test(code)) {
            imports.push(`import * as __ext${i} from "${ext}";`);
            code = code.replace(re, `__ext${i}`);
          }
        }
        if (imports.length) code = imports.join('\n') + '\n' + code;

        // esbuild CJS→ESM only produces `export default` — add named exports
        const exportNames = getExportNames(v.pkg);
        if (exportNames.length) {
          const match = code.match(/export\s+default\s+(.+?)\s*;\s*$/m);
          if (match) {
            const named = exportNames.map(e => `  ${e}`).join(',\n');
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

export default defineConfig({
  plugins: [vendorBuildPlugin(), react()],
  build: {
    outDir: path.resolve(__dirname, '../wwwroot/js'),
    emptyOutDir: false,
    rollupOptions: {
      input: path.resolve(__dirname, 'app.tsx'),
      external: vendors.map(v => v.pkg),
      output: {
        entryFileNames: 'app.js',
        paths: vendorPaths,
      },
    },
  },
});
