import { usePage } from '@inertiajs/react';
import { safeUrl } from './safe-url';
import type { SharedProps } from './types';

/**
 * A configurable site footer rendered below page content when branding enables it.
 */
export function Footer() {
  const { props } = usePage<SharedProps & Record<string, unknown>>();
  const footer = props.branding?.footer;
  const appName = props.branding?.appName ?? 'SimpleModule';
  if (!footer?.enabled) return null;

  const year = new Date().getFullYear();

  return (
    <footer className="border-t border-border px-6 py-6 text-sm text-text-muted">
      <div className="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-3">
        <div>
          {footer.showCopyright && (
            <span>
              © {year} {appName}.{' '}
            </span>
          )}
          {footer.text && <span>{footer.text}</span>}
        </div>
        <div className="flex items-center gap-4">
          {footer.links.map((link) => (
            <a
              key={`${link.label}-${link.url}`}
              href={safeUrl(link.url)}
              className="hover:text-text no-underline"
            >
              {link.label}
            </a>
          ))}
        </div>
      </div>
    </footer>
  );
}
