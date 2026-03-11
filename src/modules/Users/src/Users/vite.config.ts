import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  build: {
    lib: {
      entry: resolve(__dirname, 'Scripts/index.ts'),
      formats: ['es'],
      fileName: () => 'Users.lib.module.js',
    },
    outDir: 'wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      output: {
        inlineDynamicImports: true,
      },
    },
  },
});
