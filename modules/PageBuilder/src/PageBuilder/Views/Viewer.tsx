import { Render } from '@measured/puck/rsc';
import { useEffect, useMemo } from 'react';
import { Alert, AlertDescription } from '@simplemodule/ui';
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
        <Alert variant="warning" className="mb-6">
          <AlertDescription>Draft Preview — this version is not published</AlertDescription>
        </Alert>
      )}
      <Render config={puckConfig} data={data} />
    </div>
  );
}
