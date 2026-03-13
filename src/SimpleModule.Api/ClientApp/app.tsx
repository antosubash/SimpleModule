import { createInertiaApp } from '@inertiajs/react';
import { resolvePage } from '@simplemodule/client';
import { createRoot } from 'react-dom/client';

createInertiaApp({
  resolve: resolvePage,
  setup({ el, App, props }) {
    createRoot(el).render(<App {...props} />);
  },
});
