import { Render } from '@measured/puck/rsc';
import { useEffect, useMemo } from 'react';
import { puckConfig } from '../puck/config';
import { loadPuckCss } from '../puck/load-css';

interface Page {
  id: number;
  title: string;
  slug: string;
  content: string;
}

interface Props {
  page: Page;
}

export default function Viewer({ page }: Props) {
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
      <Render config={puckConfig} data={data} />
    </div>
  );
}
