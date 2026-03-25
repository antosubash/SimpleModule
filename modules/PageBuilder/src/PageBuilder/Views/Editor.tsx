import { router } from '@inertiajs/react';
import { Puck, usePuck } from '@measured/puck';
import {
  Button,
  Card,
  CardContent,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Field,
  Input,
  Label,
} from '@simplemodule/ui';
import { useCallback, useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { puckConfig } from '../puck/config';
import { loadPuckCss } from '../puck/load-css';
import type { Page, PageTemplate } from '../types';

interface Props {
  page: Page | null;
  templates?: PageTemplate[];
}

function HeaderActions({ page }: { page: Page | null }) {
  const { appState } = usePuck();
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [showSaveTemplate, setShowSaveTemplate] = useState(false);
  const [templateName, setTemplateName] = useState('');

  async function handleSaveDraft() {
    setSaving(true);
    try {
      const content = JSON.stringify(appState.data);

      if (page) {
        await fetch(`/api/pagebuilder/${page.id}/content`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ content }),
        });
      } else {
        const title = appState.data.root?.props?.title || 'Untitled Page';
        const createRes = await fetch('/api/pagebuilder', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ title }),
        });
        const created = await createRes.json();
        await fetch(`/api/pagebuilder/${created.id}/content`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ content }),
        });
      }

      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    } catch (err) {
      console.error('Failed to save draft:', err);
    } finally {
      setSaving(false);
    }
  }

  async function handleSaveAsTemplate() {
    if (!templateName.trim()) return;
    const content = JSON.stringify(appState.data);
    await fetch('/api/pagebuilder/templates', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: templateName.trim(), content }),
    });
    setShowSaveTemplate(false);
    setTemplateName('');
  }

  return (
    <>
      <Button variant="outline" size="sm" onClick={() => setShowSaveTemplate(true)}>
        Save as Template
      </Button>
      <Button variant="outline" size="sm" onClick={handleSaveDraft} disabled={saving}>
        {saving ? 'Saving...' : saved ? 'Saved!' : 'Save Draft'}
      </Button>
      <Dialog open={showSaveTemplate} onOpenChange={setShowSaveTemplate}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Save as Template</DialogTitle>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <Field>
              <Label htmlFor="template-name">Template name</Label>
              <Input
                id="template-name"
                placeholder="e.g. Landing Page"
                value={templateName}
                onChange={(e) => setTemplateName(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleSaveAsTemplate();
                }}
                autoFocus
              />
            </Field>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowSaveTemplate(false)}>
              Cancel
            </Button>
            <Button onClick={handleSaveAsTemplate}>Save</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

export default function Editor({ page, templates }: Props) {
  const [showTemplatePicker, setShowTemplatePicker] = useState(!page && !!templates?.length);

  useEffect(() => loadPuckCss(), []);

  // Hide the Blazor shell (sidebar + toggle) so the editor has full screen
  useEffect(() => {
    const sidebar = document.getElementById('app-sidebar');
    const toggle = document.querySelector('.app-sidebar-toggle') as HTMLElement;
    const content = document.querySelector('.app-content') as HTMLElement;
    if (sidebar) sidebar.style.display = 'none';
    if (toggle) toggle.style.display = 'none';
    if (content) content.style.marginLeft = '0';
    return () => {
      if (sidebar) sidebar.style.display = '';
      if (toggle) toggle.style.display = '';
      if (content) content.style.marginLeft = '';
    };
  }, []);

  const [initialData, setInitialData] = useState(() => {
    if (page?.content) {
      try {
        return JSON.parse(page.content);
      } catch {
        return {};
      }
    }
    return {};
  });

  function handleSelectTemplate(template: PageTemplate) {
    try {
      setInitialData(JSON.parse(template.content));
    } catch {
      setInitialData({});
    }
    setShowTemplatePicker(false);
  }

  function handleStartBlank() {
    setInitialData({});
    setShowTemplatePicker(false);
  }

  const handlePublish = useCallback(
    async (data: any) => {
      const content = JSON.stringify(data);

      if (page) {
        await fetch(`/api/pagebuilder/${page.id}/content`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ content }),
        });
      } else {
        const title = data.root?.props?.title || 'Untitled Page';
        const createRes = await fetch('/api/pagebuilder', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ title }),
        });
        const created = await createRes.json();

        await fetch(`/api/pagebuilder/${created.id}/content`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ content }),
        });
      }

      router.visit('/admin/pages');
    },
    [page],
  );

  if (showTemplatePicker) {
    return (
      <Dialog
        open
        onOpenChange={(open) => {
          if (!open) router.visit('/admin/pages');
        }}
      >
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Create New Page</DialogTitle>
          </DialogHeader>
          <p className="text-text-muted text-sm">Start from a template or create a blank page.</p>
          <div className="grid grid-cols-2 gap-3 py-4">
            <button
              type="button"
              onClick={handleStartBlank}
              className="p-6 border-2 border-dashed border-border rounded-lg cursor-pointer bg-transparent text-center text-text-muted text-sm hover:border-primary/50 transition-colors"
            >
              <div className="text-3xl mb-2">+</div>
              Blank Page
            </button>
            {templates?.map((t) => (
              <Card
                key={t.id}
                className="cursor-pointer hover:border-primary/50 transition-colors"
                onClick={() => handleSelectTemplate(t)}
              >
                <CardContent className="p-6 text-center">
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                    className="mx-auto mb-2"
                    aria-hidden="true"
                  >
                    <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6z" />
                    <path d="M14 2v6h6M16 13H8M16 17H8M10 9H8" />
                  </svg>
                  <span className="text-sm font-medium">{t.name}</span>
                </CardContent>
              </Card>
            ))}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => router.visit('/admin/pages')}>
              Cancel
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );
  }

  const editor = (
    <div className="fixed inset-0 z-[9999] bg-white" data-testid="puck-editor">
      <Puck
        key={JSON.stringify(initialData)}
        config={puckConfig}
        data={initialData}
        onPublish={handlePublish}
        overrides={{
          headerActions: ({ children }) => (
            <>
              <Button
                variant="outline"
                size="sm"
                onClick={() => router.visit('/admin/pages')}
                className="gap-1"
              >
                <svg
                  width="16"
                  height="16"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path d="M15 19l-7-7 7-7" />
                </svg>
                Back to Pages
              </Button>
              <HeaderActions page={page} />
              {children}
            </>
          ),
        }}
      />
    </div>
  );

  return createPortal(editor, document.body);
}
