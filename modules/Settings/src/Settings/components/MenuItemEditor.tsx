import {
  Alert,
  AlertDescription,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  Input,
  Label,
  RadioGroup,
  RadioGroupItem,
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Separator,
  Switch,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
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

  return (
    <div className="space-y-4">
      <Tabs defaultValue="content">
        <TabsList>
          <TabsTrigger value="content">Content</TabsTrigger>
          <TabsTrigger value="settings">Settings</TabsTrigger>
        </TabsList>

        <TabsContent value="content" className="space-y-5 pt-2">
          <div className="space-y-2">
            <Label htmlFor="label">Label</Label>
            <Input id="label" value={label} onChange={(e) => setLabel(e.target.value)} />
          </div>

          <div className="space-y-3">
            <Label>Link Type</Label>
            <RadioGroup
              value={linkType}
              onValueChange={(v) => setLinkType(v as 'page' | 'url')}
              className="flex gap-4"
            >
              <div className="flex items-center gap-2">
                <RadioGroupItem value="page" id="linkType-page" />
                <Label htmlFor="linkType-page" className="font-normal cursor-pointer">
                  Page
                </Label>
              </div>
              <div className="flex items-center gap-2">
                <RadioGroupItem value="url" id="linkType-url" />
                <Label htmlFor="linkType-url" className="font-normal cursor-pointer">
                  URL
                </Label>
              </div>
            </RadioGroup>
          </div>

          {linkType === 'page' ? (
            <div className="space-y-2">
              <Label>Page</Label>
              <Select value={pageRoute} onValueChange={setPageRoute}>
                <SelectTrigger>
                  <SelectValue placeholder="Select a page..." />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(pagesByModule).map(([mod, pages]) => (
                    <SelectGroup key={mod}>
                      <Label className="px-2 py-1.5 text-xs font-semibold text-text-secondary">
                        {mod}
                      </Label>
                      {pages.map((p) => (
                        <SelectItem key={p.pageRoute} value={p.viewPrefix}>
                          {p.pageRoute}
                        </SelectItem>
                      ))}
                    </SelectGroup>
                  ))}
                </SelectContent>
              </Select>
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
              <Card>
                <CardContent className="flex items-center gap-3 p-3">
                  <span className="text-xs font-medium text-text-secondary">Preview:</span>
                  <div className="flex h-8 w-8 items-center justify-center rounded-lg border border-border bg-surface-raised">
                    <iframe
                      title="Icon preview"
                      srcDoc={`<!DOCTYPE html><html><body style="margin:0;display:flex;align-items:center;justify-content:center;height:24px;width:24px">${icon}</body></html>`}
                      className="h-6 w-6 border-0"
                      sandbox=""
                    />
                  </div>
                </CardContent>
              </Card>
            )}
          </div>
        </TabsContent>

        <TabsContent value="settings" className="space-y-5 pt-2">
          <div className="flex items-center justify-between">
            <div>
              <Label htmlFor="isVisible">Visible</Label>
              <p className="text-xs text-text-secondary">Show this item in the public menu</p>
            </div>
            <Switch id="isVisible" checked={isVisible} onCheckedChange={setIsVisible} />
          </div>

          <Separator />

          <div className="flex items-center justify-between">
            <div>
              <Label htmlFor="isHomePage">Home Page</Label>
              <p className="text-xs text-text-secondary">Display this page at the root URL</p>
            </div>
            <Switch id="isHomePage" checked={isHomePage} onCheckedChange={setIsHomePage} />
          </div>

          {isHomePage && (
            <Alert variant="info">
              <AlertDescription>
                This page will be displayed when visitors navigate to <strong>/</strong> (the site
                root).
              </AlertDescription>
            </Alert>
          )}

          <Separator />

          <div className="flex items-center justify-between">
            <div>
              <Label htmlFor="openInNewTab">Open in New Tab</Label>
              <p className="text-xs text-text-secondary">Open this link in a new browser tab</p>
            </div>
            <Switch id="openInNewTab" checked={openInNewTab} onCheckedChange={setOpenInNewTab} />
          </div>

          <Separator />

          <div className="space-y-2">
            <Label htmlFor="cssClass">CSS Class</Label>
            <Input
              id="cssClass"
              value={cssClass}
              onChange={(e) => setCssClass(e.target.value)}
              placeholder="Optional CSS class name"
            />
          </div>
        </TabsContent>
      </Tabs>

      <Separator />

      <div className="flex items-center justify-between">
        <Dialog>
          <DialogTrigger asChild>
            <Button variant="danger" size="sm" disabled={saving}>
              Delete
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Delete Menu Item</DialogTitle>
              <DialogDescription>
                Are you sure you want to delete &ldquo;{item.label}&rdquo;?
                {item.children.length > 0 &&
                  ` This will also delete ${item.children.length} child item${item.children.length > 1 ? 's' : ''}.`}{' '}
                This action cannot be undone.
              </DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <DialogClose asChild>
                <Button variant="secondary">Cancel</Button>
              </DialogClose>
              <Button variant="danger" onClick={() => onDelete(item.id)}>
                Delete
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        <Button variant="primary" onClick={handleSave} disabled={saving}>
          {saving ? 'Saving...' : 'Save Changes'}
        </Button>
      </div>
    </div>
  );
}
