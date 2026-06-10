interface BundleCandidate {
  /** Base URL of the bundle (no cache-buster suffix). */
  url: string;
  /** Assembly name, used in error messages only. */
  assemblyName: string;
}

// Caches the candidate that successfully served a module's bundle, keyed by the
// module's short name. Inertia calls resolvePage on every navigation, so without
// this the "wrong" candidate would 404 again on every page load for that module.
// We only ever store a candidate whose URL actually resolved.
const resolvedBundles = new Map<string, BundleCandidate>();

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
  return moduleAssets ?? null;
}

/**
 * Ordered bundle URLs to try for a module: the exact path from the module's
 * manifest first (no probing needed), then the legacy convention candidates —
 * assembly-qualified "SimpleModule.<Module>" before the bare module name (#224).
 * A previously resolved candidate short-circuits the whole list.
 */
function buildCandidates(moduleName: string): BundleCandidate[] {
  const cached = resolvedBundles.get(moduleName);
  if (cached) return [cached];

  const candidates: BundleCandidate[] = [];
  const manifestEntry = getModuleAssets()?.[moduleName];
  if (manifestEntry) {
    candidates.push({
      url: `/${manifestEntry}`,
      assemblyName: manifestEntry.split('/')[1] ?? moduleName,
    });
  }

  for (const assemblyName of [`SimpleModule.${moduleName}`, moduleName]) {
    const url = `/_content/${assemblyName}/${assemblyName}.pages.js`;
    if (!candidates.some((c) => c.url === url)) {
      candidates.push({ url, assemblyName });
    }
  }

  return candidates;
}

export async function resolvePage(name: string) {
  const moduleName = name.split('/')[0];
  const cacheBuster = (document.querySelector('meta[name="cache-buster"]') as HTMLMetaElement)
    ?.content;
  const suffix = cacheBuster ? `?v=${cacheBuster}` : '';

  const candidates = buildCandidates(moduleName);
  // biome-ignore lint/suspicious/noExplicitAny: matches existing dynamic-import shape
  let mod: any;
  let resolvedCandidate = candidates[0];
  let lastError: unknown;
  for (const candidate of candidates) {
    try {
      mod = await import(/* @vite-ignore */ `${candidate.url}${suffix}`);
      resolvedCandidate = candidate;
      resolvedBundles.set(moduleName, candidate);
      break;
    } catch (err) {
      lastError = err;
    }
  }

  if (!mod) {
    throw new Error(
      `Could not load pages bundle for module "${moduleName}". ` +
        `Tried ${candidates.map((c) => c.url).join(', ')}. Last error: ${String(lastError)}`,
    );
  }

  if (!mod.pages) {
    throw new Error(
      `Module "${moduleName}" does not export a "pages" record. Check ${resolvedCandidate.assemblyName}.pages.js.`,
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
