import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  PageShell,
  ScrollArea,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@simplemodule/ui';
import { useCallback, useState } from 'react';
import MenuItemEditor from '@/components/MenuItemEditor';
import MenuTree from '@/components/MenuTree';
import { SettingsKeys } from '@/Locales/keys';

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

interface AvailablePage {
  pageRoute: string;
  viewPrefix: string;
  module: string;
}

interface MenuManagerProps {
  menuItems: MenuItemDto[];
  availablePages: AvailablePage[];
}

function findItem(items: MenuItemDto[], id: number): MenuItemDto | null {
  for (const item of items) {
    if (item.id === id) return item;
    const found = findItem(item.children, id);
    if (found) return found;
  }
  return null;
}

function getDepth(items: MenuItemDto[], id: number, depth = 0): number {
  for (const item of items) {
    if (item.id === id) return depth;
    const found = getDepth(item.children, id, depth + 1);
    if (found >= 0) return found;
  }
  return -1;
}

function countItems(items: MenuItemDto[]): number {
  let count = 0;
  for (const item of items) {
    count += 1 + countItems(item.children);
  }
  return count;
}

export default function MenuManager({ menuItems: initial, availablePages }: MenuManagerProps) {
  const { t } = useTranslation('Settings');
  const [menus, setMenus] = useState(initial);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);

  const selectedItem = selectedId !== null ? findItem(menus, selectedId) : null;
  const selectedDepth = selectedId !== null ? getDepth(menus, selectedId) : -1;
  const totalItems = countItems(menus);

  const refreshMenus = useCallback(async () => {
    const res = await fetch('/api/settings/menus');
    if (res.ok) {
      const data = await res.json();
      setMenus(data);
    }
  }, []);

  const handleAddItem = async (parentId: number | null) => {
    setSaving(true);
    try {
      const res = await fetch('/api/settings/menus', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          parentId,
          label: 'New Item',
          icon: '',
          isVisible: true,
        }),
      });
      if (res.ok) {
        const created = await res.json();
        await refreshMenus();
        setSelectedId(created.id);
      }
    } finally {
      setSaving(false);
    }
  };

  const handleSave = async (id: number, data: Record<string, unknown>) => {
    setSaving(true);
    try {
      await fetch(`/api/settings/menus/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });
      await refreshMenus();
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    setSaving(true);
    try {
      await fetch(`/api/settings/menus/${id}`, { method: 'DELETE' });
      if (selectedId === id) setSelectedId(null);
      await refreshMenus();
    } finally {
      setSaving(false);
    }
  };

  const handleToggleVisibility = async (item: MenuItemDto) => {
    await handleSave(item.id, {
      label: item.label,
      url: item.url,
      pageRoute: item.pageRoute,
      icon: item.icon,
      cssClass: item.cssClass,
      openInNewTab: item.openInNewTab,
      isVisible: !item.isVisible,
      isHomePage: item.isHomePage,
    });
  };

  return (
    <TooltipProvider>
      <PageShell
        title={t(SettingsKeys.MenuManager.Title)}
        description={t(SettingsKeys.MenuManager.Description)}
        breadcrumbs={[
          { label: t(SettingsKeys.MenuManager.BreadcrumbSettings), href: '/admin/settings' },
          { label: t(SettingsKeys.MenuManager.BreadcrumbMenuManager) },
        ]}
      >
        <div className="grid grid-cols-1 gap-4 md:gap-6 md:grid-cols-[2fr_3fr]">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 p-4 sm:p-6">
              <div className="flex items-center gap-2">
                <CardTitle className="text-base">
                  {t(SettingsKeys.MenuManager.CardTreeTitle)}
                </CardTitle>
                {totalItems > 0 && (
                  <Badge>{t(SettingsKeys.MenuManager.ItemsCount, { count: totalItems })}</Badge>
                )}
              </div>
              <div className="flex gap-1.5">
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => handleAddItem(null)}
                      disabled={saving}
                    >
                      <svg
                        className="mr-1.5 h-3.5 w-3.5"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        viewBox="0 0 24 24"
                        aria-hidden="true"
                      >
                        <path d="M12 5v14m-7-7h14" />
                      </svg>
                      {t(SettingsKeys.MenuManager.AddButton)}
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>{t(SettingsKeys.MenuManager.AddTooltip)}</TooltipContent>
                </Tooltip>
                {selectedItem && selectedDepth < 2 && (
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleAddItem(selectedId)}
                        disabled={saving}
                      >
                        <svg
                          className="mr-1.5 h-3.5 w-3.5"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2"
                          viewBox="0 0 24 24"
                          aria-hidden="true"
                        >
                          <path d="M12 5v14m-7-7h14" />
                        </svg>
                        {t(SettingsKeys.MenuManager.AddChildButton)}
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>
                      {t(SettingsKeys.MenuManager.AddChildTooltip, { label: selectedItem.label })}
                    </TooltipContent>
                  </Tooltip>
                )}
              </div>
            </CardHeader>
            <CardContent className="p-0">
              {menus.length === 0 ? (
                <div className="flex flex-col items-center justify-center px-6 py-12 text-center">
                  <svg
                    className="mb-3 h-10 w-10 text-text-secondary opacity-40"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.5"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
                  </svg>
                  <p className="text-sm font-medium text-text-secondary">
                    {t(SettingsKeys.MenuManager.EmptyTitle)}
                  </p>
                  <p className="mt-1 text-xs text-text-secondary">
                    {t(SettingsKeys.MenuManager.EmptyDescription)}
                  </p>
                </div>
              ) : (
                <ScrollArea className="max-h-[500px]">
                  <div className="px-3 pb-3">
                    <MenuTree
                      items={menus}
                      selectedId={selectedId}
                      onSelect={setSelectedId}
                      onToggleVisibility={handleToggleVisibility}
                    />
                  </div>
                </ScrollArea>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="p-4 sm:p-6">
              <CardTitle className="text-base">
                {selectedItem
                  ? t(SettingsKeys.MenuManager.EditorEditTitle, { label: selectedItem.label })
                  : t(SettingsKeys.MenuManager.EditorTitle)}
              </CardTitle>
            </CardHeader>
            <CardContent className="p-4 sm:p-6">
              {selectedItem ? (
                <MenuItemEditor
                  key={selectedItem.id}
                  item={selectedItem}
                  availablePages={availablePages}
                  onSave={handleSave}
                  onDelete={handleDelete}
                />
              ) : (
                <div className="flex flex-col items-center justify-center py-12 text-center">
                  <svg
                    className="mb-3 h-10 w-10 text-text-secondary opacity-40"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.5"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0 1 15.75 21H5.25A2.25 2.25 0 0 1 3 18.75V8.25A2.25 2.25 0 0 1 5.25 6H10" />
                  </svg>
                  <p className="text-sm font-medium text-text-secondary">
                    {t(SettingsKeys.MenuManager.NoItemSelectedTitle)}
                  </p>
                  <p className="mt-1 text-xs text-text-secondary">
                    {t(SettingsKeys.MenuManager.NoItemSelectedDescription)}
                  </p>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </PageShell>
    </TooltipProvider>
  );
}
