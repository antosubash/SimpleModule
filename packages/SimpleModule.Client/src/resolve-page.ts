// Caches the assembly name that successfully served a module's bundle, keyed by
// the module's short name. Inertia calls resolvePage on every navigation, so
// without this the "wrong" candidate would 404 again on every page load for that
// module. We only ever store a name that actually resolved.
const resolvedAssemblies = new Map<string, string>();

// Module → bundle path map injected into the HTML shell by the host from each
// module's compile-time manifest (<script id="sm-module-assets">). Parsed once;
// null when the host (or an older framework version) ships no map.
let moduleAssets: Record<string, string> | null | undefined;

function getModuleAssets(): Record<string, string> | null {
  if (moduleAssets !== undefined) return moduleAssets;
  try {
    const el = document.getElementById('sm-module-assets');
    moduleAssets = el?.textContent ? JSON.parse(el.textContent) : null;
  } catch {
    moduleAssets = null;
  }
  return moduleAssets;
}

export async function resolvePage(name: string) {
  const moduleName = name.split('/')[0];
  const cacheBuster = (document.querySelector('meta[name="cache-buster"]') as HTMLMetaElement)
    ?.content;
  const suffix = cacheBuster ? `?v=${cacheBuster}` : '';

  // Framework modules serve their bundle under the assembly-qualified path
  // "SimpleModule.<Module>" (their RCL AssemblyName), whereas downstream apps
  // frequently ship modules under a bare assembly name (e.g. "Customers"). Try
  // the assembly-qualified form first so framework modules — the common, always-
  // present case — resolve without a 404 (#224), then fall back to the bare name
  // for consumer modules. Once a module resolves, reuse that name directly so
  // later navigations never re-probe.
  const cached = resolvedAssemblies.get(moduleName);
  const candidates = cached ? [cached] : [`SimpleModule.${moduleName}`, moduleName];
  // biome-ignore lint/suspicious/noExplicitAny: matches existing dynamic-import shape
  let mod: any;
  let assemblyName = candidates[0];
  let lastError: unknown;

  // Manifest-first: the host injects each module's exact bundle path, so no
  // candidate probing is needed. On failure fall through to the convention
  // probing below — the map may be stale relative to deployed assets.
  const manifestEntry = cached ? undefined : getModuleAssets()?.[moduleName];
  if (manifestEntry) {
    try {
      mod = await import(/* @vite-ignore */ `/${manifestEntry}${suffix}`);
      // The bundle directory is the assembly name: _content/{assembly}/{assembly}.pages.js
      const dir = manifestEntry.split('/')[1];
      if (dir) {
        assemblyName = dir;
        resolvedAssemblies.set(moduleName, dir);
      }
    } catch (err) {
      lastError = err;
    }
  }

  if (!mod) {
    for (const candidate of candidates) {
      try {
        mod = await import(
          /* @vite-ignore */
          `/_content/${candidate}/${candidate}.pages.js${suffix}`
        );
        assemblyName = candidate;
        resolvedAssemblies.set(moduleName, candidate);
        break;
      } catch (err) {
        lastError = err;
      }
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
