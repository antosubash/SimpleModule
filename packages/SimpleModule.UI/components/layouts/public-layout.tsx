import { Link, usePage } from '@inertiajs/react';
import * as React from 'react';
import { DarkModeToggle } from './dark-mode-toggle';
import type { PublicMenuItem, SharedProps } from './types';

function MenuLink({
  item,
  className,
  onClick,
  children,
}: {
  item: PublicMenuItem;
  className: string;
  onClick?: () => void;
  children: React.ReactNode;
}) {
  if (item.openInNewTab) {
    return (
      <a
        href={item.url}
        className={className}
        target="_blank"
        rel="noopener noreferrer"
        onClick={onClick}
      >
        {children}
      </a>
    );
  }
  return (
    <Link href={item.url} className={className} onClick={onClick}>
      {children}
    </Link>
  );
}

function DesktopDropdown({ item }: { item: PublicMenuItem }) {
  return (
    <div className="relative group">
      <MenuLink
        item={item}
        className={`text-sm text-text-muted no-underline hover:text-primary transition-colors cursor-pointer ${item.cssClass}`}
      >
        {item.label}
      </MenuLink>
      <div className="absolute hidden group-hover:block top-full left-0 mt-1 py-1 bg-surface-overlay border border-border rounded-lg shadow-lg min-w-[160px] z-50">
        {item.children.map((child) =>
          child.children.length === 0 ? (
            <MenuLink
              key={child.url}
              item={child}
              className="block px-4 py-2 text-sm text-text-muted hover:text-primary hover:bg-surface-hover"
            >
              {child.label}
            </MenuLink>
          ) : (
            <div key={child.url} className="relative group/sub">
              <MenuLink
                item={child}
                className="block px-4 py-2 text-sm text-text-muted hover:text-primary hover:bg-surface-hover"
              >
                {child.label}
              </MenuLink>
              <div className="absolute hidden group-hover/sub:block top-0 left-full ml-0.5 py-1 bg-surface-overlay border border-border rounded-lg shadow-lg min-w-[160px] z-50">
                {child.children.map((grandchild) => (
                  <MenuLink
                    key={grandchild.url}
                    item={grandchild}
                    className="block px-4 py-2 text-sm text-text-muted hover:text-primary hover:bg-surface-hover"
                  >
                    {grandchild.label}
                  </MenuLink>
                ))}
              </div>
            </div>
          ),
        )}
      </div>
    </div>
  );
}

function DesktopMenu({ items }: { items: PublicMenuItem[] }) {
  return (
    <div className="hidden md:flex items-center gap-1">
      {items.map((item) =>
        item.children.length === 0 ? (
          <MenuLink
            key={item.url}
            item={item}
            className={`text-sm text-text-muted no-underline hover:text-primary transition-colors ${item.cssClass}`}
          >
            {item.label}
          </MenuLink>
        ) : (
          <DesktopDropdown key={item.url} item={item} />
        ),
      )}
    </div>
  );
}

function MobileOverlayItem({ item, onClose }: { item: PublicMenuItem; onClose: () => void }) {
  const [open, setOpen] = React.useState(false);

  if (item.children.length === 0) {
    return (
      <MenuLink
        item={item}
        className={`block py-3 text-lg text-text no-underline border-b border-border/50 ${item.cssClass}`}
        onClick={onClose}
      >
        {item.label}
      </MenuLink>
    );
  }

  return (
    <div className="border-b border-border/50">
      <button
        type="button"
        className="flex items-center justify-between w-full py-3 text-lg text-text bg-transparent border-none cursor-pointer"
        onClick={() => setOpen((o) => !o)}
      >
        {item.label}
        <svg
          aria-hidden="true"
          className={`w-5 h-5 transition-transform duration-200 ${open ? 'rotate-180' : ''}`}
          fill="none"
          stroke="currentColor"
          strokeWidth={2}
          viewBox="0 0 24 24"
        >
          <path d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      {open && (
        <div className="pl-4 pb-2 space-y-1">
          {item.children.map((child) => (
            <MenuLink
              key={child.url}
              item={child}
              className="block py-2.5 text-base text-text-secondary no-underline"
              onClick={onClose}
            >
              {child.label}
            </MenuLink>
          ))}
        </div>
      )}
    </div>
  );
}

function MobileOverlay({
  items,
  isOpen,
  onClose,
}: {
  items: PublicMenuItem[];
  isOpen: boolean;
  onClose: () => void;
}) {
  const overlayRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    if (!isOpen) return;
    document.body.style.overflow = 'hidden';

    function handleEscape(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose();
    }
    document.addEventListener('keydown', handleEscape);
    return () => {
      document.body.style.overflow = '';
      document.removeEventListener('keydown', handleEscape);
    };
  }, [isOpen, onClose]);

  // Focus trap
  React.useEffect(() => {
    if (!isOpen || !overlayRef.current) return;

    const focusable = 'a[href], button:not([disabled]), input:not([disabled])';
    const elements = overlayRef.current.querySelectorAll<HTMLElement>(focusable);
    if (elements.length) elements[0].focus();

    function handleTab(e: KeyboardEvent) {
      if (e.key !== 'Tab' || !overlayRef.current) return;
      const els = overlayRef.current.querySelectorAll<HTMLElement>(focusable);
      if (!els.length) return;
      const first = els[0];
      const last = els[els.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    }
    document.addEventListener('keydown', handleTab);
    return () => document.removeEventListener('keydown', handleTab);
  }, [isOpen]);

  // Close on resize to desktop
  React.useEffect(() => {
    if (!isOpen) return;
    const mql = window.matchMedia('(min-width: 768px)');
    function handleChange(e: MediaQueryListEvent) {
      if (e.matches) onClose();
    }
    mql.addEventListener('change', handleChange);
    return () => mql.removeEventListener('change', handleChange);
  }, [isOpen, onClose]);

  return (
    <div
      ref={overlayRef}
      className={`public-overlay ${isOpen ? 'open' : ''}`}
      role="dialog"
      aria-modal="true"
      aria-hidden={!isOpen}
    >
      <div className="flex flex-col h-full">
        {/* Overlay header */}
        <div className="flex items-center justify-between px-6 py-3 border-b border-border">
          <Link
            href="/"
            className="flex items-center gap-2.5 no-underline font-bold text-text"
            style={{ fontFamily: "'Sora',sans-serif" }}
          >
            <span
              className="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-bold"
              style={{
                background: 'linear-gradient(135deg,var(--color-primary),var(--color-accent))',
              }}
            >
              S
            </span>
            <span className="text-base">SimpleModule</span>
          </Link>
          <button
            type="button"
            onClick={onClose}
            className="p-2 text-text-muted hover:text-text"
            aria-label="Close menu"
          >
            <svg
              aria-hidden="true"
              className="w-6 h-6"
              fill="none"
              stroke="currentColor"
              strokeWidth={2}
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Overlay nav items */}
        <nav className="flex-1 overflow-y-auto px-6 py-6 space-y-1">
          {items?.map((item) => (
            <MobileOverlayItem key={item.url} item={item} onClose={onClose} />
          ))}
        </nav>

        {/* Overlay footer */}
        <div className="px-6 py-6 border-t border-border space-y-3">
          <a
            href="/Identity/Account/Login"
            className="btn-ghost w-full text-center no-underline block py-3"
          >
            Log in
          </a>
          <a
            href="/Identity/Account/Register"
            className="btn-primary w-full text-center no-underline block py-3"
          >
            Sign up
          </a>
        </div>
      </div>
    </div>
  );
}

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
