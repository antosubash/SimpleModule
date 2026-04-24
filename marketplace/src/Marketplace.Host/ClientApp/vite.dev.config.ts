import path from 'node:path';
import { moduleHmrPlugin } from '@simplemodule/client/vite-hmr';
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

const repoRoot = path.resolve(import.meta.dirname, '../../..');

export default defineConfig({
  plugins: [react(), tailwindcss(), moduleHmrPlugin(repoRoot)],
  root: import.meta.dirname,
  resolve: {
    alias: {
      '@': import.meta.dirname,
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    cors: true,
    // Ensure HMR works when accessed through ASP.NET proxy
    hmr: {
      port: 5173,
    },
  },
  // In dev server mode, CSS entry is loaded through the main app entry.
  // Tailwind scans source files via the @source directives in the CSS.
  css: {
    transformer: 'lightningcss',
  },
  optimizeDeps: {
    include: ['react', 'react-dom', 'react/jsx-runtime', 'react-dom/client', '@inertiajs/react'],
  },
});
