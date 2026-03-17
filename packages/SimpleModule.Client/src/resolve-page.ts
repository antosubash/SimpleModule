export async function resolvePage(name: string) {
  const moduleName = name.split('/')[0];
  const cacheBuster = (document.querySelector('meta[name="cache-buster"]') as HTMLMetaElement)
    ?.content;
  const suffix = cacheBuster ? `?v=${cacheBuster}` : '';
  const mod = await import(
    /* @vite-ignore */
    `/_content/${moduleName}/${moduleName}.pages.js${suffix}`
  );

  if (!mod.pages) {
    throw new Error(
      `Module "${moduleName}" does not export a "pages" record. Check ${moduleName}.pages.js.`,
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

  return page.default ? page : { default: page };
}
