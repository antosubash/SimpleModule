import { Link, usePage } from '@inertiajs/react';
import * as React from 'react';
import { DarkModeToggle } from './dark-mode-toggle';
import type { MenuItem, SharedProps } from './types';
import { UserDropdown } from './user-dropdown';

const ACTIVE_CLASS =
  'flex items-center gap-3 px-3 py-2 rounded-xl text-sm font-medium text-primary bg-primary-subtle no-underline transition-all duration-150';
const INACTIVE_CLASS =
  'flex items-center gap-3 px-3 py-2 rounded-xl text-sm text-text-secondary no-underline hover:bg-surface-raised hover:text-text transition-all duration-150';

function isActiveUrl(url: string, pathname: string): boolean {
  if (url === '/') return pathname === '/';
  return pathname.toLowerCase().startsWith(url.toLowerCase());
}

function SidebarIcon({ icon }: { icon: string }) {
  // biome-ignore lint/security/noDangerouslySetInnerHtml: trusted server-provided SVG icon content
  return <span className="sidebar-icon" dangerouslySetInnerHTML={{ __html: icon }} />;
}

interface MenuGroup {
  group: string | null;
  groupId: string;
  items: MenuItem[];
}

function groupAdminItems(items: MenuItem[]): MenuGroup[] {
  const groups: MenuGroup[] = [];
  let currentGroup: string | null = null;
  let currentItems: MenuItem[] = [];

  for (const item of items) {
    if (item.group !== currentGroup) {
      if (currentItems.length > 0) {
        groups.push({
          group: currentGroup,
          groupId: (currentGroup ?? '').replace(/ /g, '-').toLowerCase(),
          items: currentItems,
        });
      }
      currentGroup = item.group;
      currentItems = [];
    }
    currentItems.push(item);
  }
  if (currentItems.length > 0) {
    groups.push({
      group: currentGroup,
      groupId: (currentGroup ?? '').replace(/ /g, '-').toLowerCase(),
      items: currentItems,
    });
  }
  return groups;
}

function NavLink({
  item,
  pathname,
  onClick,
}: {
  item: MenuItem;
  pathname: string;
  onClick?: () => void;
}) {
  const active = isActiveUrl(item.url, pathname);
  return (
    <Link href={item.url} className={active ? ACTIVE_CLASS : INACTIVE_CLASS} onClick={onClick}>
      <SidebarIcon icon={item.icon} />
      <span className="sidebar-label">{item.label}</span>
    </Link>
  );
}

function AdminSection({
  adminItems,
  pathname,
  onLinkClick,
}: {
  adminItems: MenuItem[];
  pathname: string;
  onLinkClick?: () => void;
}) {
  const [open, setOpen] = React.useState(() => localStorage.getItem('admin-nav-open') !== 'false');
  const groups = React.useMemo(() => groupAdminItems(adminItems), [adminItems]);

  const toggle = React.useCallback(() => {
    setOpen((prev) => {
      const next = !prev;
      localStorage.setItem('admin-nav-open', String(next));
      return next;
    });
  }, []);

  return (
    <div className="pt-4 mt-4 border-t border-border">
      <button
        type="button"
        className="flex items-center gap-2 w-full px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-text-muted bg-transparent border-none cursor-pointer hover:text-text transition-colors"
        onClick={toggle}
      >
        <span className="sidebar-label">Admin</span>
        <svg
          aria-hidden="true"
          className={`w-3 h-3 transition-transform duration-200 ml-auto sidebar-label ${!open ? '-rotate-90' : ''}`}
          fill="none"
          stroke="currentColor"
          strokeWidth={2}
          viewBox="0 0 24 24"
        >
          <path d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      {open && (
        <div className="space-y-1 mt-1">
          {groups.map((menuGroup, idx) =>
            menuGroup.group !== null ? (
              <AdminGroup
                key={`${menuGroup.groupId}-${idx}`}
                group={menuGroup}
                pathname={pathname}
                onLinkClick={onLinkClick}
              />
            ) : (
              menuGroup.items.map((item) => (
                <NavLink key={item.url} item={item} pathname={pathname} onClick={onLinkClick} />
              ))
            ),
          )}
        </div>
      )}
    </div>
  );
}

function AdminGroup({
  group,
  pathname,
  onLinkClick,
}: {
  group: MenuGroup;
  pathname: string;
  onLinkClick?: () => void;
}) {
  const [open, setOpen] = React.useState(true);

  return (
    <div className="mt-2">
      <button
        type="button"
        className="flex items-center gap-2 w-full px-3 py-1 text-[11px] font-semibold uppercase tracking-wider text-text-muted bg-transparent border-none cursor-pointer hover:text-text transition-colors"
        onClick={() => setOpen((o) => !o)}
      >
        <span className="sidebar-label">{group.group}</span>
        <svg
          aria-hidden="true"
          className={`w-3 h-3 transition-transform duration-200 ml-auto sidebar-label ${!open ? 'rotate-180' : ''}`}
          fill="none"
          stroke="currentColor"
          strokeWidth={2}
          viewBox="0 0 24 24"
        >
          <path d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      {open && (
        <div className="space-y-0.5 mt-0.5">
          {group.items.map((item) => (
            <NavLink key={item.url} item={item} pathname={pathname} onClick={onLinkClick} />
          ))}
        </div>
      )}
    </div>
  );
}

