import { usePage } from '@inertiajs/react';
import * as React from 'react';
import type { SharedProps } from './types';

const DISMISS_KEY = 'branding-topbar-dismissed';

/**
 * A full-width announcement/utility bar rendered above the app chrome when branding
 * enables it. Optionally dismissible (remembered in localStorage).
 */
export function TopBar() {
  const { props } = usePage<SharedProps & Record<string, unknown>>();
  const topBar = props.branding?.topBar;
  const [dismissed, setDismissed] = React.useState(false);

  React.useEffect(() => {
    if (topBar?.dismissible) {
      setDismissed(localStorage.getItem(DISMISS_KEY) === 'true');
    }
  }, [topBar?.dismissible]);

  if (!topBar?.enabled || dismissed || !topBar.message) return null;

  const dismiss = () => {
    setDismissed(true);
    localStorage.setItem(DISMISS_KEY, 'true');
  };

  return (
    <div
      className="w-full px-4 py-2 text-sm flex items-center justify-center gap-4"
      style={{ background: topBar.backgroundColor, color: topBar.textColor }}
    >
      <span>{topBar.message}</span>
      {topBar.links.map((link) => (
        <a
          key={`${link.label}-${link.url}`}
          href={link.url}
          className="underline font-medium"
          style={{ color: topBar.textColor }}
        >
          {link.label}
        </a>
      ))}
      {topBar.dismissible && (
        <button
          type="button"
          onClick={dismiss}
          aria-label="Dismiss"
          className="ml-auto opacity-80 hover:opacity-100"
          style={{ color: topBar.textColor }}
        >
          <svg
            aria-hidden="true"
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth={2}
            viewBox="0 0 24 24"
          >
            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      )}
    </div>
  );
}
