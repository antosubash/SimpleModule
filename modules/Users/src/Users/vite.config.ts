import { resolve } from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

const isDev = process.env.VITE_MODE !== 'prod';

export default defineConfig({
  plugins: [react()],
  build: {
    lib: {
      entry: {
        'Users.pages': resolve(__dirname, 'Pages/index.ts'),
      },
      formats: ['es'],
      fileName: (_format, entryName) => `${entryName}.js`,
    },
    sourcemap: isDev,
    minify: isDev ? false : 'esbuild',
    outDir: 'wwwroot',
    emptyOutDir: false,
    rollupOptions: {
      external: ['react', 'react-dom', 'react/jsx-runtime', '@inertiajs/react'],
    },
  },
});