export function AppLayout({ children }: { children: React.ReactNode }) {
  const { props } = usePage<SharedProps>();
  const { auth, menus, csrfToken } = props;
  const pathname = typeof window !== 'undefined' ? window.location.pathname : '/';

  const [collapsed, setCollapsed] = React.useState(
    () => localStorage.getItem('sidebar-collapsed') === 'true',
  );
  const [mobileOpen, setMobileOpen] = React.useState(false);

  const toggleCollapse = React.useCallback(() => {
    setCollapsed((prev) => {
      const next = !prev;
      localStorage.setItem('sidebar-collapsed', String(next));
      return next;
    });
  }, []);

  const closeMobile = React.useCallback(() => setMobileOpen(false), []);

  const isAdmin = auth.roles.includes('Admin');
  const displayName = auth.userName ?? 'User';
  const userInitial = displayName.charAt(0).toUpperCase();

  return (
    <div className="app-layout">
      {/* Mobile header */}
      <div className="app-mobile-header">
        <button
          type="button"
          onClick={() => setMobileOpen(true)}
          aria-expanded={mobileOpen}
          aria-label="Open navigation"
          className="p-1 -ml-1 text-text-muted hover:text-text"
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
        <Link
          href="/"
          className="flex items-center gap-2 no-underline font-bold text-text"
          style={{ fontFamily: "'Sora',sans-serif" }}
        >
          <span
            className="w-7 h-7 rounded-lg flex items-center justify-center text-white text-xs font-bold"
            style={{
              background: 'linear-gradient(135deg,var(--color-primary),var(--color-accent))',
            }}
          >
            S
          </span>
          <span className="text-sm">SimpleModule</span>
        </Link>
      </div>

      <aside
        className={`app-sidebar ${collapsed ? 'app-sidebar-collapsed' : ''} ${mobileOpen ? 'app-sidebar-open' : ''}`}
      >
        <div className="flex flex-col h-full overflow-visible">
          {/* Logo */}
          <div className="px-4 py-4 border-b border-border">
            <Link
              href="/"
              className="flex items-center gap-2.5 no-underline font-bold text-text group"
              style={{ fontFamily: "'Sora',sans-serif" }}
            >
              <span
                className="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-bold shadow-md transition-transform duration-200 group-hover:scale-105 shrink-0"
                style={{
                  background: 'linear-gradient(135deg,var(--color-primary),var(--color-accent))',
                }}
              >
                S
              </span>
              <span className="text-base sidebar-label">SimpleModule</span>
            </Link>
          </div>

          {/* Navigation */}
          <nav className="flex-1 overflow-y-auto px-3 py-4 space-y-1">
            {menus.sidebar.map((item) => (
              <NavLink key={item.url} item={item} pathname={pathname} onClick={closeMobile} />
            ))}

            {isAdmin && menus.adminSidebar.length > 0 && (
              <AdminSection
                adminItems={menus.adminSidebar}
                pathname={pathname}
                onLinkClick={closeMobile}
              />
            )}
          </nav>

          {/* Footer: Dark Mode + User */}
          <div className="px-3 py-3 border-t border-border space-y-2 overflow-visible">
            <div className="flex items-center gap-2 px-3">
              <DarkModeToggle />
              <span className="text-xs text-text-muted sidebar-label">Toggle theme</span>
            </div>
            <UserDropdown
              displayName={displayName}
              userInitial={userInitial}
              items={menus.userDropdown}
              csrfToken={csrfToken}
            />
          </div>
        </div>
      </aside>

      {/* Mobile backdrop */}
      <div
        className={`app-sidebar-backdrop ${mobileOpen ? 'visible' : ''}`}
        aria-hidden="true"
        onClick={closeMobile}
        onKeyDown={(e) => {
          if (e.key === 'Escape') closeMobile();
        }}
      />

      {/* Sidebar collapse toggle */}
      <button
        type="button"
        className="app-sidebar-toggle"
        onClick={toggleCollapse}
        title="Toggle sidebar"
      >
        <svg
          aria-hidden="true"
          className="w-4 h-4"
          fill="none"
          stroke="currentColor"
          strokeWidth={2}
          viewBox="0 0 24 24"
        >
          <path d="M11 19l-7-7 7-7m8 14l-7-7 7-7" />
        </svg>
      </button>

      <main className="app-content">
        <div className="mt-8 mb-16">{children}</div>
      </main>
    </div>
  );
}
