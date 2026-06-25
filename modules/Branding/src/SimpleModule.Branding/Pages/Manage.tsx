import { router, usePage } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Field,
  FieldGroup,
  Input,
  Label,
  PageShell,
  Switch,
  Textarea,
} from '@simplemodule/ui';
import * as React from 'react';

interface BrandingLink {
  label: string;
  url: string;
}
interface TopBarConfig {
  enabled: boolean;
  message: string;
  backgroundColor: string;
  textColor: string;
  links: BrandingLink[];
  dismissible: boolean;
}
interface FooterConfig {
  enabled: boolean;
  text: string;
  links: BrandingLink[];
  showCopyright: boolean;
}
interface BrandingEditModel {
  appName: string;
  logoFileId: string | null;
  logoUrl: string | null;
  faviconFileId: string | null;
  faviconUrl: string | null;
  colorPrimary: string;
  colorPrimaryDark: string;
  customCss: string;
  topBar: TopBarConfig;
  footer: FooterConfig;
}

export default function Manage() {
  const { branding } = usePage<{ branding: BrandingEditModel }>().props;
  const [model, setModel] = React.useState<BrandingEditModel>(branding);
  const [saving, setSaving] = React.useState(false);
  const [saved, setSaved] = React.useState(false);

  const set = <K extends keyof BrandingEditModel>(key: K, value: BrandingEditModel[K]) =>
    setModel((m) => ({ ...m, [key]: value }));

  const setTopBar = <K extends keyof TopBarConfig>(key: K, value: TopBarConfig[K]) =>
    setModel((m) => ({ ...m, topBar: { ...m.topBar, [key]: value } }));

  const setFooter = <K extends keyof FooterConfig>(key: K, value: FooterConfig[K]) =>
    setModel((m) => ({ ...m, footer: { ...m.footer, [key]: value } }));

  async function uploadAsset(kind: 'logo' | 'favicon', file: File) {
    const data = new FormData();
    data.append('file', file);
    const res = await fetch(`/api/branding/assets/${kind}`, { method: 'POST', body: data });
    if (!res.ok) return;
    const json = (await res.json()) as { fileId: string; url: string };
    if (kind === 'logo') {
      set('logoFileId', json.fileId);
      set('logoUrl', json.url);
    } else {
      set('faviconFileId', json.fileId);
      set('faviconUrl', json.url);
    }
  }

  async function save() {
    setSaving(true);
    try {
      const res = await fetch('/api/branding', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(model),
      });
      if (res.ok) {
        setSaved(true);
        setTimeout(() => setSaved(false), 2000);
        router.reload();
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <PageShell title="Branding" description="Customize the appearance of your application.">
      <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Identity</CardTitle>
            </CardHeader>
            <CardContent>
              <FieldGroup>
                <Field>
                  <Label htmlFor="appName">Application name</Label>
                  <Input
                    id="appName"
                    value={model.appName}
                    onChange={(e) => set('appName', e.target.value)}
                  />
                </Field>
                <Field>
                  <Label htmlFor="logo">Logo</Label>
                  {model.logoUrl && (
                    <img src={model.logoUrl} alt="Logo preview" className="h-10 w-auto mb-2" />
                  )}
                  <input
                    id="logo"
                    type="file"
                    accept="image/*"
                    onChange={(e) => {
                      const f = e.target.files?.[0];
                      if (f) void uploadAsset('logo', f);
                    }}
                  />
                  {model.logoFileId && (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => {
                        set('logoFileId', '');
                        set('logoUrl', null);
                      }}
                    >
                      Remove logo
                    </Button>
                  )}
                </Field>
                <Field>
                  <Label htmlFor="favicon">Favicon</Label>
                  {model.faviconUrl && (
                    <img src={model.faviconUrl} alt="Favicon preview" className="h-6 w-6 mb-2" />
                  )}
                  <input
                    id="favicon"
                    type="file"
                    accept="image/*"
                    onChange={(e) => {
                      const f = e.target.files?.[0];
                      if (f) void uploadAsset('favicon', f);
                    }}
                  />
                </Field>
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Colors</CardTitle>
            </CardHeader>
            <CardContent>
              <FieldGroup>
                <Field>
                  <Label htmlFor="primary">Primary color (light)</Label>
                  <input
                    id="primary"
                    type="color"
                    value={model.colorPrimary}
                    onChange={(e) => set('colorPrimary', e.target.value)}
                    className="h-9 w-16 cursor-pointer rounded border-0 bg-transparent p-0"
                  />
                </Field>
                <Field>
                  <Label htmlFor="primaryDark">Primary color (dark)</Label>
                  <input
                    id="primaryDark"
                    type="color"
                    value={model.colorPrimaryDark}
                    onChange={(e) => set('colorPrimaryDark', e.target.value)}
                    className="h-9 w-16 cursor-pointer rounded border-0 bg-transparent p-0"
                  />
                </Field>
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Top bar</CardTitle>
            </CardHeader>
            <CardContent>
              <FieldGroup>
                <Field className="flex items-center gap-3">
                  <Switch
                    id="topbar-enabled"
                    checked={model.topBar.enabled}
                    onCheckedChange={(v) => setTopBar('enabled', v)}
                  />
                  <Label htmlFor="topbar-enabled">Show top bar</Label>
                </Field>
                <Field>
                  <Label htmlFor="topbar-message">Message</Label>
                  <Input
                    id="topbar-message"
                    value={model.topBar.message}
                    onChange={(e) => setTopBar('message', e.target.value)}
                  />
                </Field>
                <Field>
                  <Label htmlFor="topbar-bg">Background color</Label>
                  <input
                    id="topbar-bg"
                    type="color"
                    value={model.topBar.backgroundColor}
                    onChange={(e) => setTopBar('backgroundColor', e.target.value)}
                    className="h-9 w-16 cursor-pointer rounded border-0 bg-transparent p-0"
                  />
                </Field>
                <Field className="flex items-center gap-3">
                  <Switch
                    id="topbar-dismissible"
                    checked={model.topBar.dismissible}
                    onCheckedChange={(v) => setTopBar('dismissible', v)}
                  />
                  <Label htmlFor="topbar-dismissible">Dismissible</Label>
                </Field>
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Footer</CardTitle>
            </CardHeader>
            <CardContent>
              <FieldGroup>
                <Field className="flex items-center gap-3">
                  <Switch
                    id="footer-enabled"
                    checked={model.footer.enabled}
                    onCheckedChange={(v) => setFooter('enabled', v)}
                  />
                  <Label htmlFor="footer-enabled">Show footer</Label>
                </Field>
                <Field>
                  <Label htmlFor="footer-text">Footer text</Label>
                  <Input
                    id="footer-text"
                    value={model.footer.text}
                    onChange={(e) => setFooter('text', e.target.value)}
                  />
                </Field>
                <Field className="flex items-center gap-3">
                  <Switch
                    id="footer-copyright"
                    checked={model.footer.showCopyright}
                    onCheckedChange={(v) => setFooter('showCopyright', v)}
                  />
                  <Label htmlFor="footer-copyright">Show copyright</Label>
                </Field>
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Advanced — Custom CSS</CardTitle>
            </CardHeader>
            <CardContent>
              <Textarea
                rows={8}
                value={model.customCss}
                placeholder=":root { --color-primary: #059669; }"
                onChange={(e) => set('customCss', e.target.value)}
                className="font-mono text-sm"
              />
              <p className="text-xs text-text-muted mt-2">
                Injected globally into the page head. Use with care — invalid CSS can break the UI.
              </p>
            </CardContent>
          </Card>

          <div className="flex items-center gap-3">
            <Button onClick={() => void save()} disabled={saving}>
              {saving ? 'Saving…' : 'Save changes'}
            </Button>
            {saved && <span className="text-sm text-green-600">Saved ✓</span>}
          </div>
        </div>

        {/* Live preview */}
        <div className="space-y-3">
          <p className="text-sm font-semibold text-text-muted">Preview</p>
          <div className="rounded-xl border border-border overflow-hidden">
            {model.topBar.enabled && model.topBar.message && (
              <div
                className="px-3 py-2 text-xs text-center"
                style={{ background: model.topBar.backgroundColor, color: model.topBar.textColor }}
              >
                {model.topBar.message}
              </div>
            )}
            <div className="flex items-center gap-2 p-3 border-b border-border">
              {model.logoUrl ? (
                <img src={model.logoUrl} alt="" className="h-7 w-auto" />
              ) : (
                <span
                  className="w-7 h-7 rounded-lg flex items-center justify-center text-white text-xs font-bold"
                  style={{ background: model.colorPrimary }}
                >
                  {(model.appName || 'S').charAt(0).toUpperCase()}
                </span>
              )}
              <span className="text-sm font-bold">{model.appName || 'SimpleModule'}</span>
            </div>
            <div className="p-3">
              <button
                type="button"
                className="rounded-md px-3 py-1.5 text-xs font-medium text-white"
                style={{ background: model.colorPrimary }}
              >
                Primary button
              </button>
            </div>
            {model.footer.enabled && (
              <div className="border-t border-border px-3 py-2 text-xs text-text-muted">
                {model.footer.showCopyright && `© ${new Date().getFullYear()} ${model.appName}. `}
                {model.footer.text}
              </div>
            )}
          </div>
        </div>
      </div>
    </PageShell>
  );
}
