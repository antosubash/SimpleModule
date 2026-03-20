import { Button, Card, CardContent, CardHeader, CardTitle, Separator } from '@simplemodule/ui';
import { useCallback, useState } from 'react';
import MenuItemEditor from '../components/MenuItemEditor';
import MenuTree from '../components/MenuTree';

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
  menus: MenuItemDto[];
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

export default function MenuManager({ menus: initial, availablePages }: MenuManagerProps) {
  const [menus, setMenus] = useState(initial);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);

  const selectedItem = selectedId !== null ? findItem(menus, selectedId) : null;
  const selectedDepth = selectedId !== null ? getDepth(menus, selectedId) : -1;

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
    <div className="mx-auto max-w-5xl space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight">Menu Manager</h1>
        <div className="flex gap-2">
          <Button variant="primary" size="sm" onClick={() => handleAddItem(null)} disabled={saving}>
            Add Item
          </Button>
          {selectedItem && selectedDepth < 2 && (
            <Button
              variant="secondary"
              size="sm"
              onClick={() => handleAddItem(selectedId)}
              disabled={saving}
            >
              Add Child
            </Button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 md:grid-cols-[1fr_1.5fr]">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Menu Tree</CardTitle>
          </CardHeader>
          <CardContent>
            {menus.length === 0 ? (
              <p className="py-8 text-center text-sm text-muted-foreground">
                No menu items yet. Click "Add Item" to get started.
              </p>
            ) : (
              <MenuTree
                items={menus}
                selectedId={selectedId}
                onSelect={setSelectedId}
                onToggleVisibility={handleToggleVisibility}
              />
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">
              {selectedItem ? `Edit: ${selectedItem.label}` : 'Item Editor'}
            </CardTitle>
          </CardHeader>
          <Separator />
          <CardContent className="pt-4">
            {selectedItem ? (
              <MenuItemEditor
                key={selectedItem.id}
                item={selectedItem}
                availablePages={availablePages}
                onSave={handleSave}
                onDelete={handleDelete}
              />
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">
                Select a menu item to edit its properties.
              </p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
