import {
  Button,
  Card,
  CardContent,
  Input,
  Label,
  Separator,
  Switch,
  Textarea,
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

interface AvailablePage {
  pageRoute: string;
  viewPrefix: string;
  module: string;
}

interface MenuItemEditorProps {
  item: MenuItemDto;
  availablePages: AvailablePage[];
  onSave: (id: number, data: Record<string, unknown>) => Promise<void>;
  onDelete: (id: number) => Promise<void>;
}

export default function MenuItemEditor({
  item,
  availablePages,
  onSave,
  onDelete,
}: MenuItemEditorProps) {
  const [label, setLabel] = useState(item.label);
  const [linkType, setLinkType] = useState<'page' | 'url'>(item.pageRoute ? 'page' : 'url');
  const [pageRoute, setPageRoute] = useState(item.pageRoute ?? '');
  const [url, setUrl] = useState(item.url ?? '');
  const [icon, setIcon] = useState(item.icon);
  const [cssClass, setCssClass] = useState(item.cssClass ?? '');
  const [openInNewTab, setOpenInNewTab] = useState(item.openInNewTab);
  const [isVisible, setIsVisible] = useState(item.isVisible);
  const [isHomePage, setIsHomePage] = useState(item.isHomePage);
  const [saving, setSaving] = useState(false);

  const pagesByModule: Record<string, AvailablePage[]> = {};
  for (const page of availablePages) {
    if (!pagesByModule[page.module]) pagesByModule[page.module] = [];
    pagesByModule[page.module].push(page);
  }

  const handleSave = async () => {
    setSaving(true);
    try {
      await onSave(item.id, {
        label,
        url: linkType === 'url' ? url || null : null,
        pageRoute: linkType === 'page' ? pageRoute || null : null,
        icon,
        cssClass: cssClass || null,
        openInNewTab,
        isVisible,
        isHomePage,
      });
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = () => {
    if (window.confirm(`Delete "${item.label}"? This will also delete any children.`)) {
      onDelete(item.id);
    }
  };

  return (
    <div className="space-y-5">
      <div className="space-y-2">
        <Label htmlFor="label">Label</Label>
        <Input id="label" value={label} onChange={(e) => setLabel(e.target.value)} />
      </div>

      <div className="space-y-2">
        <Label>Link Type</Label>
        <div className="flex gap-4">
          <label className="flex items-center gap-2 text-sm">
            <input
              type="radio"
              name="linkType"
              value="page"
              checked={linkType === 'page'}
              onChange={() => setLinkType('page')}
              className="accent-primary"
            />
            Page
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="radio"
              name="linkType"
              value="url"
              checked={linkType === 'url'}
              onChange={() => setLinkType('url')}
              className="accent-primary"
            />
            URL
          </label>
        </div>
      </div>

      {linkType === 'page' ? (
        <div className="space-y-2">
          <Label htmlFor="pageRoute">Page</Label>
          <select
            id="pageRoute"
            value={pageRoute}
            onChange={(e) => setPageRoute(e.target.value)}
            className="w-full rounded-xl border border-border bg-surface px-4 py-3 text-sm text-text transition-all outline-none focus:border-primary focus:ring-4 focus:ring-primary-ring"
          >
            <option value="">-- Select a page --</option>
            {Object.entries(pagesByModule).map(([mod, pages]) => (
              <optgroup key={mod} label={mod}>
                {pages.map((p) => (
                  <option key={p.pageRoute} value={p.viewPrefix}>
                    {p.pageRoute}
                  </option>
                ))}
              </optgroup>
            ))}
          </select>
        </div>
      ) : (
        <div className="space-y-2">
          <Label htmlFor="url">URL</Label>
          <Input
            id="url"
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            placeholder="https://example.com or /path"
          />
        </div>
      )}

      <div className="space-y-2">
        <Label htmlFor="icon">Icon (SVG)</Label>
        <Textarea
          id="icon"
          value={icon}
          onChange={(e) => setIcon(e.target.value)}
          rows={3}
          className="font-mono text-xs"
          placeholder='<svg class="w-4 h-4" ...>...</svg>'
        />
        {icon && (
          <Card className="overflow-hidden">
            <CardContent className="flex items-center gap-2 p-3">
              <span className="text-xs text-text-secondary">Preview:</span>
              <iframe
                title="Icon preview"
                srcDoc={`<!DOCTYPE html><html><body style="margin:0;display:flex;align-items:center;justify-content:center;height:24px;width:24px">${icon}</body></html>`}
                className="h-6 w-6 border-0"
                sandbox=""
              />
            </CardContent>
          </Card>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="cssClass">CSS Class</Label>
        <Input
          id="cssClass"
          value={cssClass}
          onChange={(e) => setCssClass(e.target.value)}
          placeholder="Optional CSS class"
        />
      </div>

      <Separator />

      <div className="flex items-center justify-between">
        <Label htmlFor="openInNewTab">Open in new tab</Label>
        <Switch id="openInNewTab" checked={openInNewTab} onCheckedChange={setOpenInNewTab} />
      </div>

      <div className="flex items-center justify-between">
        <Label htmlFor="isVisible">Visible</Label>
        <Switch id="isVisible" checked={isVisible} onCheckedChange={setIsVisible} />
      </div>

      <div className="flex items-center justify-between">
        <Label htmlFor="isHomePage">Home page</Label>
        <Switch id="isHomePage" checked={isHomePage} onCheckedChange={setIsHomePage} />
      </div>

      <Separator />

      <div className="flex gap-2">
        <Button variant="primary" onClick={handleSave} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </Button>
        <Button variant="danger" onClick={handleDelete} disabled={saving}>
          Delete
        </Button>
      </div>
    </div>
  );
}
