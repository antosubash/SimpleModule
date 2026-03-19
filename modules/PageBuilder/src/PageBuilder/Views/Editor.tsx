import { router } from '@inertiajs/react';
import { Puck } from '@measured/puck';
import { useCallback, useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { puckConfig } from '../puck/config';
import { loadPuckCss } from '../puck/load-css';

interface Page {
  id: number;
  title: string;
  slug: string;
  content: string;
}

interface Props {
  page: Page | null;
}

export default function Editor({ page }: Props) {
  const [saving, setSaving] = useState(false);

  useEffect(() => loadPuckCss(), []);

  const initialData = page?.content
    ? (() => {
        try {
          return JSON.parse(page.content);
        } catch {
          return {};
        }
      })()
    : {};

  const handlePublish = useCallback(
    async (data: any) => {
      setSaving(true);
      try {
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
      } catch (err) {
        console.error('Failed to save page:', err);
      } finally {
        setSaving(false);
      }
    },
    [page],
  );

  const editor = (
    <div style={{ position: 'fixed', inset: 0, zIndex: 9999, background: 'white' }}>
      <Puck config={puckConfig} data={initialData} onPublish={handlePublish} />
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
      {saving && (
        <span
          style={{
            position: 'absolute',
            top: 12,
            left: 100,
            zIndex: 10000,
            color: '#999',
            fontSize: 13,
          }}
        >
          Saving...
        </span>
      )}
    </div>
  );

  return createPortal(editor, document.body);
}
