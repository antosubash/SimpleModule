import { Link } from '@inertiajs/react';
import type * as React from 'react';
import type { PublicMenuItem } from './types';

export function MenuLink({
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

export function DesktopMenu({ items }: { items: PublicMenuItem[] }) {
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
