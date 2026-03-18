import { createInertiaApp, router } from '@inertiajs/react';
import { resolvePage } from '@simplemodule/client/resolve-page';
import { createRoot } from 'react-dom/client';

// Handle non-Inertia error responses (404, 500, etc.) by showing a toast
// instead of the default "must receive a valid Inertia response" error.
router.on('invalid', (event) => {
  event.preventDefault();

  const response = event.detail.response;
  const body = response.data as { detail?: string; title?: string } | undefined;
  const message = body?.detail ?? body?.title ?? `Server error (${response.status})`;
  showErrorToast(message);
});

function showErrorToast(message: string) {
  const container = document.createElement('div');
  container.role = 'alert';
  container.className =
    'fixed bottom-4 right-4 z-[100] max-w-md rounded-xl border border-danger/20 bg-danger-bg p-4 text-danger-text shadow-lg animate-in slide-in-from-bottom-full';
  container.innerHTML = `
    <div class="flex items-start gap-3">
      <div class="flex-1">
        <p class="text-sm font-semibold">Error</p>
        <p class="text-sm opacity-90">${message.replace(/[<>"'&]/g, (c) => `&#${c.charCodeAt(0)};`)}</p>
      </div>
      <button class="rounded-lg p-1 text-danger-text/60 hover:text-danger-text" aria-label="Close">
        <svg class="h-4 w-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M18 6 6 18M6 6l12 12" /></svg>
      </button>
    </div>
  `;
  container.querySelector('button')?.addEventListener('click', () => container.remove());
  document.body.appendChild(container);
  setTimeout(() => container.remove(), 8000);
}

createInertiaApp({
  resolve: resolvePage,
  setup({ el, App, props }) {
    createRoot(el).render(<App {...props} />);
  },
});
