export async function resolvePage(name: string) {
  const moduleName = name.split('/')[0];
  const cacheBuster = (document.querySelector('meta[name="cache-buster"]') as HTMLMetaElement)
    ?.content;
  const suffix = cacheBuster ? `?v=${cacheBuster}` : '';

  // Downstream apps frequently ship modules under a bare assembly name
  // (e.g. "Customers", "Invoices") rather than the framework's "SimpleModule.X"
  // convention. Try the bare name first, then fall back to the prefixed form
  // so framework modules continue to resolve.
  const candidates = [moduleName, `SimpleModule.${moduleName}`];
  // biome-ignore lint/suspicious/noExplicitAny: matches existing dynamic-import shape
  let mod: any;
  let assemblyName = candidates[0];
  let lastError: unknown;
  for (const candidate of candidates) {
    try {
      mod = await import(
        /* @vite-ignore */
        `/_content/${candidate}/${candidate}.pages.js${suffix}`
      );
      assemblyName = candidate;
      break;
    } catch (err) {
      lastError = err;
    }
  }

  if (!mod) {
    throw new Error(
      `Could not load pages bundle for module "${moduleName}". ` +
        `Tried ${candidates.join(', ')}. Last error: ${String(lastError)}`,
    );
  }

  if (!mod.pages) {
    throw new Error(
      `Module "${moduleName}" does not export a "pages" record. Check ${assemblyName}.pages.js.`,
    );
  }

  const page = mod.pages[name];

  if (!page) {
    const available = Object.keys(mod.pages).join(', ');
    throw new Error(
      `Page "${name}" not found in module "${moduleName}". Available pages: ${available}. ` +
        'You may need to rebuild the module: npx vite build',
    );
  }

  // Support lazy page entries: () => import('./SomePage')
  if (typeof page === 'function') {
    const resolved = await page();
    return resolved.default ? resolved : { default: resolved };
  }

  return page.default ? page : { default: page };
}
