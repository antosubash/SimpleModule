import * as React from 'react';
import type { MenuItem } from './types';

interface UserDropdownProps {
  displayName: string;
  userInitial: string;
  items: MenuItem[];
  csrfToken: string;
}

export function UserDropdown({ displayName, userInitial, items, csrfToken }: UserDropdownProps) {
  const [open, setOpen] = React.useState(false);
  const wrapRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    function handleEscape(e: KeyboardEvent) {
      if (e.key === 'Escape') setOpen(false);
    }
    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('keydown', handleEscape);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleEscape);
    };
  }, []);

  // Group items and insert dividers between groups
  const groupedItems: React.ReactNode[] = [];
  let lastGroup: string | null = null;
  for (const item of items) {
    if (lastGroup !== null && item.group !== lastGroup) {
      groupedItems.push(<div key={`div-${item.url}`} className="user-dropdown-divider" />);
    }
    lastGroup = item.group;
    groupedItems.push(
      <a key={item.url} href={item.url} className="user-dropdown-item">
        {/* biome-ignore lint/security/noDangerouslySetInnerHtml: trusted server-provided SVG icon content */}
        <span dangerouslySetInnerHTML={{ __html: item.icon }} />
        {item.label}
      </a>,
    );
  }

  return (
    <div className="user-dropdown-wrap" ref={wrapRef}>
      <button
        type="button"
        className="user-dropdown-trigger"
        onClick={() => setOpen((o) => !o)}
        aria-expanded={open}
      >
        <span
          className="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white shadow-sm"
          style={{
            background: 'linear-gradient(135deg,var(--color-primary),var(--color-accent))',
          }}
        >
          {userInitial}
        </span>
        <span className="sidebar-label text-sm font-medium text-text max-w-[140px] truncate">
          {displayName}
        </span>
        <svg
          className={`sidebar-label w-4 h-4 text-text-muted transition-transform duration-200 ${open ? 'rotate-180' : ''}`}
          fill="none"
          stroke="currentColor"
          strokeWidth={2}
          viewBox="0 0 24 24"
        >
          <path d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      {open && (
        <div className="user-dropdown">
          <div className="user-dropdown-header">
            <div className="text-sm font-semibold text-text truncate">{displayName}</div>
          </div>
          <div className="user-dropdown-body">
            {groupedItems}
            <div className="user-dropdown-divider" />
            <form method="post" action="/Identity/Account/Logout">
              <input type="hidden" name="_handler" value="logout" />
              <input type="hidden" name="__RequestVerificationToken" value={csrfToken} />
              <button
                type="submit"
                className="user-dropdown-item danger w-full text-left bg-transparent border-none"
                style={{ fontFamily: 'inherit', fontSize: 'inherit' }}
              >
                <svg
                  className="w-4 h-4"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth={2}
                  viewBox="0 0 24 24"
                >
                  <path d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                </svg>
                Log out
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
