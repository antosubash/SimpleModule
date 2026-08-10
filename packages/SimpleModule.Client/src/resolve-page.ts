// Caches the assembly name that successfully served a module's bundle, keyed by
// the module's short name. Inertia calls resolvePage on every navigation, so
// without this the "wrong" candidate would 404 again on every page load for that
// module. We only ever store a name that actually resolved.
const resolvedAssemblies = new Map<string, string>();

// The server emits the module name -> RCL assembly name mapping into the page shell
// (see HtmlFileInertiaPageRenderer). Reading it means the very first request for a
// module's bundle goes to the path that actually serves it, instead of guessing and
// eating a 404 (#287). Parsed once — the shell is static for the life of the document.
let declaredAssemblies: Record<string, string> | undefined;

function getDeclaredAssemblies(): Record<string, string> {
  if (declaredAssemblies) return declaredAssemblies;

  const json = document.querySelector('script[data-module-assemblies]')?.textContent;
  let parsed: unknown;
  try {
    parsed = json ? JSON.parse(json) : undefined;
  } catch {
    // A malformed map is not worth failing navigation over — fall back to probing.
    parsed = undefined;
  }
  // Anything that isn't a plain object (including `null`, which is truthy-checked
  // away below but would still throw on property access) becomes an empty map, so
  // the memo always sticks and lookups never blow up.
  declaredAssemblies =
    typeof parsed === 'object' && parsed !== null ? (parsed as Record<string, string>) : {};
  return declaredAssemblies;
}

export async function resolvePage(name: string) {
  const moduleName = name.split('/')[0];
  const cacheBuster = (document.querySelector('meta[name="cache-buster"]') as HTMLMetaElement)
    ?.content;
  const suffix = cacheBuster ? `?v=${cacheBuster}` : '';

  // A module's bundle is served under its RCL AssemblyName: "SimpleModule.<Module>"
  // for framework modules, a bare "<Module>" for ones scaffolded by `sm new module`.
  // Prefer the name the server declared; only guess when the shell predates that
  // mapping, and then try the assembly-qualified form first (#224). Once a module
  // resolves, reuse that name directly so later navigations never re-probe.
  const cached = resolvedAssemblies.get(moduleName);
  const declared = getDeclaredAssemblies()[moduleName];
  const guesses = [`SimpleModule.${moduleName}`, moduleName];
  const candidates = cached
    ? [cached]
    : declared
      ? [declared, ...guesses.filter((guess) => guess !== declared)]
      : guesses;
  // biome-ignore lint/suspicious/noExplicitAny: matches existing dynamic-import shape
  let mod: any;
  let assemblyName = candidates[0];
  let lastError: unknown;
  let declaredError: unknown;
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
      if (candidate === declared) declaredError = err;
    }
  }

  if (!mod) {
    // When the server declared an assembly, that bundle failing is the real cause —
    // report it rather than the 404 from a fallback probe that was never going to
    // resolve, which would bury e.g. a syntax error inside the module's own bundle.
    throw new Error(
      `Could not load pages bundle for module "${moduleName}". ` +
        `Tried ${candidates.join(', ')}. Error: ${String(declaredError ?? lastError)}`,
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
