export async function resolvePage(name: string) {
  const moduleName = name.split('/')[0];
  const mod = await import(
    /* @vite-ignore */
    `/_content/${moduleName}/${moduleName}.pages.js`
  );
  const page = mod.pages[name];
  return page.default ? page : { default: page };
}
