import { router } from '@inertiajs/react';
import { Puck, usePuck } from '@measured/puck';
import { useCallback, useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import type { Page, PageTemplate } from '../types';
import { puckConfig } from '../puck/config';
import { loadPuckCss } from '../puck/load-css';

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
      <button
        type="button"
        onClick={() => setShowSaveTemplate(true)}
        style={{
          background: 'none',
          border: '1px solid #e5e5e5',
          borderRadius: 4,
          cursor: 'pointer',
          padding: '4px 12px',
          fontSize: 14,
          color: '#555',
        }}
      >
        Save as Template
      </button>
      <button
        type="button"
        onClick={handleSaveDraft}
        disabled={saving}
        style={{
          background: 'none',
          border: '1px solid #e5e5e5',
          borderRadius: 4,
          cursor: 'pointer',
          padding: '4px 12px',
          fontSize: 14,
          color: '#555',
          fontWeight: 500,
        }}
      >
        {saving ? 'Saving...' : saved ? 'Saved!' : 'Save Draft'}
      </button>
      {showSaveTemplate &&
        createPortal(
          <div
            style={{
              position: 'fixed',
              inset: 0,
              zIndex: 10001,
              background: 'rgba(0,0,0,0.4)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
            onClick={() => setShowSaveTemplate(false)}
          >
            <div
              style={{
                background: 'white',
                borderRadius: 8,
                padding: 24,
                minWidth: 360,
                boxShadow: '0 8px 32px rgba(0,0,0,0.2)',
              }}
              onClick={(e) => e.stopPropagation()}
            >
              <h3 style={{ fontSize: 16, fontWeight: 600, marginBottom: 12 }}>Save as Template</h3>
              <input
                type="text"
                placeholder="Template name"
                value={templateName}
                onChange={(e) => setTemplateName(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleSaveAsTemplate();
                }}
                style={{
                  width: '100%',
                  padding: '8px 12px',
                  border: '1px solid #ddd',
                  borderRadius: 6,
                  fontSize: 14,
                  marginBottom: 16,
                  boxSizing: 'border-box',
                }}
                autoFocus
              />
              <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
                <button
                  type="button"
                  onClick={() => setShowSaveTemplate(false)}
                  style={{
                    padding: '8px 16px',
                    border: '1px solid #ddd',
                    borderRadius: 6,
                    background: 'white',
                    cursor: 'pointer',
                    fontSize: 14,
                  }}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleSaveAsTemplate}
                  style={{
                    padding: '8px 16px',
                    border: 'none',
                    borderRadius: 6,
                    background: '#16a34a',
                    color: 'white',
                    cursor: 'pointer',
                    fontSize: 14,
                    fontWeight: 500,
                  }}
                >
                  Save
                </button>
              </div>
            </div>
          </div>,
          document.body,
        )}
    </>
  );
}

export default function Editor({ page, templates }: Props) {
  const [showTemplatePicker, setShowTemplatePicker] = useState(!page && !!templates?.length);

  useEffect(() => loadPuckCss(), []);

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
    const picker = (
      <div
        style={{
          position: 'fixed',
          inset: 0,
          zIndex: 9999,
          background: 'white',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <div style={{ maxWidth: 600, width: '100%', padding: 32 }}>
          <h2 style={{ fontSize: 24, fontWeight: 700, marginBottom: 8 }}>Choose a template</h2>
          <p style={{ color: '#666', marginBottom: 24, fontSize: 14 }}>
            Start from a template or create a blank page.
          </p>
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))',
              gap: 12,
              marginBottom: 24,
            }}
          >
            <button
              type="button"
              onClick={handleStartBlank}
              style={{
                padding: 24,
                border: '2px dashed #ddd',
                borderRadius: 8,
                cursor: 'pointer',
                background: 'none',
                textAlign: 'center',
                color: '#666',
                fontSize: 14,
              }}
            >
              <div style={{ fontSize: 32, marginBottom: 8 }}>+</div>
              Blank Page
            </button>
            {templates?.map((t) => (
              <button
                key={t.id}
                type="button"
                onClick={() => handleSelectTemplate(t)}
                style={{
                  padding: 24,
                  border: '1px solid #e5e5e5',
                  borderRadius: 8,
                  cursor: 'pointer',
                  background: '#fafafa',
                  textAlign: 'center',
                  fontSize: 14,
                  fontWeight: 500,
                }}
              >
                <div style={{ fontSize: 20, marginBottom: 8 }}>
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                    style={{ margin: '0 auto' }}
                  >
                    <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6z" />
                    <path d="M14 2v6h6M16 13H8M16 17H8M10 9H8" />
                  </svg>
                </div>
                {t.name}
              </button>
            ))}
          </div>
          <button
            type="button"
            onClick={() => router.visit('/admin/pages')}
            style={{
              color: '#666',
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              fontSize: 14,
            }}
          >
            Cancel
          </button>
        </div>
      </div>
    );
    return createPortal(picker, document.body);
  }

  const editor = (
    <div style={{ position: 'fixed', inset: 0, zIndex: 9999, background: 'white' }}>
      <Puck
        key={JSON.stringify(initialData)}
        config={puckConfig}
        data={initialData}
        onPublish={handlePublish}
        overrides={{
          headerActions: ({ children }) => (
            <>
              <HeaderActions page={page} />
              {children}
            </>
          ),
        }}
      />
      <button
        type="button"
        onClick={() => router.visit('/admin/pages')}
        style={{
          position: 'absolute',
          top: 8,
          left: 8,
          zIndex: 10000,
          background: 'white',
          border: '1px solid #ccc',
          borderRadius: 4,
          cursor: 'pointer',
          display: 'flex',
          alignItems: 'center',
          gap: 4,
          color: '#333',
          fontSize: 14,
          padding: '4px 12px',
        }}
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
        Back
      </button>
    </div>
  );

  return createPortal(editor, document.body);
}
