import { createInertiaApp } from '@inertiajs/react';
import { createRoot } from 'react-dom/client';

createInertiaApp({
  resolve: async (name) => {
    // "Products/Browse" → dynamic import from /_content/Products/Products.pages.js
    const moduleName = name.split('/')[0];
    const mod = await import(
      /* @vite-ignore */
      `/_content/${moduleName}/${moduleName}.pages.js`
    );
    const page = mod.pages[name];
    return page.default ? page : { default: page };
  },
  setup({ el, App, props }) {
    createRoot(el).render(<App {...props} />);
  },
});
