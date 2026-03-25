import { Render } from '@measured/puck/rsc';
import { Alert, AlertDescription } from '@simplemodule/ui';
import { useEffect, useMemo } from 'react';
import { puckConfig } from '../puck/config';
import { loadPuckCss } from '../puck/load-css';
import type { Page } from '../types';

interface Props {
  page: Page;
  isDraft?: boolean;
}

export default function Viewer({ page, isDraft }: Props) {
  useEffect(() => loadPuckCss(), []);
  const data = useMemo(() => {
    try {
      const parsed = JSON.parse(page.content);
      return {
        content: parsed.content ?? [],
        root: parsed.root ?? { props: {} },
      };
    } catch {
      return { content: [], root: { props: {} } };
    }
  }, [page.content]);

  return (
    <div className="max-w-4xl mx-auto py-8" data-testid="page-content">
      {isDraft && (
        <Alert variant="warning" className="mb-6">
          <AlertDescription>Draft Preview — this version is not published</AlertDescription>
        </Alert>
      )}
      <Render config={puckConfig} data={data} />
    </div>
  );
}
