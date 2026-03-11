import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: path.resolve(__dirname, '../wwwroot/js'),
    emptyOutDir: false,
    rollupOptions: {
      input: path.resolve(__dirname, 'app.tsx'),
      external: [
        'react',
        'react-dom',
        'react/jsx-runtime',
        'react-dom/client',
        '@inertiajs/react',
      ],
      output: {
        entryFileNames: 'app.js',
        paths: {
          'react': '/js/vendor/react.js',
          'react-dom': '/js/vendor/react-dom.js',
          'react/jsx-runtime': '/js/vendor/react-jsx-runtime.js',
          'react-dom/client': '/js/vendor/react-dom-client.js',
          '@inertiajs/react': '/js/vendor/inertiajs-react.js',
        },
      },
    },
  },
});
