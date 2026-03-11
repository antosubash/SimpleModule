import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

const buildTarget = process.env.VITE_BUILD_TARGET;

const libConfig = defineConfig({
  build: {
    lib: {
      entry: resolve(__dirname, 'Scripts/index.ts'),
      formats: ['es'],
      fileName: () => 'Users.lib.module.js',
    },
    outDir: 'wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      output: { inlineDynamicImports: true },
    },
  },
});

const pagesConfig = defineConfig({
  plugins: [react()],
  build: {
    lib: {
      entry: resolve(__dirname, 'Pages/index.ts'),
      formats: ['es'],
      fileName: () => 'Users.pages.js',
    },
    outDir: 'wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      external: ['react', 'react-dom', 'react/jsx-runtime', '@inertiajs/react'],
      output: { inlineDynamicImports: true },
    },
  },
});

export default buildTarget === 'pages' ? pagesConfig : libConfig;
