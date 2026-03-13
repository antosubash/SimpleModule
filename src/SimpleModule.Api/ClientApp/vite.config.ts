import path from 'node:path';
import { defaultVendors, vendorBuildPlugin, vendorPaths } from '@simplemodule/client/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [
    vendorBuildPlugin({
      outDir: path.resolve(__dirname, '../wwwroot/js/vendor'),
    }),
    react(),
  ],
  build: {
    outDir: path.resolve(__dirname, '../wwwroot/js'),
    emptyOutDir: false,
    rollupOptions: {
      input: path.resolve(__dirname, 'app.tsx'),
      external: defaultVendors.map((v) => v.pkg),
      output: {
        entryFileNames: 'app.js',
        paths: vendorPaths(),
      },
    },
  },
});
