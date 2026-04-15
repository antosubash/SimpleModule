import { usePuck } from '@puckeditor/core';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Field,
  Input,
  Label,
} from '@simplemodule/ui';
import { useState } from 'react';
import { PageBuilderKeys } from '@/Locales/keys';
import type { Page } from '@/types';

export function HeaderActions({ page }: { page: Page | null }) {
  const { t } = useTranslation('PageBuilder');
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
        {t(PageBuilderKeys.Editor.SaveAsTemplate)}
      </Button>
      <Button variant="outline" size="sm" onClick={handleSaveDraft} disabled={saving}>
        {saving
          ? t(PageBuilderKeys.Editor.Saving)
          : saved
            ? t(PageBuilderKeys.Editor.Saved)
            : t(PageBuilderKeys.Editor.SaveDraft)}
      </Button>
      <Dialog open={showSaveTemplate} onOpenChange={setShowSaveTemplate}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t(PageBuilderKeys.Editor.SaveTemplateDialog.Title)}</DialogTitle>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <Field>
              <Label htmlFor="template-name">
                {t(PageBuilderKeys.Editor.SaveTemplateDialog.NameLabel)}
              </Label>
              <Input
                id="template-name"
                placeholder={t(PageBuilderKeys.Editor.SaveTemplateDialog.NamePlaceholder)}
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
              {t(PageBuilderKeys.Editor.SaveTemplateDialog.Cancel)}
            </Button>
            <Button onClick={handleSaveAsTemplate}>
              {t(PageBuilderKeys.Editor.SaveTemplateDialog.Save)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
