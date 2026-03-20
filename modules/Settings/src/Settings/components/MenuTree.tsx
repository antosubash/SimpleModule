import {
  Badge,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@simplemodule/ui';
import { useState } from 'react';

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

function MenuTreeNode({
  item,
  selectedId,
  onSelect,
  onToggleVisibility,
  level,
}: {
  item: MenuItemDto;
  selectedId: number | null;
  onSelect: (id: number) => void;
  onToggleVisibility: (item: MenuItemDto) => void;
  level: number;
}) {
  const [open, setOpen] = useState(true);
  const hasChildren = item.children.length > 0;
  const isSelected = selectedId === item.id;

  const nodeContent = (
    <button
      type="button"
      className={`flex w-full items-center gap-2 rounded-lg px-2.5 py-2 text-sm transition-colors text-left group ${
        isSelected
          ? 'bg-primary-subtle text-primary font-medium border-l-2 border-primary'
          : 'hover:bg-surface-raised text-text border-l-2 border-transparent'
      } ${!item.isVisible ? 'opacity-50' : ''}`}
      style={{ paddingLeft: `${level * 16 + 10}px` }}
      onClick={() => onSelect(item.id)}
    >
      {hasChildren && (
        <CollapsibleTrigger
          asChild
          onClick={(e: React.MouseEvent) => {
            e.stopPropagation();
            setOpen(!open);
          }}
        >
          <span className="flex h-4 w-4 shrink-0 items-center justify-center rounded transition-colors hover:bg-surface-raised">
            <svg
              className={`h-3 w-3 text-text-secondary transition-transform duration-200 ${
                open ? 'rotate-90' : ''
              }`}
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path d="m9 18 6-6-6-6" />
            </svg>
          </span>
        </CollapsibleTrigger>
      )}

      {!hasChildren && <span className="w-4 shrink-0" />}

      {item.pageRoute ? (
        <svg
          className="h-3.5 w-3.5 shrink-0 text-text-secondary"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <title>Page link</title>
          <path d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z" />
        </svg>
      ) : item.url ? (
        <svg
          className="h-3.5 w-3.5 shrink-0 text-text-secondary"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <title>External URL</title>
          <path d="M13.19 8.688a4.5 4.5 0 0 1 1.242 7.244l-4.5 4.5a4.5 4.5 0 0 1-6.364-6.364l1.757-1.757m9.86-2.54a4.5 4.5 0 0 0-1.242-7.244l4.5-4.5a4.5 4.5 0 0 1 6.364 6.364l-1.757 1.757" />
        </svg>
      ) : null}

      <span className="flex-1 truncate">{item.label}</span>

      {item.isHomePage && <Badge variant="info">Home</Badge>}

      <Tooltip>
        <TooltipTrigger asChild>
          <span
            role="switch"
            aria-checked={item.isVisible}
            aria-label={item.isVisible ? 'Hide item' : 'Show item'}
            tabIndex={0}
            className="shrink-0 rounded p-1 text-text-secondary opacity-0 transition-all hover:bg-surface-raised hover:text-text group-hover:opacity-100"
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
        </TooltipTrigger>
        <TooltipContent>{item.isVisible ? 'Hide from menu' : 'Show in menu'}</TooltipContent>
      </Tooltip>
    </button>
  );

  if (!hasChildren) {
    return <li>{nodeContent}</li>;
  }

  return (
    <li>
      <Collapsible open={open} onOpenChange={setOpen}>
        {nodeContent}
        <CollapsibleContent>
          <MenuTree
            items={item.children}
            selectedId={selectedId}
            onSelect={onSelect}
            onToggleVisibility={onToggleVisibility}
            level={level + 1}
          />
        </CollapsibleContent>
      </Collapsible>
    </li>
  );
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
        <MenuTreeNode
          key={item.id}
          item={item}
          selectedId={selectedId}
          onSelect={onSelect}
          onToggleVisibility={onToggleVisibility}
          level={level}
        />
      ))}
    </ul>
  );
}
