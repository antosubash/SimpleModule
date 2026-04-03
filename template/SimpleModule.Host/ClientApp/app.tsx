import { createInertiaApp, router } from '@inertiajs/react';
import { resolvePage } from '@simplemodule/client/resolve-page';
import { createRoot } from 'react-dom/client';

// Navigation progress bar — 150ms delay so instant navigations don't flash
const PROGRESS_DELAY = 150;
const PROGRESS_FILL_PAUSE = 200;
const PROGRESS_FADE = 300;

let progressBar: HTMLDivElement | null = null;
let startTimer: ReturnType<typeof setTimeout> | null = null;
let fadeTimer: ReturnType<typeof setTimeout> | null = null;
let resetTimer: ReturnType<typeof setTimeout> | null = null;

function getProgressBar() {
  if (!progressBar) {
    progressBar = document.createElement('div');
    progressBar.style.cssText =
      'position:fixed;top:0;left:0;height:3px;background:var(--color-primary,#059669);z-index:9999;' +
      `transition:width ${PROGRESS_FADE}ms ease;pointer-events:none;opacity:0;`;
    document.body.appendChild(progressBar);
  }
  return progressBar;
}

function clearTimers() {
  if (startTimer) {
    clearTimeout(startTimer);
    startTimer = null;
  }
  if (fadeTimer) {
    clearTimeout(fadeTimer);
    fadeTimer = null;
  }
  if (resetTimer) {
    clearTimeout(resetTimer);
    resetTimer = null;
  }
}

// Intercept plain <a> clicks from the Blazor layout (sidebar, nav, dropdowns) and
// route them through Inertia so the page swap is SPA-style — no full reload.
const nonInertiaPathPrefixes = ['/Identity/', '/swagger', '/health', '/connect/'];

document.addEventListener('click', (event) => {
  const link = (event.target as Element).closest?.('a');
  if (!link?.href) return;

  // Respect modifier keys (new tab), target attrs, downloads
  if (event.defaultPrevented) return;
  if (event.button !== 0) return;
  if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
  if (link.target && link.target !== '_self') return;
  if (link.hasAttribute('download')) return;
  if (link.dataset.inertia === 'false') return;

  // Only same-origin
  if (link.origin !== window.location.origin) return;

  // Skip non-Inertia server routes (Identity, Swagger, health checks, OAuth)
  const path = link.pathname;
  if (nonInertiaPathPrefixes.some((prefix) => path.startsWith(prefix))) return;

  event.preventDefault();
  router.visit(link.href);
});

router.on('start', () => {
  clearTimers();
  startTimer = setTimeout(() => {
    const b = getProgressBar();
    b.style.width = '0%';
    b.style.opacity = '1';
    void b.offsetWidth; // force reflow so transition starts from 0%
    b.style.width = '80%';
  }, PROGRESS_DELAY);
});

router.on('finish', () => {
  clearTimers();
  if (progressBar) {
    progressBar.style.width = '100%';
    fadeTimer = setTimeout(() => {
      if (progressBar) progressBar.style.opacity = '0';
      resetTimer = setTimeout(() => {
        if (progressBar) progressBar.style.width = '0%';
      }, PROGRESS_FADE);
    }, PROGRESS_FILL_PAUSE);
  }
});

// Handle non-Inertia error responses (404, 500, etc.) by showing a toast
// instead of the default "must receive a valid Inertia response" error.
router.on('httpException', (event) => {
  event.preventDefault();

  const response = event.detail.response;
  const body = response.data as { detail?: string; title?: string } | string | undefined;
  let parsed: { detail?: string; title?: string } | undefined;
  if (typeof body === 'string') {
    try {
      parsed = JSON.parse(body);
    } catch {
      // non-JSON response body
    }
  } else {
    parsed = body;
  }
  const message = parsed?.detail ?? parsed?.title ?? `Server error (${response.status})`;
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
        <p class="text-sm opacity-90"></p>
      </div>
      <button class="rounded-lg p-1 text-danger-text/60 hover:text-danger-text" aria-label="Close">
        <svg class="h-4 w-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M18 6 6 18M6 6l12 12" /></svg>
      </button>
    </div>
  `;
  container.querySelector('p.opacity-90')!.textContent = message;
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
