interface MenuItemDto {
  id: number;
  parentId: number | null;
  label: string;
  url: string | null;
  pageRoute: string | null;
  icon: string;
  cssClass: string | null;
  openInNewTab: boolean;
  isVisible: boolean;
  isHomePage: boolean;
  sortOrder: number;
  children: MenuItemDto[];
}

interface MenuTreeProps {
  items: MenuItemDto[];
  selectedId: number | null;
  onSelect: (id: number) => void;
  onToggleVisibility: (item: MenuItemDto) => void;
  level?: number;
}

export default function MenuTree({
  items,
  selectedId,
  onSelect,
  onToggleVisibility,
  level = 0,
}: MenuTreeProps) {
  return (
    <ul className="space-y-0.5">
      {items.map((item) => (
        <li key={item.id}>
          <button
            type="button"
            className={`flex w-full cursor-pointer items-center gap-2 rounded-lg px-2 py-1.5 text-sm transition-colors text-left ${
              selectedId === item.id
                ? 'bg-primary-subtle text-primary font-medium'
                : 'hover:bg-surface-raised text-text'
            } ${!item.isVisible ? 'opacity-50' : ''}`}
            style={{ paddingLeft: `${level * 20 + 8}px` }}
            onClick={() => onSelect(item.id)}
          >
            {item.icon && (
              <span className="flex h-4 w-4 shrink-0 items-center justify-center text-xs">*</span>
            )}
            <span className="flex-1 truncate">{item.label}</span>
            {item.isHomePage && (
              <span className="rounded bg-primary/10 px-1.5 py-0.5 text-xs font-medium text-primary">
                Home
              </span>
            )}
            <span
              role="switch"
              aria-checked={item.isVisible}
              aria-label={item.isVisible ? 'Hide' : 'Show'}
              tabIndex={0}
              className="shrink-0 rounded p-0.5 text-text-secondary hover:text-text transition-colors"
              onClick={(e) => {
                e.stopPropagation();
                onToggleVisibility(item);
              }}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  e.stopPropagation();
                  onToggleVisibility(item);
                }
              }}
            >
              {item.isVisible ? (
                <svg
                  className="h-3.5 w-3.5"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <title>Visible</title>
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                  <circle cx="12" cy="12" r="3" />
                </svg>
              ) : (
                <svg
                  className="h-3.5 w-3.5"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <title>Hidden</title>
                  <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
                  <line x1="1" y1="1" x2="23" y2="23" />
                </svg>
              )}
            </span>
          </button>
          {item.children.length > 0 && (
            <MenuTree
              items={item.children}
              selectedId={selectedId}
              onSelect={onSelect}
              onToggleVisibility={onToggleVisibility}
              level={level + 1}
            />
          )}
        </li>
      ))}
    </ul>
  );
}
