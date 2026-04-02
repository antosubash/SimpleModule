import path from 'node:path';
import { defaultVendors, vendorBuildPlugin, vendorPaths } from '@simplemodule/client/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

const isDev = process.env.VITE_MODE !== 'prod';

export default defineConfig({
  plugins: [
    vendorBuildPlugin({
      outDir: path.resolve(import.meta.dirname, '../wwwroot/js/vendor'),
    }),
    react(),
  ],
  build: {
    sourcemap: isDev,
    minify: isDev ? false : 'esbuild',
    outDir: path.resolve(import.meta.dirname, '../wwwroot/js'),
    emptyOutDir: false,
    rolldownOptions: {
      input: path.resolve(import.meta.dirname, 'app.tsx'),
      external: defaultVendors.map((v) => v.pkg),
      output: {
        entryFileNames: 'app.js',
        paths: vendorPaths(),
      },
    },
  },
});
