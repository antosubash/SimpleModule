import { Link, usePage } from '@inertiajs/react';
import * as React from 'react';
import { DarkModeToggle } from './dark-mode-toggle';
import { MobileOverlay } from './public-layout-mobile';
import { DesktopMenu } from './public-layout-nav';
import type { SharedProps } from './types';

export function PublicLayout({ children }: { children: React.ReactNode }) {
  const { props } = usePage<SharedProps & Record<string, unknown>>();
  const { publicMenu = [] } = props;
  const [mobileOpen, setMobileOpen] = React.useState(false);
  const closeMobile = React.useCallback(() => setMobileOpen(false), []);

  return (
    <>
      <nav
        className="sticky top-0 z-50 border-b border-border bg-surface-overlay"
        style={{
          backdropFilter: 'blur(20px)',
          WebkitBackdropFilter: 'blur(20px)',
        }}
      >
        <div className="max-w-7xl mx-auto flex items-center px-6 py-3">
          <div className="flex items-center gap-6">
            <Link
              href="/"
              className="flex items-center gap-2.5 no-underline font-bold text-text group"
              style={{ fontFamily: "'Sora',sans-serif" }}
            >
              <span
                className="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-bold shadow-md transition-transform duration-200 group-hover:scale-105"
                style={{
                  background: 'linear-gradient(135deg,var(--color-primary),var(--color-accent))',
                }}
              >
                S
              </span>
              <span className="text-base">SimpleModule</span>
            </Link>
            <DesktopMenu items={publicMenu} />
          </div>
          <div className="ml-auto flex items-center gap-3">
            <button
              type="button"
              className="md:hidden p-1 text-text-muted hover:text-text"
              onClick={() => setMobileOpen(true)}
              aria-expanded={mobileOpen}
              aria-label="Open menu"
            >
              <svg
                aria-hidden="true"
                className="w-6 h-6"
                fill="none"
                stroke="currentColor"
                strokeWidth={2}
                viewBox="0 0 24 24"
              >
                <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            </button>
            <DarkModeToggle />
            <a
              href="/Identity/Account/Login"
              className="btn-ghost btn-sm no-underline hidden md:inline-flex"
            >
              Log in
            </a>
            <a
              href="/Identity/Account/Register"
              className="btn-primary btn-sm no-underline hidden md:inline-flex"
            >
              Sign up
            </a>
          </div>
        </div>
      </nav>

      <MobileOverlay items={publicMenu} isOpen={mobileOpen} onClose={closeMobile} />

      <main className="max-w-7xl mx-auto mt-8 mb-16 px-4 sm:px-6">{children}</main>
    </>
  );
}
