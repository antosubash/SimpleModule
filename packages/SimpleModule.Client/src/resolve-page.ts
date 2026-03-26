const loadedCss = new Set<string>();

function ensureModuleCss(moduleName: string, cacheBuster: string | undefined) {
  if (loadedCss.has(moduleName)) return;
  loadedCss.add(moduleName);

  const suffix = cacheBuster ? `?v=${cacheBuster}` : '';
  const href = `/_content/${moduleName}/${moduleName.toLowerCase()}.css${suffix}`;
  const link = document.createElement('link');
  link.rel = 'stylesheet';
  link.href = href;
  link.onerror = () => {
    // Module has no CSS — remove the broken link and suppress further attempts
    link.remove();
  };
  document.head.appendChild(link);
}

export async function resolvePage(name: string) {
  const moduleName = name.split('/')[0];
  const assemblyName = `SimpleModule.${moduleName}`;
  const cacheBuster = (document.querySelector('meta[name="cache-buster"]') as HTMLMetaElement)
    ?.content;
  const suffix = cacheBuster ? `?v=${cacheBuster}` : '';

  ensureModuleCss(moduleName, cacheBuster);

  const mod = await import(
    /* @vite-ignore */
    `/_content/${assemblyName}/${assemblyName}.pages.js${suffix}`
  );

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
