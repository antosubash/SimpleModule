const DANGEROUS_SCHEMES = ['javascript:', 'data:', 'vbscript:'];

/**
 * Returns a link href safe to render. Branding link URLs are admin-configured but
 * rendered to every visitor (top bar / footer, including public pages), so a
 * `javascript:`/`data:`/`vbscript:` URL would be stored XSS. Whitespace (including
 * tabs/newlines used to obfuscate the scheme, e.g. `java\tscript:`) is stripped
 * before the check; anything dangerous collapses to `#`.
 */
export function safeUrl(raw: string | null | undefined): string {
  const original = (raw ?? '').trim();
  if (!original) return '#';
  const collapsed = original.replace(/\s+/g, '').toLowerCase();
  if (DANGEROUS_SCHEMES.some((scheme) => collapsed.startsWith(scheme))) return '#';
  return original;
}
