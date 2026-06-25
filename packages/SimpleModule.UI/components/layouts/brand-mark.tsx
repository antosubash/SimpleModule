import { usePage } from '@inertiajs/react';
import type { SharedProps } from './types';

/**
 * Renders the app's brand: an uploaded logo if branding provides one, otherwise a
 * colored badge with the app name's first letter plus the app name text. Reads the
 * `branding` shared prop, falling back to "SimpleModule".
 */
export function BrandMark() {
  const { props } = usePage<SharedProps & Record<string, unknown>>();
  const branding = props.branding;
  const appName = branding?.appName ?? 'SimpleModule';

  if (branding?.logoUrl) {
    return (
      <img
        src={branding.logoUrl}
        alt={appName}
        className="h-8 w-auto max-w-[160px] object-contain"
      />
    );
  }

  return (
    <>
      <span
        className="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-bold shadow-md transition-transform duration-200 group-hover:scale-105 shrink-0"
        style={{ background: 'var(--color-primary)' }}
      >
        {appName.charAt(0).toUpperCase()}
      </span>
      <span className="text-base sidebar-label">{appName}</span>
    </>
  );
}
