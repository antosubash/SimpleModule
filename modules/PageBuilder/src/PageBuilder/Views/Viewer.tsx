import { Render } from '@measured/puck/rsc';
import { useEffect, useMemo } from 'react';
import type { Page } from '../types';
import { puckConfig } from '../puck/config';
import { loadPuckCss } from '../puck/load-css';

interface Props {
  page: Page;
  isDraft?: boolean;
}

export default function Viewer({ page, isDraft }: Props) {
  useEffect(() => loadPuckCss(), []);
  const data = useMemo(() => {
    try {
      return JSON.parse(page.content);
    } catch {
      return { content: [], root: {} };
    }
  }, [page.content]);

  return (
    <div className="max-w-4xl mx-auto py-8">
      {isDraft && (
        <div className="mb-6 rounded-lg border border-warning/30 bg-warning-bg px-4 py-3 text-warning-text text-sm font-medium flex items-center gap-2">
          <svg
            width="16"
            height="16"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path d="M12 9v4m0 4h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
          </svg>
          Draft Preview — this version is not published
        </div>
      )}
      <Render config={puckConfig} data={data} />
    </div>
  );
}
